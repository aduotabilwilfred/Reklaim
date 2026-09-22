using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reklaim.api.Data;
using Reklaim.api.Dtos;
using Reklaim.api.Models;
using System.Security.Claims;

namespace Reklaim.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly AppDbContext _db;
    private const int MaxPendingClaims = 3;

    public ClaimsController(AppDbContext db)
    {
        _db = db;
    }

    // POST /api/claims  — Submit a new claim
    [HttpPost]
    public async Task<IActionResult> SubmitClaim([FromBody] CreateClaimRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var post = await _db.Posts.FindAsync(request.PostId);
        if (post == null) return NotFound("Item post not found.");

        // Cannot claim your own post
        if (post.UserId == userId)
            return BadRequest("You cannot claim your own post.");

        // Post must be Active to be claimed
        if (post.Status != PostStatus.Active)
            return BadRequest("This item is no longer available for claiming.");

        // Enforce max 3 open/pending claims per user
        var pendingCount = await _db.Claims
            .CountAsync(c => c.ClaimerUserId == userId && c.Status == ClaimStatus.Pending);

        if (pendingCount >= MaxPendingClaims)
            return BadRequest($"You already have {MaxPendingClaims} pending claims. Wait for a response before submitting more.");

        // Prevent duplicate claims on the same post
        var existingClaim = await _db.Claims
            .AnyAsync(c => c.PostId == request.PostId && c.ClaimerUserId == userId && c.Status == ClaimStatus.Pending);
        if (existingClaim)
            return BadRequest("You already have a pending claim on this item.");

        var claim = new ClaimRequest
        {
            PostId = request.PostId,
            ClaimerUserId = userId.Value,
            ProofDescription = request.ProofDescription,
            Status = ClaimStatus.Pending
        };

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetClaimById), new { id = claim.Id }, new { claim.Id, Message = "Claim submitted. Wait for the finder to review it." });
    }

    // GET /api/claims/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClaimById(int id)
    {
        var userId = GetCurrentUserId();

        var claim = await _db.Claims
            .Include(c => c.Post)
            .Include(c => c.ClaimerUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim == null) return NotFound();

        // Only the claimer or the post owner can view a claim
        if (claim.ClaimerUserId != userId && claim.Post.UserId != userId)
            return Forbid();

        return Ok(MapToResponse(claim));
    }

    // GET /api/claims/my-claims  — Get all claims submitted by the logged-in user
    [HttpGet("my-claims")]
    public async Task<IActionResult> GetMyClaims()
    {
        var userId = GetCurrentUserId();

        var claims = await _db.Claims
            .Include(c => c.Post)
            .Include(c => c.ClaimerUser)
            .Where(c => c.ClaimerUserId == userId)
            .ToListAsync();

        return Ok(claims.Select(MapToResponse));
    }

    // GET /api/claims/on-my-posts  — Get all incoming claims on posts owned by the logged-in user (finder's inbox)
    [HttpGet("on-my-posts")]
    public async Task<IActionResult> GetClaimsOnMyPosts()
    {
        var userId = GetCurrentUserId();

        var claims = await _db.Claims
            .Include(c => c.Post)
            .Include(c => c.ClaimerUser)
            .Where(c => c.Post.UserId == userId)
            .ToListAsync();

        return Ok(claims.Select(MapToResponse));
    }

    // POST /api/claims/{id}/review  — Finder approves or denies a claim
    [HttpPost("{id}/review")]
    public async Task<IActionResult> ReviewClaim(int id, [FromBody] ReviewClaimRequest request)
    {
        var userId = GetCurrentUserId();

        var claim = await _db.Claims
            .Include(c => c.Post)
            .Include(c => c.ClaimerUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim == null) return NotFound();

        // Only the finder (post owner) can approve or deny
        if (claim.Post.UserId != userId)
            return Forbid();

        if (claim.Status != ClaimStatus.Pending)
            return BadRequest("This claim has already been reviewed.");

        if (request.Approve)
        {
            claim.Status = ClaimStatus.Approved;

            // Mark the post as Claimed so others can't claim it
            claim.Post.Status = PostStatus.Claimed;

            // Fetch the finder's phone to include in the response
            var finder = await _db.Users.FindAsync(userId);

            await _db.SaveChangesAsync();

            // On approval: reveal both phone numbers
            return Ok(new ApprovedClaimResponse
            {
                Id = claim.Id,
                PostId = claim.PostId,
                PostTitle = claim.Post.Title,
                ClaimerUserId = claim.ClaimerUserId,
                ClaimerName = claim.ClaimerUser.Name,
                ProofDescription = claim.ProofDescription,
                Status = claim.Status.ToString(),
                ClaimerPhone = claim.ClaimerUser.PhoneNumber ?? "Not provided",
                FinderPhone = finder?.PhoneNumber ?? "Not provided"
            });
        }
        else
        {
            claim.Status = ClaimStatus.Denied;
            await _db.SaveChangesAsync();

            return Ok(new ClaimResponse
            {
                Id = claim.Id,
                PostId = claim.PostId,
                PostTitle = claim.Post.Title,
                ClaimerUserId = claim.ClaimerUserId,
                ClaimerName = claim.ClaimerUser.Name,
                ProofDescription = claim.ProofDescription,
                Status = claim.Status.ToString()
            });
        }
    }

    // Helper: map a ClaimRequest entity to a safe ClaimResponse DTO (no phone numbers)
    private static ClaimResponse MapToResponse(ClaimRequest claim) => new()
    {
        Id = claim.Id,
        PostId = claim.PostId,
        PostTitle = claim.Post?.Title ?? string.Empty,
        ClaimerUserId = claim.ClaimerUserId,
        ClaimerName = claim.ClaimerUser?.Name ?? string.Empty,
        ProofDescription = claim.ProofDescription,
        Status = claim.Status.ToString()
    };

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }
}
