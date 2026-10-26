using StudySpace.Core.Entities;
using StudySpace.Web.Services;

namespace StudySpace.UnitTests;

public sealed class FeatureGateTests
{
    [Theory]
    [InlineData(SubscriptionPlan.Free, WorkspaceFeature.SharedBoard, false)]
    [InlineData(SubscriptionPlan.Free, WorkspaceFeature.AiAssistant, false)]
    [InlineData(SubscriptionPlan.Premium, WorkspaceFeature.SharedBoard, true)]
    [InlineData(SubscriptionPlan.Premium, WorkspaceFeature.AiAssistant, true)]
    public void PlanControlsPremiumFeatures(SubscriptionPlan plan, WorkspaceFeature feature, bool expected)
    {
        Assert.Equal(expected, new FeatureGateService().IsEnabled(plan, feature));
    }
}
