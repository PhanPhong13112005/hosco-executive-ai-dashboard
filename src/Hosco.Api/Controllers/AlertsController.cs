using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hosco.Api.Controllers;

[ApiController]
[Authorize(Policy = "ReportingReader")]
[Route("api/v1/alerts")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AlertsController(IAlertService alerts) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AlertPage>(StatusCodes.Status200OK)]
    public Task<AlertPage> List([FromQuery] AlertFilter filter, CancellationToken ct) => alerts.ListAsync(filter, ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AlertDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<AlertDetail> Detail(Guid id, CancellationToken ct) => alerts.GetAsync(id, ct);

    [HttpPost("{id:guid}/acknowledge")]
    [ProducesResponseType<AlertDetail>(StatusCodes.Status200OK)]
    public Task<AlertDetail> Acknowledge(Guid id, CancellationToken ct) => alerts.AcknowledgeAsync(id, ct);

    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType<AlertDetail>(StatusCodes.Status200OK)]
    public Task<AlertDetail> Resolve(Guid id, CancellationToken ct) => alerts.ResolveAsync(id, ct);
}

[ApiController]
[Authorize(Policy = "ReportingReader")]
[Route("api/v1/alert-rules")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AlertRulesController(IAlertService alerts) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AlertRuleView>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<AlertRuleView>> List([FromQuery] Guid? branchId, CancellationToken ct) => alerts.ListRulesAsync(branchId, ct);

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "AlertConfigurator")]
    [ProducesResponseType<AlertRuleView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<AlertRuleView> Update(Guid id, [FromBody] UpdateAlertRule update, CancellationToken ct) => alerts.UpdateRuleAsync(id, update, ct);
}

