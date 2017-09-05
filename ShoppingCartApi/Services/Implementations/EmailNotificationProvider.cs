using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Services.Abstractions
{
    public class EmailNotificationProvider : INotificationProvider
    {
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Athanasios Eleftheriadis", "test@localhost"));
            emailMessage.To.Add(new MailboxAddress("Athanasios Eleftheriadis", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("plain") { Text = message };

            using (var client = new SmtpClient())
            {
                //needs a proper smtp server
                //client.LocalDomain = "localhost";
                //await client.ConnectAsync("localhost", 25, SecureSocketOptions.None).ConfigureAwait(false);
                //await client.SendAsync(emailMessage).ConfigureAwait(false);
                // client.DisconnectAsync(true).ConfigureAwait(false);
            }
        }
    }
}
