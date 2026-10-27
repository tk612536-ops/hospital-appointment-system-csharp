using HospitalAppointments.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<long>, long>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        BookingModelConfiguration.Configure(builder);
        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.UserName).HasMaxLength(80).IsRequired();
            user.Property(u => u.NormalizedUserName).HasMaxLength(80).IsRequired();
            user.Property(u => u.DisplayName).HasMaxLength(120).IsRequired();
            user.ToTable("AspNetUsers", table =>
            {
                table.HasCheckConstraint("CK_Users_UserName", "length(UserName) BETWEEN 1 AND 80 AND length(NormalizedUserName) BETWEEN 1 AND 80");
                table.HasCheckConstraint("CK_Users_DisplayName", "length(trim(DisplayName)) BETWEEN 1 AND 120");
            });
        });
    }
}
