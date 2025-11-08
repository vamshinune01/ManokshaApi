using System.Threading.Tasks;

namespace ManokshaApi.Services
{
    public class SmsService : ISmsService
    {
        public Task SendSmsAsync(string phoneNumber, string message)
        {
            Console.WriteLine($"📱 SMS to {phoneNumber}: {message}");
            return Task.CompletedTask;
        }
    }
}
