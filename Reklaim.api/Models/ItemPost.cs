namespace Reklaim.api.Models;

public class ItemPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LocationFound { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Electronics, Keys, Documents, etc.
    public PostType PostType { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime DatePosted { get; set; } = DateTime.UtcNow;
    public PostStatus Status { get; set; } = PostStatus.Active;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<ClaimRequest> Claims { get; set; } = new List<ClaimRequest>();
}
