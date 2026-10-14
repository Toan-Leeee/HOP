using System.Net;
using System.Net.Mail;
using MeetingScheduler.Web.ViewModels.Email;
namespace MeetingScheduler.Web.Services.Email;
public interface IMailService { Task<bool> SendAsync(EmailJob job, CancellationToken ct = default); }
public class MailService(IConfiguration configuration, ILogger<MailService> logger) : IMailService
{
    public async Task<bool> SendAsync(EmailJob job, CancellationToken ct = default)
    {
        if (!SmtpSettings.IsConfigured(configuration))
        {
            logger.LogWarning("SMTP is not configured; email was not sent."); return false;
        }
        try
        {
            using var message = new MailMessage(configuration["Smtp:From"]!, job.To, job.Subject, job.Body);
            using var client = new SmtpClient(configuration["Smtp:Host"], configuration.GetValue("Smtp:Port", 587)) { EnableSsl = configuration.GetValue("Smtp:EnableSsl", true), UseDefaultCredentials = false };
            if (!string.IsNullOrWhiteSpace(configuration["Smtp:Username"])) client.Credentials = new NetworkCredential(configuration["Smtp:Username"], configuration["Smtp:Password"]);
            await client.SendMailAsync(message, ct);
            logger.LogInformation("SMTP accepted email with subject {Subject}.", job.Subject);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or FormatException or ArgumentException or InvalidOperationException)
        {
            logger.LogError(ex, "Email was not sent."); return false;
        }
    }
}
