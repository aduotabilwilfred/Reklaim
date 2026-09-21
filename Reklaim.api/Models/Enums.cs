namespace Reklaim.api.Models;

public enum PostType
{
    Lost,
    Found
}

public enum PostStatus
{
    Active,
    Claimed,
    Resolved
}

public enum ClaimStatus
{
    Pending,
    Approved,
    Denied
}
