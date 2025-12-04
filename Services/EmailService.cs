using System.Net;
using System.Net.Mail;

namespace WorkFlowBasic.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string message);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        // AppSettings.json'dan ayarları oku
        var settings = _configuration.GetSection("EmailSettings");

        var mailServer = settings["MailServer"];
        var mailPort = int.Parse(settings["MailPort"]!);
        var senderEmail = settings["SenderEmail"];
        var senderName = settings["SenderName"];
        var password = settings["Password"];

        var smtpClient = new SmtpClient(mailServer)
        {
            Port = mailPort,
            Credentials = new NetworkCredential(senderEmail, password),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail!, senderName),
            Subject = subject,
            Body = message,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(toEmail);

        try
        {
            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            // Mail gitmezse uygulama çökmesin, loga yazıp devam etsin
            Console.WriteLine($"Mail gönderilemedi: {ex.Message}");
        }
    }
}