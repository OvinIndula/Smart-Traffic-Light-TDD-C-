namespace SmartTrafficLight.Tests
{
    public interface IEmailService
    {
        // Send email notification
        void SendMail(string emailAddress, string subject, string message);
    }
}