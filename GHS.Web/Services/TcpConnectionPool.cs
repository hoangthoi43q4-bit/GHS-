using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using GHS.Web.Hubs;

namespace GHS.Web.Services;

/// <summary>
/// Manages TCP connections to industrial equipment.
/// Connection pool pattern with health checks and auto-reconnect.
/// </summary>
public class TcpConnectionPool : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<int, TcpClient> _clients = new();
    private readonly IHubContext<LogHub> _hubContext;
    private readonly ILogger<TcpConnectionPool> _logger;
    private readonly IConfiguration _config;
    private CancellationTokenSource? _cts;

    public TcpConnectionPool(
        IHubContext<LogHub> hubContext,
        ILogger<TcpConnectionPool> logger,
        IConfiguration config)
    {
        _hubContext = hubContext;
        _logger = logger;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => HealthCheckLoop(_cts.Token), _cts.Token);
        _logger.LogInformation("TCP Connection Pool started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        foreach (var kvp in _clients)
        {
            try { kvp.Value.Close(); } catch { }
        }
        _clients.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Send a LOAD_MATERIAL command to the specified equipment and return the response.
    /// </summary>
    public async Task<SendResult> SendCommandAsync(
        int equipmentId,
        string equipmentName,
        string serverIp,
        int serverPort,
        string command,
        CancellationToken ct = default)
    {
        var timeoutMs = _config.GetValue("TcpSettings:ReadTimeoutMs", 5000);
        var result = new SendResult();

        try
        {
            // Get or create connection
            var client = GetOrCreateClient(equipmentId, serverIp, serverPort);

            if (!client.Connected)
            {
                await client.ConnectAsync(serverIp, serverPort, ct);
            }

            var stream = client.GetStream();
            stream.ReadTimeout = timeoutMs;

            // Wrap with STX/ETX
            string formattedMessage = $"{(char)0x02}{command}{(char)0x03}";
            byte[] dataToSend = Encoding.ASCII.GetBytes(formattedMessage);

            await stream.WriteAsync(dataToSend, ct);
            await _hubContext.Clients.All.SendAsync("ReceiveLog",
                $"发送到 {equipmentName}({serverIp}:{serverPort}) -> {formattedMessage}", ct);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);

            if (bytesRead > 0)
            {
                result.Response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                result.IsAckSuccess = result.Response.Contains("ACK");
                await _hubContext.Clients.All.SendAsync("ReceiveLog",
                    $"响应: {result.Response}", ct);
            }
            else
            {
                result.Response = "服务器未返回数据";
            }
        }
        catch (Exception ex)
        {
            result.Response = ex.Message;
            RemoveClient(equipmentId);
            await _hubContext.Clients.All.SendAsync("ReceiveLog",
                $"错误: {ex.Message}", ct);
        }

        return result;
    }

    private TcpClient GetOrCreateClient(int equipmentId, string ip, int port)
    {
        return _clients.GetOrAdd(equipmentId, _ => new TcpClient
        {
            ReceiveTimeout = _config.GetValue("TcpSettings:ReadTimeoutMs", 5000),
            SendTimeout = _config.GetValue("TcpSettings:ConnectTimeoutMs", 5000)
        });
    }

    private void RemoveClient(int equipmentId)
    {
        if (_clients.TryRemove(equipmentId, out var client))
        {
            try { client.Close(); } catch { }
        }
    }

    private async Task HealthCheckLoop(CancellationToken ct)
    {
        var interval = _config.GetValue("TcpSettings:HealthCheckIntervalSeconds", 30);
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval), ct);
            foreach (var kvp in _clients)
            {
                try
                {
                    if (!kvp.Value.Connected)
                    {
                        RemoveClient(kvp.Key);
                    }
                }
                catch
                {
                    RemoveClient(kvp.Key);
                }
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        foreach (var kvp in _clients)
        {
            try { kvp.Value.Dispose(); } catch { }
        }
        _clients.Clear();
    }
}

public class SendResult
{
    public string Response { get; set; } = string.Empty;
    public bool IsAckSuccess { get; set; }
}
