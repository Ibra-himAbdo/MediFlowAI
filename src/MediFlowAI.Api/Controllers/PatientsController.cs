namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController(IPatientService patientService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PatientDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await patientService.GetAllAsync(cancellationToken));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<PatientDto>>> Search(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Query parameter 'q' is required.");
        }

        return Ok(await patientService.SearchAsync(q, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        PatientDto? patient = await patientService.GetByIdAsync(id, cancellationToken);

        if (patient is null)
        {
            return NotFound();
        }

        return Ok(patient);
    }
}
