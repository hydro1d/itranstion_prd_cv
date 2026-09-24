using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CvPlatform.Data;

/// <summary>
/// Fast, non-blocking database reachability detector supporting both local and cloud databases.
/// Dynamically extracts host/port from the active connection string, supports DNS resolution,
/// and uses adaptive timeouts (short for localhost, cloud-friendly for remote hosts).
/// </summary>
public static class DatabaseAvailability
{
    private static string? _connectionString;
    private static bool? _isAvailable;
    private static DateTime _lastChecked = DateTime.MinValue;
    private static readonly object _lock = new();

    public static void SetConnectionString(string? connStr)
    {
        _connectionString = connStr;
        lock (_lock)
        {
            _isAvailable = null;
            _lastChecked = DateTime.MinValue;
        }
    }

    public static async Task<bool> IsAvailableAsync(DbContext? context = null)
    {
        lock (_lock)
        {
            // Cache reachability for 30 seconds
            if (_isAvailable.HasValue && (DateTime.UtcNow - _lastChecked).TotalSeconds < 30)
            {
                return _isAvailable.Value;
            }
        }

        try
        {
            var connStr = _connectionString ?? context?.Database.GetConnectionString();
            string host = "127.0.0.1";
            int port = 5432;

            if (!string.IsNullOrWhiteSpace(connStr))
            {
                try
                {
                    var builder = new NpgsqlConnectionStringBuilder(connStr);
                    if (!string.IsNullOrWhiteSpace(builder.Host))
                    {
                        host = builder.Host;
                    }
                    if (builder.Port > 0)
                    {
                        port = builder.Port;
                    }
                }
                catch
                {
                    // Fall back to default host/port if unparseable
                }
            }

            bool isLocal = host == "127.0.0.1" || host == "localhost" || host == "::1";
            int timeoutMs = isLocal ? 200 : 3500;

            // 1. Fast DNS and TCP handshake
            using var tcpClient = new TcpClient();
            using var cts = new CancellationTokenSource(timeoutMs);
            await tcpClient.ConnectAsync(host, port, cts.Token);

            if (!tcpClient.Connected)
            {
                SetResult(false);
                return false;
            }

            // 2. If EF Core DbContext is provided, test authentication & database access
            if (context != null)
            {
                int efTimeoutMs = isLocal ? 400 : 4000;
                using var efCts = new CancellationTokenSource(efTimeoutMs);
                var canConnect = await context.Database.CanConnectAsync(efCts.Token);
                SetResult(canConnect);
                return canConnect;
            }

            SetResult(true);
            return true;
        }
        catch
        {
            SetResult(false);
            return false;
        }
    }

    private static void SetResult(bool available)
    {
        lock (_lock)
        {
            _isAvailable = available;
            _lastChecked = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Synchronous quick check based on current cache state.
    /// </summary>
    public static bool IsKnownOffline => _isAvailable == false;
}
