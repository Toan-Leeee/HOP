using MeetingScheduler.Web.Data;
using MeetingScheduler.Web.Models;
using Microsoft.EntityFrameworkCore;
using MeetingScheduler.Web.ViewModels.Email;
namespace MeetingScheduler.Web.Services.Email;
public interface IEmailQueue
{
    Task EnqueueAsync(EmailJob job, DateTime? availableAt = null, int? meetingId = null, string kind = "Notification", CancellationToken ct = default);
}
public class EmailQueue(ApplicationDbContext db) : IEmailQueue
{
    public async Task EnqueueAsync(EmailJob job, DateTime? availableAt = null, int? meetingId = null, string kind = "Notification", CancellationToken ct = default)
    {
        db.OutgoingEmails.Add(new OutgoingEmail { To = job.To, Subject = job.Subject, Body = job.Body, AvailableAt = availableAt ?? DateTime.UtcNow, MeetingId = meetingId, Kind = kind });
        await db.SaveChangesAsync(ct);
    }
}
internal static class SmtpSettings
{
    public static bool IsConfigured(IConfiguration configuration) => !string.IsNullOrWhiteSpace(configuration["Smtp:Host"]) && !string.IsNullOrWhiteSpace(configuration["Smtp:From"]);
}
