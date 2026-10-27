using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Catalog;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Patients;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Identity;
using HospitalAppointments.Infrastructure.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HospitalAppointments.Infrastructure.Persistence;

internal static class BookingModelConfiguration
{
    private static readonly ValueConverter<DateTime, long> UtcTicks = new(v => v.Ticks, v => new DateTime(v, DateTimeKind.Utc));
    private static readonly ValueConverter<decimal, long> MinorUnits = new(v => checked((long)(v * 100m)), v => v / 100m);

    internal static void Configure(ModelBuilder b)
    {
        b.Entity<Facility>(e =>
        {
            e.ToTable("Facilities"); e.HasKey(x => x.Id); e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Property(x => x.Type).HasConversion<string>().IsRequired();
            e.ToTable(t => t.HasCheckConstraint("CK_Facility_Type", "Type IN ('Hospital','Clinic')"));
        });
        b.Entity<Branch>(e =>
        {
            e.ToTable("Branches"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.HasIndex(x => new { x.FacilityId, x.Name }).IsUnique();
            e.HasOne(x => x.Facility).WithMany().HasForeignKey(x => x.FacilityId);
        });
        b.Entity<Department>(e =>
        {
            e.ToTable("Departments"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.HasIndex(x => new { x.BranchId, x.Name }).IsUnique();
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
        });
        b.Entity<OpeningWindow>(e =>
        {
            e.ToTable("OpeningWindows", t => t.HasCheckConstraint("CK_Opening_Positive", "OpensAt < ClosesAt"));
            e.HasKey(x => x.Id); e.Property(x => x.Weekday).HasConversion<string>();
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
            e.HasIndex(x => new { x.BranchId, x.Weekday });
        });
        b.Entity<HealthcareService>(e =>
        {
            e.ToTable("HealthcareServices", t =>
            {
                t.HasCheckConstraint("CK_Service_Terms", "typeof(DurationMinutes) = 'integer' AND DurationMinutes BETWEEN 1 AND 1439 AND typeof(FeeMinor) = 'integer' AND FeeMinor BETWEEN 0 AND 999999999999 AND Currency = 'TRY'");
            });
            e.HasKey(x => x.Id); e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Property(x => x.Fee).HasConversion(MinorUnits).HasColumnName("FeeMinor");
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        });
        b.Entity<Specialty>(e =>
        {
            e.ToTable("Specialties"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(255); e.HasIndex(x => x.Name).IsUnique();
        });
        b.Entity<StaffMember>(e =>
        {
            e.ToTable("StaffMembers", t => t.HasCheckConstraint("CK_Staff_Subtype",
                "(StaffType = 'Doctor' AND SpecialtyId IS NOT NULL AND JobTitle IS NULL) OR (StaffType = 'HealthcareStaff' AND SpecialtyId IS NULL AND length(trim(JobTitle)) > 0)"));
            e.HasKey(x => x.Id); e.Property(x => x.Name).IsRequired().HasMaxLength(255);
            e.Ignore(x => x.ProfessionalDescription);
            e.HasDiscriminator<string>("StaffType").HasValue<Doctor>("Doctor").HasValue<HealthcareStaff>("HealthcareStaff");
            e.HasOne(x => x.Facility).WithMany().HasForeignKey(x => x.FacilityId);
            e.HasOne<ApplicationUser>().WithOne().HasForeignKey<StaffMember>(x => x.AccountId);
            e.HasIndex(x => x.AccountId).IsUnique();
        });
        b.Entity<Doctor>().HasOne(x => x.Specialty).WithMany().HasForeignKey(x => x.SpecialtyId);
        b.Entity<HealthcareStaff>().Property(x => x.JobTitle).HasMaxLength(255);
        b.Entity<StaffOffering>(e =>
        {
            e.ToTable("StaffOfferings"); e.HasKey(x => x.Id); e.Ignore(x => x.Branch); e.Ignore(x => x.IsBookable);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId);
            e.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId);
            e.HasIndex(x => new { x.StaffId, x.ServiceId }).IsUnique();
        });
        b.Entity<AvailabilityWindow>(e =>
        {
            e.ToTable("AvailabilityWindows", t => t.HasCheckConstraint("CK_Availability_Positive", "Start < End"));
            e.HasKey(x => x.Id); e.Ignore(x => x.Interval);
            e.Property(x => x.Start).HasConversion(UtcTicks); e.Property(x => x.End).HasConversion(UtcTicks);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
            e.HasIndex(x => new { x.StaffId, x.BranchId, x.Start });
        });
        b.Entity<Patient>(e =>
        {
            e.ToTable("Patients"); e.HasKey(x => x.Id); e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.HasOne<ApplicationUser>().WithOne().HasForeignKey<Patient>(x => x.AccountId);
            e.HasIndex(x => x.AccountId).IsUnique();
        });
        b.Entity<ManagerFacilityGrant>(e =>
        {
            e.ToTable("ManagerFacilityGrants"); e.HasKey(x => new { x.AccountId, x.FacilityId });
            e.HasOne(x => x.Facility).WithMany().HasForeignKey(x => x.FacilityId);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AccountId);
        });
        b.Entity<DemoSeedMarker>().ToTable("DemoSeedMarkers").HasKey(x => x.Key);
        b.Entity<Appointment>(e =>
        {
            e.ToTable("Appointments", t =>
            {
                t.HasCheckConstraint("CK_Appointment_Terms", "Start < End AND typeof(BookedDurationMinutes) = 'integer' AND BookedDurationMinutes BETWEEN 1 AND 1439 AND End - Start = BookedDurationMinutes * 600000000 AND typeof(BookedFeeMinor) = 'integer' AND BookedFeeMinor BETWEEN 0 AND 999999999999 AND BookedCurrency = 'TRY'");
                t.HasCheckConstraint("CK_Appointment_Decision", "(Status = 'Pending' AND DecidedByAccountId IS NULL AND DecidedAt IS NULL) OR (Status IN ('Approved','Rejected') AND DecidedByAccountId IS NOT NULL AND DecidedAt IS NOT NULL)");
            });
            e.HasKey(x => x.Id); e.Ignore(x => x.Interval);
            e.Property(x => x.Start).HasConversion(UtcTicks); e.Property(x => x.End).HasConversion(UtcTicks);
            e.Property(x => x.CreatedAt).HasConversion(UtcTicks); e.Property(x => x.DecidedAt).HasConversion(UtcTicks);
            e.Property(x => x.BookedFee).HasConversion(MinorUnits).HasColumnName("BookedFeeMinor");
            e.Property(x => x.Status).HasConversion<string>(); e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.BookedCurrency).IsRequired().HasMaxLength(3);
            e.Property(x => x.BookedServiceName).IsRequired().HasMaxLength(255);
            e.Property(x => x.BookedStaffName).IsRequired().HasMaxLength(255);
            e.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId);
            e.HasOne(x => x.Offering).WithMany().HasForeignKey(x => x.OfferingId);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DecidedByAccountId);
            e.HasIndex(x => new { x.PatientId, x.Status, x.Start });
            e.HasIndex(x => new { x.OfferingId, x.Status, x.Start });
        });
        foreach (var entity in b.Model.GetEntityTypes().Where(e => e.ClrType.Namespace?.StartsWith("HospitalAppointments.Domain", StringComparison.Ordinal) == true))
            foreach (var foreignKey in entity.GetForeignKeys()) foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
