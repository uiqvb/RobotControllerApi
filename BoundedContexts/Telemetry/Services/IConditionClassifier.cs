namespace RobotControllerApi.BoundedContexts.Telemetry.Services;

public interface IConditionClassifier
{
    string ClassifyStatusMessage(string payloadJson);
    string ExtractOperationalState(string payloadJson);
}
