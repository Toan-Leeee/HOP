using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Meetings;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Areas.Admin.Controllers;

public class MeetingController(IMeetingService meetings, IRoomService rooms) : AdminController
{
    [HttpGet]
    public async Task<IActionResult> Index(MeetingFilter filter, CancellationToken ct)
    {
        ViewBag.Filter = filter;
        ViewBag.Rooms = await rooms.GetLookupAsync(ct);
        return View(await meetings.QueryAsync(filter, CurrentUserId, ct));
    }

    [HttpGet]
    public IActionResult Details(int id) => RedirectToAction("Details", "Meeting", new { area = "", id });

    [HttpGet]
    public IActionResult Create() => RedirectToAction("Create", "Meeting", new { area = "" });

    [HttpGet]
    public IActionResult Edit(int id) => RedirectToAction("Edit", "Meeting", new { area = "", id });

    [HttpGet]
    public IActionResult Cancel(int id) => RedirectToAction("Cancel", "Meeting", new { area = "", id });

    [HttpGet]
    public IActionResult Delete(int id) => RedirectToAction("Delete", "Meeting", new { area = "", id });

    [HttpGet]
    public IActionResult RsvpTracking(int id) => RedirectToAction("RsvpTracking", "Meeting", new { area = "", id });
}
