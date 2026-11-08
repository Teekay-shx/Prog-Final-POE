using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Payments;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Entry-fee payments for enrolments. Not one of Part 2's required minimum endpoint
/// groups, but implemented here because it was part of our own Part 1 endpoint plan
/// (docs/api-endpoint-plan.md), which Categories.EntryFee depends on to make sense.
/// </summary>
[ApiController]
[Route("api/enrolments/{enrolmentId:int}/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PaymentsController(RaceDayDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>Records a payment for an enrolment's entry fee. Only the enrolment's own participant may pay for it.</summary>
    /// <response code="201">Created payment.</response>
    /// <response code="403">The caller does not own this enrolment.</response>
    /// <response code="404">The enrolment does not exist.</response>
    [HttpPost]
    [Authorize(Roles = UserRole.Participant)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePayment(int enrolmentId, CreatePaymentDto dto)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null)
        {
            return NotFound(new { message = "Enrolment does not exist." });
        }
        if (enrolment.ParticipantId != _currentUser.UserId)
        {
            return Forbid();
        }

        var payment = new Payment
        {
            EnrolmentId = enrolmentId,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            Status = PaymentStatus.Pending
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPayment), new { enrolmentId }, ToDto(payment));
    }

    /// <summary>Returns the payment record for an enrolment. Visible to the enrolment's own participant or the event's organiser.</summary>
    /// <response code="200">The payment.</response>
    /// <response code="403">The caller is neither the participant nor the organiser.</response>
    /// <response code="404">No payment exists for this enrolment.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(int enrolmentId)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .Include(e => e.Payment)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment?.Payment is null)
        {
            return NotFound();
        }

        var isOwner = enrolment.ParticipantId == _currentUser.UserId;
        var isOrganiser = enrolment.Category!.Event!.OrganiserId == _currentUser.UserId;
        if (!isOwner && !isOrganiser)
        {
            return Forbid();
        }

        return Ok(ToDto(enrolment.Payment));
    }

    private static PaymentDto ToDto(Payment p) => new()
    {
        PaymentId = p.PaymentId,
        EnrolmentId = p.EnrolmentId,
        Amount = p.Amount,
        PaymentMethod = p.PaymentMethod,
        PaymentDate = p.PaymentDate,
        Status = p.Status
    };
}
