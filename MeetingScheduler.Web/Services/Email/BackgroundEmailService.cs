namespace MeetingScheduler.Web.Services.Email;
using Microsoft.EntityFrameworkCore;
public class BackgroundEmailService(IServiceScopeFactory scopes, IMailService mail, IConfiguration configuration, ILogger<BackgroundEmailService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!SmtpSettings.IsConfigured(configuration)) { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); continue; }
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MeetingScheduler.Web.Data.ApplicationDbContext>();
                var now = DateTime.UtcNow;
                var pending = await db.OutgoingEmails.Where(x => x.SentAt == null && x.AvailableAt <= now && x.Attempts < 5)
                    .OrderBy(x => x.AvailableAt).Take(20).ToListAsync(stoppingToken);
                foreach (var item in pending)
                {
                    var sent = false;
                    try { sent = await mail.SendAsync(new(item.To, item.Subject, item.Body), stoppingToken); }
                    catch (Exception ex) when (ex is not OperationCanceledException) { item.LastError = ex.Message; logger.LogError(ex, "Queued notification failed."); }
                    item.Attempts++;
                    if (sent) { item.SentAt = DateTime.UtcNow; item.LastError = null; }
                    else { item.LastError ??= "SMTP send failed."; item.AvailableAt = DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, item.Attempts))); }
                }
                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Email queue polling failed."); }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
