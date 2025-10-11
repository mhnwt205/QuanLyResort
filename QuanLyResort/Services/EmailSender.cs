using Microsoft.Extensions.Configuration;
using QuanLyResort.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Threading.Tasks;

namespace QuanLyResort.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _config["Smtp:Host"];
            var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Pass"]?.Replace(" ", ""); // Loại bỏ dấu cách trong App Password
            var from = _config["Smtp:From"] ?? user;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                // Fallback: log to console
                System.Console.WriteLine($"[EMAIL-FAKE] To: {toEmail}, Subject: {subject}\n{htmlBody}");
                await Task.CompletedTask;
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Resort Management System", from));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // Kết nối tới Gmail SMTP server
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                
                // Xác thực với App Password
                await client.AuthenticateAsync(user, pass);
                
                // Gửi email
                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}


