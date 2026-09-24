using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CvPlatform.Data;

/// <summary>
/// Fast, non-blocking database reachability detector supporting local and cloud databases.
/// For localhost, uses an ultra-fast 200ms socket probe to avoid hanging.
/// For cloud databases (Render/Railway), directly relies on EF Core's CanConnectAsync with adaptive timeouts.
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
                catch { }
            }

            bool isLocal = host == "127.0.0.1" || host == "localhost" || host == "::1";

            // If localhost, use ultra-fast 200ms TCP probe so local dev without Postgres is instantaneous
            if (isLocal)
            {
                try
                {
                    using var tcpClient = new TcpClient();
                    using var cts = new CancellationTokenSource(200);
                    await tcpClient.ConnectAsync(host, port, cts.Token);
                    if (!tcpClient.Connected)
                    {
                        SetResult(false);
                        return false;
                    }
                }
                catch
                {
                    SetResult(false);
                    return false;
                }
            }

            // For cloud databases (Render/Railway), directly verify with EF Core
            if (context != null)
            {
                int efTimeoutMs = isLocal ? 500 : 10000;
                using var efCts = new CancellationTokenSource(efTimeoutMs);
                var canConnect = await context.Database.CanConnectAsync(efCts.Token);
                Console.WriteLine($"[DatabaseAvailability] CanConnectAsync result: {canConnect} on host {host}");
                SetResult(canConnect);
                return canConnect;
            }

            SetResult(true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DatabaseAvailability] Reachability check failed: {ex.Message}");
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

    public static bool IsKnownOffline => _isAvailable == false;
}
