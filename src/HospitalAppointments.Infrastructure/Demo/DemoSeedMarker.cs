namespace HospitalAppointments.Infrastructure.Demo;

/// <summary>Infrastructure-only fixture marker, independent of editable names.</summary>
public sealed class DemoSeedMarker
{
    public string Key { get; private set; } = "";
    private DemoSeedMarker() { }
    public DemoSeedMarker(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > 80) throw new ArgumentException("Seed key is too long.");
        Key = key;
    }
}
