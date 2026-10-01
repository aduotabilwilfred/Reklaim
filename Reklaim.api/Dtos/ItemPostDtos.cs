using Reklaim.api.Models;
using System.ComponentModel.DataAnnotations;

namespace Reklaim.api.Dtos;

// --- Request DTOs ---

public class ItemPostQuery : IValidatableObject
{
    [EnumDataType(typeof(PostType))]
    public PostType? Type { get; set; }
    public string? Category { get; set; }
    [EnumDataType(typeof(PostStatus))]
    public PostStatus? Status { get; set; }
    public string? Search { get; set; }
    public string? Location { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom.HasValue && DateTo.HasValue && DateFrom > DateTo)
            yield return new ValidationResult("DateFrom must be on or before DateTo.",
                new[] { nameof(DateFrom), nameof(DateTo) });
    }
}

public class CreateItemPostRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    [Required]
    public string LocationFound { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; } = string.Empty;
    [EnumDataType(typeof(PostType))]
    public PostType PostType { get; set; }
    public IFormFile? Image { get; set; }
}

public class UpdateItemPostStatusRequest
{
    [Required]
    [EnumDataType(typeof(PostStatus))]
    public PostStatus? Status { get; set; }
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
