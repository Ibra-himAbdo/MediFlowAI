namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecializationsController(ISpecializationService specializationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpecializationDto>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<SpecializationDto> specializations = await specializationService.GetAllAsync(cancellationToken);
        return Ok(specializations);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<SpecializationDto>>> Search(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Query parameter 'q' is required.");
        }

        return Ok(await specializationService.SearchByNameAsync(q, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SpecializationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        SpecializationDto? specialization = await specializationService.GetByIdAsync(id, cancellationToken);

        if (specialization is null)
        {
            return NotFound();
        }

        return Ok(specialization);
    }
}