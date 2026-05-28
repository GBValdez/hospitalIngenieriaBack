using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using project.utils.dto;

namespace project.utils.services
{
    public class emailService
    {
        private readonly IConfiguration configuration;
        public emailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void SendEmail(emailSendDto emailData)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(MailboxAddress.Parse(this.configuration.GetSection("Email:UserName").Value));
            emailMessage.To.Add(new MailboxAddress("", emailData.email));
            emailMessage.Subject = emailData.subject;

            BodyBuilder bodyBuilder = new BodyBuilder { HtmlBody = emailData.message };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.Connect(
                    this.configuration.GetSection("Email:Host").Value,
                    Convert.ToInt16(this.configuration.GetSection("Email:Port").Value),
                    SecureSocketOptions.StartTls);

                client.Authenticate(
                    this.configuration.GetSection("Email:UserName").Value,
                    this.configuration.GetSection("Email:Password").Value
                );
                client.Send(emailMessage);
                client.Disconnect(true);
            }
        }
    }
}