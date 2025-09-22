using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService
{
    public class SMTPEmailSender : IEmailSender
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _from;

        public SMTPEmailSender(string host, int port, string user, string pass, string from)
        {
            _host = host;
            _port = port;
            _user = user;
            _pass = pass;
            _from = from;
        }

        public async Task SendAsync(IEnumerable<string> recipients, string subject, string body)
        {
            if (recipients == null || !recipients.Any())
                return;

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(_from);
                foreach (var to in recipients.Distinct())
                {
                    message.To.Add(to);
                }

                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = false;

                using (var client = new SmtpClient(_host, _port))
                {
                    client.Credentials = new NetworkCredential(_user, _pass);
                    client.EnableSsl = true; 
                    await client.SendMailAsync(message);
                }
            }
        }

        public Task SendAsync(string to, string subject, string body)
        {
            return SendAsync(new[] { to }, subject, body);
        }
    }
}
