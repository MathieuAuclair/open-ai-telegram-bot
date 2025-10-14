using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace OlegBot.Helpers
{
    public class MailHelper
    {
        public static void SendEmail(string email, string subject, string htmlMessage)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var host = config.GetValue("Smtp:Server", "defaultmailserver");
            var port = config.GetValue("Smtp:Port", 25);
            var fromAddress = config.GetValue("Smtp:FromAddress", "defaultfromaddress");
            var password = config.GetValue("Smtp:Password", "Passw0rd!");
            var message = new MailMessage
            {
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };

            message.Headers.Add(
                "List-Unsubscribe",
                $"<mailto:{config.GetValue("Smtp:Unsubscribe", fromAddress)}>"
            );

            message.To.Add(email);
            message.From = new MailAddress(fromAddress);

            var smtp = new SmtpClient
            {
                Host = host,
                Port = port,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress, password)
            };

            using (var msg = new MailMessage(fromAddress, email)
            {
                Subject = subject,
                Body = htmlMessage
            })
            {
                smtp.Send(message);
            }
        }
    }
}