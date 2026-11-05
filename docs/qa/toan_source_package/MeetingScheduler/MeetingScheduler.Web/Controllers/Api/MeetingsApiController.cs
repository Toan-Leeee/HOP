using MeetingScheduler.Web.Services.Interfaces;
using MeetingScheduler.Web.ViewModels.Meetings;
using Microsoft.AspNetCore.Mvc;

namespace MeetingScheduler.Web.Controllers.Api;

[Route("api/meetings")]
public class MeetingsApiController(IMeetingService meetings) : AuthenticatedController
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] MeetingFilter filter, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return Ok(await meetings.QueryAsync(filter, CurrentUserId, ct));
    }
}
