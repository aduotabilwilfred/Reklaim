namespace Reklaim.api.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;

    public ICollection<ItemPost> Posts { get; set; } = new List<ItemPost>();
    public ICollection<ClaimRequest> Claims { get; set; } = new List<ClaimRequest>();
}
