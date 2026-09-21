namespace Reklaim.api.Models;

public class ClaimRequest
{
    public int Id { get; set; }
    public string ProofDescription { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

    public int PostId { get; set; }
    public ItemPost Post { get; set; } = null!;

    public int ClaimerUserId { get; set; }
    public User ClaimerUser { get; set; } = null!;
}
