namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController(IScheduleService scheduleService) : ControllerBase
{
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<DoctorSlotDto>>> GetAvailable(
        [FromQuery] Guid doctorId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        return Ok(await scheduleService.GetAvailableSlotsAsync(doctorId, date, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DoctorSlotDto>>> GetSchedule(
        [FromQuery] Guid doctorId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (to < from)
        {
            return BadRequest("'to' date must be on or after 'from' date.");
        }

        return Ok(await scheduleService.GetDoctorScheduleAsync(doctorId, from, to, cancellationToken));
    }

    [HttpGet("{scheduleId:guid}/available")]
    public async Task<ActionResult<bool>> IsAvailable(Guid scheduleId, CancellationToken cancellationToken)
    {
        bool available = await scheduleService.IsSlotAvailableAsync(scheduleId, cancellationToken);
        return Ok(available);
    }
}
