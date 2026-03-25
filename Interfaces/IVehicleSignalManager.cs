namespace SmartTrafficLight.Tests
{
    public interface IVehicleSignalManager
    {
        // Returns status string - "VehicleSignal,OK,FAULT,..." for each signal direction
        string GetStatus();

        // Set all vehicle signals to red
        bool SetAllRed();

        // Set all vehicle signals to green
        bool SetAllGreen(bool on);

        // Log if engineer maintenance is needed
        bool LogEngineerRequired(bool needsEngineer);
    }
}