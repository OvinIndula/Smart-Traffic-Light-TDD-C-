namespace SmartTrafficLight.Tests
{
    public interface IWebService
    {
        // Log fault detection event to web service
        bool FaultDetected(bool on);

        // Log which devices need engineer inspection (e.g., "VehicleSignal,PedestrianSignal,")
        void LogEngineerRequired(string deviceType);
    }
}