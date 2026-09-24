using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CvPlatform.Data;

/// <summary>
/// Fast, non-blocking database reachability detector supporting local, cloud, and cross-region Render databases.
/// Dynamically extracts host/port from active connection string, supports DNS resolution,
/// Render cross-region automatic fallback, and adaptive timeouts.
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
            bool tcpSuccess = false;
            try
            {
                using var tcpClient = new TcpClient();
                using var cts = new CancellationTokenSource(timeoutMs);
                await tcpClient.ConnectAsync(host, port, cts.Token);
                tcpSuccess = tcpClient.Connected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseAvailability] Direct TCP connect to {host}:{port} failed: {ex.Message}");
                // If it's an internal Render host (dpg-*) that couldn't be resolved (e.g. cross-region), try known Render external endpoints
                if (host.StartsWith("dpg-") && !host.Contains('.'))
                {
                    string[] regions = ["oregon", "singapore", "frankfurt", "ohio", "virginia"];
                    foreach (var reg in regions)
                    {
                        var candidate = $"{host}.{reg}-postgres.render.com";
                        try
                        {
                            Console.WriteLine($"[DatabaseAvailability] Attempting fallback to external host {candidate}:{port}...");
                            using var fallbackClient = new TcpClient();
                            using var fbCts = new CancellationTokenSource(2500);
                            await fallbackClient.ConnectAsync(candidate, port, fbCts.Token);
                            if (fallbackClient.Connected)
                            {
                                Console.WriteLine($"[DatabaseAvailability] Successfully resolved external host {candidate}!");
                                host = candidate;
                                tcpSuccess = true;
                                if (!string.IsNullOrWhiteSpace(connStr))
                                {
                                    var bldr = new NpgsqlConnectionStringBuilder(connStr) { Host = candidate };
                                    connStr = bldr.ConnectionString;
                                    _connectionString = connStr;
                                    if (context != null)
                                    {
                                        context.Database.SetConnectionString(connStr);
                                    }
                                }
                                break;
                            }
                        }
                        catch
                        {
                            // continue trying next region
                        }
                    }
                }
            }

            if (!tcpSuccess)
            {
                SetResult(false);
                return false;
            }

            // 2. If EF Core DbContext is provided, test authentication & database access
            if (context != null)
            {
                try
                {
                    int efTimeoutMs = isLocal ? 400 : 5000;
                    using var efCts = new CancellationTokenSource(efTimeoutMs);
                    var canConnect = await context.Database.CanConnectAsync(efCts.Token);
                    Console.WriteLine($"[DatabaseAvailability] CanConnectAsync result: {canConnect} on host {host}");
                    SetResult(canConnect);
                    return canConnect;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DatabaseAvailability] CanConnectAsync failed on host {host}: {ex.Message}");
                    SetResult(false);
                    return false;
                }
            }

            SetResult(true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DatabaseAvailability] Unexpected error: {ex.Message}");
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
