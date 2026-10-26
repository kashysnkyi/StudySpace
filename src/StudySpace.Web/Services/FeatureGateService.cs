using StudySpace.Core.Entities;

namespace StudySpace.Web.Services;

public enum WorkspaceFeature
{
    SharedBoard,
    AiAssistant,
    AdvancedInsights,
    LargeUploads
}

public sealed class FeatureGateService
{
    public bool IsEnabled(SubscriptionPlan plan, WorkspaceFeature feature) => feature switch
    {
        WorkspaceFeature.SharedBoard => plan == SubscriptionPlan.Premium,
        WorkspaceFeature.AiAssistant => plan == SubscriptionPlan.Premium,
        WorkspaceFeature.AdvancedInsights => plan == SubscriptionPlan.Premium,
        WorkspaceFeature.LargeUploads => plan == SubscriptionPlan.Premium,
        _ => false
    };
}

