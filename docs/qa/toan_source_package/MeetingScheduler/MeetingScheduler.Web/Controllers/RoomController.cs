using MeetingScheduler.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Controllers;

public class RoomController(IRoomAvailabilityService availability) : AuthenticatedController
{
    [HttpGet]
    public async Task<IActionResult> Available(DateTime start, DateTime end, int minimumCapacity = 1,
        int? excludeMeetingId = null, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || start == default || end <= start || minimumCapacity <= 0 || excludeMeetingId <= 0)
            return BadRequest(new { error = "Vui lòng nhập thời gian bắt đầu, kết thúc và sức chứa hợp lệ." });
        return Json(await availability.GetAvailableAsync(start, end, minimumCapacity, excludeMeetingId, ct));
    }
}
