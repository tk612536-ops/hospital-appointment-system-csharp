namespace HospitalAppointments.Domain.Scheduling;

public sealed record TimeInterval
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public TimeInterval(DateTime start, DateTime end)
    {
        DomainRules.Utc(start);
        DomainRules.Utc(end);
        if (start >= end) throw new ArgumentException("End must follow start.");
        Start = start;
        End = end;
    }

    public bool Overlaps(TimeInterval other) => Start < other.End && other.Start < End;
    public bool Contains(TimeInterval other) => Start <= other.Start && other.End <= End;

    public bool IsCoveredBy(IEnumerable<TimeInterval> windows)
    {
        var cursor = Start;
        foreach (var window in windows.OrderBy(w => w.Start))
        {
            if (window.End <= cursor) continue;
            if (window.Start > cursor) return false;
            cursor = window.End;
            if (cursor >= End) return true;
        }
        return false;
    }
}
