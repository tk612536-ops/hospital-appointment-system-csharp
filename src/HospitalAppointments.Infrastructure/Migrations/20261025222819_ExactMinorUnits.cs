using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalAppointments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExactMinorUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Service_Terms",
                table: "HealthcareServices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Appointment_Terms",
                table: "Appointments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Service_Terms",
                table: "HealthcareServices",
                sql: "typeof(DurationMinutes) = 'integer' AND DurationMinutes BETWEEN 1 AND 1439 AND typeof(FeeMinor) = 'integer' AND FeeMinor BETWEEN 0 AND 999999999999 AND Currency = 'TRY'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Appointment_Terms",
                table: "Appointments",
                sql: "Start < End AND typeof(BookedDurationMinutes) = 'integer' AND BookedDurationMinutes BETWEEN 1 AND 1439 AND End - Start = BookedDurationMinutes * 600000000 AND typeof(BookedFeeMinor) = 'integer' AND BookedFeeMinor BETWEEN 0 AND 999999999999 AND BookedCurrency = 'TRY'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Service_Terms",
                table: "HealthcareServices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Appointment_Terms",
                table: "Appointments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Service_Terms",
                table: "HealthcareServices",
                sql: "DurationMinutes BETWEEN 1 AND 1439 AND FeeMinor BETWEEN 0 AND 999999999999 AND Currency = 'TRY'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Appointment_Terms",
                table: "Appointments",
                sql: "Start < End AND BookedDurationMinutes BETWEEN 1 AND 1439 AND End - Start = BookedDurationMinutes * 600000000 AND BookedFeeMinor BETWEEN 0 AND 999999999999 AND BookedCurrency = 'TRY'");
        }
    }
}
