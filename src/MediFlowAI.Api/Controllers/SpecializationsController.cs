namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/specializations")]
public class SpecializationsController(ISpecializationService specializationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpecializationDto>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<SpecializationDto> specializations = await specializationService.GetAllAsync(cancellationToken);
        return Ok(specializations);
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