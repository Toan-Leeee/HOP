using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Reports;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Areas.Admin.Controllers;

public class ReportController(IReportService reports, IRoomService rooms) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(ReportFilter filter, CancellationToken ct)
    {
        ViewBag.Rooms = await rooms.GetLookupAsync(ct);
        if (filter.To <= filter.From) ModelState.AddModelError(nameof(filter.To), "Ngày kết thúc phải sau ngày bắt đầu.");
        return View(ModelState.IsValid ? await reports.GetAsync(filter, ct) : new ReportResult { Filter = filter });
    }
}
