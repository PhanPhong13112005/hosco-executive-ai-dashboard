using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.Application.Services;

public static class AlertRecipientPolicy
{
    public static IReadOnlyList<SystemRole> InitialRecipients(Alert alert) => alert.RuleCode switch
    {
        "AL-01" or "AL-02" => alert.Severity == AlertSeverity.Critical
            ? [SystemRole.BranchManager, SystemRole.Owner, SystemRole.ChainManager]
            : [SystemRole.BranchManager, SystemRole.Owner],
        "AL-03" => alert.Severity == AlertSeverity.Critical
            ? [SystemRole.BranchManager, SystemRole.Owner, SystemRole.ChainManager]
            : [SystemRole.BranchManager, SystemRole.Owner],
        "AL-04" => alert.Severity == AlertSeverity.High
            ? [SystemRole.BranchManager, SystemRole.ChainManager]
            : [SystemRole.BranchManager],
        "AL-05" => alert.Severity == AlertSeverity.Critical
            ? [SystemRole.BranchManager, SystemRole.Owner, SystemRole.ChainManager]
            : [SystemRole.BranchManager, SystemRole.Owner],
        _ => []
    };

    public static IReadOnlyList<SystemRole> EscalationRecipients(Alert alert) => alert.RuleCode switch
    {
        "AL-01" or "AL-02" => [SystemRole.ChainManager],
        _ => []
    };
}
