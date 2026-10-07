using Hosco.Api.Observability;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;

namespace Hosco.Api.Controllers;

[ApiController]
[Authorize(Policy = "ReportingReader")]
[RequestTimeout("chat")]
[Route("api/v1/chat/messages")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ChatController(IChatService chat, ICorrelationContext correlation) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ChatMessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatMessageResponse>> Send(ChatMessageRequest request, CancellationToken ct)
    {
        var result = await chat.SendAsync(request, ct);
        HttpContext.Items["Hosco.QueryId"] = result.ReportingOperation;
        return Ok(new ChatMessageResponse(result.Message, result.Intent, result.Status, result.Confidence,
            result.Data, result.Suggestions, result.Context, result.ReportingOperation, correlation.CorrelationId));
    }
}
