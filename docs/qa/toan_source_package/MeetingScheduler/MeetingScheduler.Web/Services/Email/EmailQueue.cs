using System.Threading.Channels;
using MeetingScheduler.Web.ViewModels.Email;
namespace MeetingScheduler.Web.Services.Email;
public interface IEmailQueue
{
    bool TryEnqueue(EmailJob job);
    IAsyncEnumerable<EmailJob> ReadAllAsync(CancellationToken ct);
}
public class EmailQueue(IConfiguration configuration) : IEmailQueue
{
    private readonly Channel<EmailJob> channel = Channel.CreateBounded<EmailJob>(new BoundedChannelOptions(500) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    public bool TryEnqueue(EmailJob job) => SmtpSettings.IsConfigured(configuration) && channel.Writer.TryWrite(job);
    public IAsyncEnumerable<EmailJob> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}
internal static class SmtpSettings
{
    public static bool IsConfigured(IConfiguration configuration) => !string.IsNullOrWhiteSpace(configuration["Smtp:Host"]) && !string.IsNullOrWhiteSpace(configuration["Smtp:From"]);
}
