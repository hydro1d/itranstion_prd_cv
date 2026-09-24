using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;

namespace CvPlatform.Data;

/// <summary>
/// Fast, non-blocking database reachability detector.
/// Uses a short-timeout TCP probe and caching to prevent 15-30 second socket timeouts
/// when PostgreSQL is not running locally.
/// </summary>
public static class DatabaseAvailability
{
    private static bool? _isAvailable;
    private static DateTime _lastChecked = DateTime.MinValue;
    private static readonly object _lock = new();

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

        return await Task.Run(async () =>
        {
            try
            {
                // 1. Ultra-fast TCP socket probe to 127.0.0.1:5432 (150ms max)
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var ar = socket.BeginConnect("127.0.0.1", 5432, null, null);
                bool connected = ar.AsyncWaitHandle.WaitOne(150, true);

                if (!connected || !socket.Connected)
                {
                    lock (_lock)
                    {
                        _isAvailable = false;
                        _lastChecked = DateTime.UtcNow;
                    }
                    return false;
                }

                socket.EndConnect(ar);

                // 2. If TCP succeeded and context provided, verify EF Core connection with short cancellation
                if (context != null)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
                    var canConnect = await context.Database.CanConnectAsync(cts.Token);
                    lock (_lock)
                    {
                        _isAvailable = canConnect;
                        _lastChecked = DateTime.UtcNow;
                    }
                    return canConnect;
                }

                lock (_lock)
                {
                    _isAvailable = true;
                    _lastChecked = DateTime.UtcNow;
                }
                return true;
            }
            catch
            {
                lock (_lock)
                {
                    _isAvailable = false;
                    _lastChecked = DateTime.UtcNow;
                }
                return false;
            }
        });
    }

    /// <summary>
    /// Synchronous quick check based on current cache state.
    /// </summary>
    public static bool IsKnownOffline => _isAvailable == false;
}
