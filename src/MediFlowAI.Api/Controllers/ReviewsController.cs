namespace MediFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddReviewRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await reviewService.AddReviewAsync(
                request.AppointmentId,
                request.Rate,
                request.Comment,
                cancellationToken);

            return NoContent();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("doctor/{doctorId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetByDoctor(
        Guid doctorId,
        CancellationToken cancellationToken)
    {
        return Ok(await reviewService.GetDoctorReviewsAsync(doctorId, cancellationToken));
    }

    [HttpGet("doctor/{doctorId:guid}/average")]
    public async Task<ActionResult<double>> GetAverageRate(Guid doctorId, CancellationToken cancellationToken)
    {
        return Ok(await reviewService.GetDoctorAverageRateAsync(doctorId, cancellationToken));
    }
}
