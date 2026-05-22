namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController(IDoctorService doctorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await doctorService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DoctorDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        DoctorDto? doctor = await doctorService.GetByIdAsync(id, cancellationToken);
        return doctor is null ? NotFound() : Ok(doctor);
    }

    [HttpGet("specialization/{specializationId:guid}")]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetBySpecialization(
        Guid specializationId,
        CancellationToken cancellationToken)
    {
        return Ok(await doctorService.GetBySpecializationAsync(specializationId, cancellationToken));
    }

    [HttpGet("top/{specializationId:guid}")]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetTopRated(
        Guid specializationId,
        [FromQuery] int take = 5,
        CancellationToken cancellationToken = default)
    {
        return Ok(await doctorService.GetTopRatedDoctorsAsync(specializationId, take, cancellationToken));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> Search(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Query parameter 'q' is required.");
        }

        return Ok(await doctorService.SearchDoctorsAsync(q, cancellationToken));
    }
}
