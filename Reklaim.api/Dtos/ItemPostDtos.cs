using Reklaim.api.Models;

namespace Reklaim.api.Dtos;

// --- Request DTOs ---

public class CreateItemPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LocationFound { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public PostType PostType { get; set; }
    public IFormFile? Image { get; set; }
}

public class UpdateItemPostStatusRequest
{
    public PostStatus Status { get; set; }
}

// --- Response DTOs ---

public class ItemPostResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LocationFound { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PostType { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime DatePosted { get; set; }
    public string Status { get; set; } = string.Empty;
    public int PostedByUserId { get; set; }
    public string PostedByName { get; set; } = string.Empty;
}
