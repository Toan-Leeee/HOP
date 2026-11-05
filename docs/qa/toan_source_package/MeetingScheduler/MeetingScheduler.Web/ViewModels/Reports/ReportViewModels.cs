namespace MeetingScheduler.Web.ViewModels.Reports;
public class ReportFilter
{
    public DateTime From { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime To { get; set; } = DateTime.Today.AddDays(1);
    public int? RoomId { get; set; }
}
public record RoomReport(int RoomId, string RoomName, int MeetingCount, double TotalHours);
public class ReportResult
{
    public ReportFilter Filter { get; set; } = new();
    public int TotalMeetings { get; set; }
    public int CancelledMeetings { get; set; }
    public int AcceptedResponses { get; set; }
    public int PendingResponses { get; set; }
    public int DeclinedResponses { get; set; }
    public List<RoomReport> Rooms { get; set; } = [];
}
