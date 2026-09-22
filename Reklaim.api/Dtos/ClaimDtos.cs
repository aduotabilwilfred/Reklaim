namespace Reklaim.api.Dtos;

// --- Request DTOs ---

public class CreateClaimRequest
{
    public int PostId { get; set; }
    public string ProofDescription { get; set; } = string.Empty;
}

public class ReviewClaimRequest
{
    public bool Approve { get; set; }
}

// --- Response DTOs ---

public class ClaimResponse
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string PostTitle { get; set; } = string.Empty;
    public int ClaimerUserId { get; set; }
    public string ClaimerName { get; set; } = string.Empty;
    public string ProofDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ApprovedClaimResponse : ClaimResponse
{
    // Only revealed after approval
    public string ClaimerPhone { get; set; } = string.Empty;
    public string FinderPhone { get; set; } = string.Empty;
}
