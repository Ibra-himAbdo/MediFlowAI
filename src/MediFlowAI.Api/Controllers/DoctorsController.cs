namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController(IDoctorService doctorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetAll(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        return Ok(await doctorService.GetAllAsync(date, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DoctorDto>> GetById(
        Guid id,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        DoctorDto? doctor = await doctorService.GetByIdAsync(id, date, cancellationToken);
        return doctor is null ? NotFound() : Ok(doctor);
    }

    [HttpGet("specialization/{specializationId:guid}")]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetBySpecialization(
        Guid specializationId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        return Ok(await doctorService.GetBySpecializationAsync(specializationId, date, cancellationToken));
    }

    [HttpGet("specialization/name/{name}")]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetBySpecializationName(
        string name,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        return Ok(await doctorService.GetBySpecializationNameAsync(name, date, cancellationToken));
    }

    [HttpGet("top/{specializationId:guid}")]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetTopRated(
        Guid specializationId,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 100)] int take = 5,
        [FromQuery] DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await doctorService.GetTopRatedDoctorsAsync(specializationId, take, date, cancellationToken));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> Search(
        [FromQuery] string q,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Query parameter 'q' is required.");
        }

        return Ok(await doctorService.SearchDoctorsAsync(q, date, cancellationToken));
    }
}
