using MeetingScheduler.Web.Models.Enums;
using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Meetings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Controllers;

public class MeetingController(IMeetingService meetings, IRoomService rooms, IUserService users,
    IRsvpService rsvps) : AuthenticatedController
{
    [HttpGet]
    public async Task<IActionResult> Index(MeetingFilter filter, CancellationToken ct)
    {
        ViewBag.Filter = filter;
        ViewBag.Rooms = await rooms.GetLookupAsync(ct);
        return View(await meetings.QueryAsync(filter, CurrentUserId, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await meetings.GetAsync(id, CurrentUserId, ct);
        return model is null ? NotFound() : View(model);
    }

    [Authorize(Roles = "Organizer,Admin"), HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await PopulateLookupsAsync(ct);
        return View(new MeetingEditViewModel());
    }

    [Authorize(Roles = "Organizer,Admin"), HttpPost]
    public async Task<IActionResult> Create(MeetingEditViewModel model, CancellationToken ct)
    {
        model.MeetingId = 0;
        model.RowVersion = [];
        if (ModelState.IsValid)
        {
            var result = await meetings.SaveAsync(model, CurrentUserId, ct);
            if (result.Succeeded) return RedirectToAction(nameof(Details), new { id = result.EntityId });
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể tạo cuộc họp.");
        }
        await PopulateLookupsAsync(ct);
        return View(model);
    }

    [Authorize(Roles = "Organizer,Admin"), HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await meetings.GetAsync(id, CurrentUserId, ct);
        if (model is null) return NotFound();
        if (!User.IsInRole("Admin") && model.CreatedByUserId != CurrentUserId) return Forbid();
        await PopulateLookupsAsync(ct);
        return View(model);
    }

    [Authorize(Roles = "Organizer,Admin"), HttpPost]
    public async Task<IActionResult> Edit(int id, MeetingEditViewModel model, CancellationToken ct)
    {
        if (id <= 0 || id != model.MeetingId) return BadRequest();
        if (ModelState.IsValid)
        {
            var result = await meetings.SaveAsync(model, CurrentUserId, ct);
            if (result.Succeeded) return RedirectToAction(nameof(Details), new { id });
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật cuộc họp.");
        }
        await PopulateLookupsAsync(ct);
        return View(model);
    }

    [Authorize(Roles = "Organizer,Admin"), HttpGet]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var meeting = await meetings.GetAsync(id, CurrentUserId, ct);
        if (meeting is null) return NotFound();
        if (!User.IsInRole("Admin") && meeting.CreatedByUserId != CurrentUserId) return Forbid();
        ViewBag.Meeting = meeting;
        return View("Cancel", new CancelMeetingViewModel { MeetingId = id, RowVersion = meeting.RowVersion });
    }

    [Authorize(Roles = "Organizer,Admin"), HttpPost]
    public async Task<IActionResult> Cancel(int id, CancelMeetingViewModel model, CancellationToken ct)
    {
        if (id <= 0 || id != model.MeetingId) return BadRequest();
        if (ModelState.IsValid)
        {
            var result = await meetings.CancelAsync(model, CurrentUserId, ct);
            if (result.Succeeded) return RedirectToAction(nameof(Details), new { id });
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể hủy cuộc họp.");
        }
        ViewBag.Meeting = await meetings.GetAsync(id, CurrentUserId, ct);
        return View("Cancel", model);
    }

    [Authorize(Roles = "Organizer,Admin"), HttpGet]
    public Task<IActionResult> Delete(int id, CancellationToken ct) => Cancel(id, ct);

    [Authorize(Roles = "Organizer,Admin"), HttpPost, ActionName("Delete")]
    public Task<IActionResult> DeleteConfirmed(int id, CancelMeetingViewModel model, CancellationToken ct) => Cancel(id, model, ct);

    [HttpGet]
    public Task<IActionResult> Rsvp(int id, RsvpFilter filter, CancellationToken ct) => RsvpTracking(id, filter, ct);

    [HttpPost]
    public async Task<IActionResult> Respond(int meetingId, RsvpStatus status, CancellationToken ct)
    {
        if (!ModelState.IsValid || meetingId <= 0 || !Enum.IsDefined(status)) return BadRequest();
        var result = await rsvps.RespondAsync(meetingId, CurrentUserId, status, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã ghi nhận phản hồi tham dự." : result.Error;
        return RedirectToAction(nameof(Details), new { id = meetingId });
    }

    [HttpGet]
    public async Task<IActionResult> RsvpTracking(int id, RsvpFilter filter, CancellationToken ct)
    {
        if (id > 0) filter.MeetingId = id;
        if (filter.MeetingId <= 0) return BadRequest();
        var result = await rsvps.QueryAsync(filter, CurrentUserId, ct);
        if (result is null) return NotFound();
        ViewBag.Filter = filter;
        ViewBag.Meeting = await meetings.GetAsync(filter.MeetingId, CurrentUserId, ct);
        return View("RsvpTracking", result);
    }

    [HttpPost]
    public Task<IActionResult> Rsvp(int id, RsvpStatus status, CancellationToken ct) => Respond(id, status, ct);

    private async Task PopulateLookupsAsync(CancellationToken ct)
    {
        ViewBag.Rooms = await rooms.GetLookupAsync(ct);
        ViewBag.Users = await users.GetLookupAsync(ct);
        ViewBag.Participants = ViewBag.Users;
    }
}
