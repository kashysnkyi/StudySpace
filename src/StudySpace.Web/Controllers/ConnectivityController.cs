using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers;

public sealed class ConnectivityController(ConnectivityPulseService pulseService) : Controller
{
    public IActionResult Index() => View();

    [HttpGet("api/connectivity/state")]
    public IActionResult State() => Ok(pulseService.Current);

    [HttpGet("api/connectivity/long-poll")]
    public async Task<IActionResult> LongPoll(long version = -1, CancellationToken cancellationToken = default) =>
        Ok(await pulseService.WaitForChangeAsync(version, TimeSpan.FromSeconds(20), cancellationToken));

    [HttpGet("api/connectivity/websocket")]
    public async Task WebSocket(CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var version = -1L;

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var pulse = await pulseService.WaitForChangeAsync(version, TimeSpan.FromSeconds(20), cancellationToken);
            version = pulse.Version;
            var payload = JsonSerializer.SerializeToUtf8Bytes(pulse, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
        }
    }
}

