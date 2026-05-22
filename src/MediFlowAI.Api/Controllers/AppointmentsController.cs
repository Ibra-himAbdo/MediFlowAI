namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpPost("book")]
    public async Task<ActionResult<BookingResultDto>> Book(
        [FromBody] BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        BookingResultDto result = await appointmentService.BookAppointmentAsync(
            request.PatientId,
            request.DoctorScheduleId,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await appointmentService.CancelAppointmentAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await appointmentService.CompleteAppointmentAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        AppointmentDto? appointment = await appointmentService.GetByIdAsync(id, cancellationToken);
        return appointment is null ? NotFound() : Ok(appointment);
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetByPatient(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return Ok(await appointmentService.GetPatientAppointmentsAsync(patientId, cancellationToken));
    }

    [HttpGet("doctor/{doctorId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetByDoctor(
        Guid doctorId,
        CancellationToken cancellationToken)
    {
        return Ok(await appointmentService.GetDoctorAppointmentsAsync(doctorId, cancellationToken));
    }
}
