namespace HospitalAppointments.Domain;

internal static class DomainRules
{
    internal static string Text(string value, int maximum = 255)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Trim().Length > maximum) throw new ArgumentException("Text exceeds the permitted length.");
        return value.Trim();
    }

    internal static DateTime Utc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc) throw new ArgumentException("An explicit UTC instant is required.");
        return value;
    }

    internal static decimal Fee(decimal value)
    {
        if (value < 0 || value > 9_999_999_999.99m || decimal.Truncate(value * 100) != value * 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Fee must fit exact nonnegative minor units.");
        return value;
    }
}
