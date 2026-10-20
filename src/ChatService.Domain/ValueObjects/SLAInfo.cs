namespace ChatService.Domain.ValueObjects;

public sealed class SLAInfo
{
    public DateTime? FirstResponseDueUtc { get; private set; }
    public DateTime? ResolutionDueUtc { get; private set; }

    public static SLAInfo Empty() => new();

    private SLAInfo()
    {
    }

    public SLAInfo(DateTime? firstResponseDueUtc, DateTime? resolutionDueUtc)
    {
        FirstResponseDueUtc = firstResponseDueUtc;
        ResolutionDueUtc = resolutionDueUtc;
    }

    public void SetDeadlines(DateTime? firstResponseDueUtc, DateTime? resolutionDueUtc)
    {
        FirstResponseDueUtc = firstResponseDueUtc;
        ResolutionDueUtc = resolutionDueUtc;
    }
}
