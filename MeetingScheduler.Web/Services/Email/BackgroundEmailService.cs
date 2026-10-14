namespace MeetingScheduler.Web.Services.Email;
public class BackgroundEmailService(IEmailQueue queue, IMailService mail, ILogger<BackgroundEmailService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in queue.ReadAllAsync(stoppingToken))
            {
                try { if (!await mail.SendAsync(job, stoppingToken)) logger.LogWarning("Queued notification failed; no delivery success was recorded."); }
                catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Queued notification failed."); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
