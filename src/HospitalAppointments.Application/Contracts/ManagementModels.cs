using HospitalAppointments.Domain.Facilities;

namespace HospitalAppointments.Application.Contracts;

public sealed record OpeningPeriod(TimeOnly? Opens, TimeOnly? Closes);
public sealed record FacilityRow(long Id, string Name, FacilityType Type);
public sealed record BranchRow(long Id, string Name);
public sealed record DepartmentRow(long Id, long BranchId, string Branch, string Name);
public sealed record ServiceRow(long Id, long DepartmentId, string Department, string Branch, string Name, int Duration, decimal Fee, bool Active);
public sealed record StaffRow(long Id, string Name, string Profession, bool IsDoctor, long? SpecialtyId, string? JobTitle, bool Active, bool HasLogin, string Branches);
public sealed record OfferingRow(long Id, string Staff, string Service, string Branch, bool Active);
public sealed record OpeningRow(long BranchId, DayOfWeek Day, TimeOnly Opens, TimeOnly Closes);
public sealed record AvailabilityRow(long Id, long StaffId, long BranchId, string Staff, string Branch, DateOnly Date, TimeOnly Starts, TimeOnly Ends);
public sealed record ManagementPage(FacilityRow Facility, IReadOnlyList<BranchRow> Branches,
    IReadOnlyList<DepartmentRow> Departments, IReadOnlyList<ServiceRow> Services, IReadOnlyList<StaffRow> Staff,
    IReadOnlyList<Choice> Specialties, IReadOnlyList<OfferingRow> Offerings, IReadOnlyList<OpeningRow> Opening,
    IReadOnlyList<AvailabilityRow> Availability, DateOnly SuggestedDate);

public sealed class ConfigurationException(string message) : Exception(message);
