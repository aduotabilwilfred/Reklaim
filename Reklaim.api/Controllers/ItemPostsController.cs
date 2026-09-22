using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reklaim.api.Data;
using Reklaim.api.Dtos;
using Reklaim.api.Models;
using Reklaim.api.Services;
using System.Security.Claims;

namespace Reklaim.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require a valid JWT
public class ItemPostsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public ItemPostsController(AppDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    // GET /api/itemposts?type=Lost&category=Electronics&status=Active&search=keys
    [HttpGet]
    [AllowAnonymous] // The Hub feed is publicly viewable
    public async Task<IActionResult> GetAll(
        [FromQuery] string? type,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? search)
    {
        var query = _db.Posts.Include(p => p.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<PostType>(type, true, out var postType))
            query = query.Where(p => p.PostType == postType);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category.ToLower() == category.ToLower());

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PostStatus>(status, true, out var postStatus))
            query = query.Where(p => p.Status == postStatus);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search));

        var posts = await query
            .OrderByDescending(p => p.DatePosted)
            .Select(p => new ItemPostResponse
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                LocationFound = p.LocationFound,
                Category = p.Category,
                PostType = p.PostType.ToString(),
                ImageUrl = p.ImageUrl,
                DatePosted = p.DatePosted,
                Status = p.Status.ToString(),
                PostedByUserId = p.UserId,
                PostedByName = p.User.Name
            })
            .ToListAsync();

        return Ok(posts);
    }

    // GET /api/itemposts/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await _db.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return NotFound();

        return Ok(new ItemPostResponse
        {
            Id = post.Id,
            Title = post.Title,
            Description = post.Description,
            LocationFound = post.LocationFound,
            Category = post.Category,
            PostType = post.PostType.ToString(),
            ImageUrl = post.ImageUrl,
            DatePosted = post.DatePosted,
            Status = post.Status.ToString(),
            PostedByUserId = post.UserId,
            PostedByName = post.User.Name
        });
    }

    // POST /api/itemposts  (multipart/form-data for image upload)
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateItemPostRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        string? imageUrl = null;
        if (request.Image != null)
        {
            try
            {
                imageUrl = await _fileStorage.UploadFileAsync(request.Image);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        var post = new ItemPost
        {
            Title = request.Title,
            Description = request.Description,
            LocationFound = request.LocationFound,
            Category = request.Category,
            PostType = request.PostType,
            ImageUrl = imageUrl,
            UserId = userId.Value
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, new { post.Id });
    }

    // PATCH /api/itemposts/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateItemPostStatusRequest request)
    {
        var userId = GetCurrentUserId();
        var post = await _db.Posts.FindAsync(id);

        if (post == null) return NotFound();
        if (post.UserId != userId) return Forbid(); // Only the poster can update status

        post.Status = request.Status;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/itemposts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var post = await _db.Posts.FindAsync(id);

        if (post == null) return NotFound();
        if (post.UserId != userId) return Forbid();

        if (!string.IsNullOrEmpty(post.ImageUrl))
            await _fileStorage.DeleteFileAsync(post.ImageUrl);

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }
}
