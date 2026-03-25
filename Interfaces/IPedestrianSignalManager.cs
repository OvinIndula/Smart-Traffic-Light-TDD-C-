namespace SmartTrafficLight.Tests
{
    public interface IPedestrianSignalManager
    {
        // Returns status string - "PedestrianSignal,OK,FAULT,..." indicating health of each signal
        string GetStatus();

        // Enable/disable pedestrian wait state
        bool SetWait(bool on);

        // Enable/disable pedestrian walk state
        bool SetWalk(bool on);

        // Enable/disable audible indicator (beeping/audio cues)
        bool SetAudible(bool on);
    }
}