namespace SmartTrafficLight.Tests
{
    public interface ITimeManager
    {
        // Returns status string - "Timer,OK,FAULT,..." indicating timer health
        string GetStatus();

        // Wait for specified seconds
        bool Delay(int time);
    }
}