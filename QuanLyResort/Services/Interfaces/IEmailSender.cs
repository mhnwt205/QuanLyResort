using System.Threading.Tasks;

namespace QuanLyResort.Services.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}


