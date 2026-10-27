using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalAppointments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoSeedMarkers",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoSeedMarkers", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.Id);
                    table.CheckConstraint("CK_Facility_Type", "Type IN ('Hospital','Clinic')");
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Specialties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FacilityId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManagerFacilityGrants",
                columns: table => new
                {
                    AccountId = table.Column<long>(type: "INTEGER", nullable: false),
                    FacilityId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerFacilityGrants", x => new { x.AccountId, x.FacilityId });
                    table.ForeignKey(
                        name: "FK_ManagerFacilityGrants_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerFacilityGrants_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FacilityId = table.Column<long>(type: "INTEGER", nullable: false),
                    AccountId = table.Column<long>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    StaffType = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    SpecialtyId = table.Column<long>(type: "INTEGER", nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.Id);
                    table.CheckConstraint("CK_Staff_Subtype", "(StaffType = 'Doctor' AND SpecialtyId IS NOT NULL AND JobTitle IS NULL) OR (StaffType = 'HealthcareStaff' AND SpecialtyId IS NULL AND length(trim(JobTitle)) > 0)");
                    table.ForeignKey(
                        name: "FK_StaffMembers_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffMembers_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffMembers_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BranchId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpeningWindows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BranchId = table.Column<long>(type: "INTEGER", nullable: false),
                    Weekday = table.Column<string>(type: "TEXT", nullable: false),
                    OpensAt = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    ClosesAt = table.Column<TimeOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpeningWindows", x => x.Id);
                    table.CheckConstraint("CK_Opening_Positive", "OpensAt < ClosesAt");
                    table.ForeignKey(
                        name: "FK_OpeningWindows_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AvailabilityWindows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StaffId = table.Column<long>(type: "INTEGER", nullable: false),
                    BranchId = table.Column<long>(type: "INTEGER", nullable: false),
                    Start = table.Column<long>(type: "INTEGER", nullable: false),
                    End = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityWindows", x => x.Id);
                    table.CheckConstraint("CK_Availability_Positive", "Start < End");
                    table.ForeignKey(
                        name: "FK_AvailabilityWindows_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AvailabilityWindows_StaffMembers_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HealthcareServices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartmentId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    FeeMinor = table.Column<long>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthcareServices", x => x.Id);
                    table.CheckConstraint("CK_Service_Terms", "DurationMinutes BETWEEN 1 AND 1439 AND FeeMinor BETWEEN 0 AND 999999999999 AND Currency = 'TRY'");
                    table.ForeignKey(
                        name: "FK_HealthcareServices_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffOfferings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StaffId = table.Column<long>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<long>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffOfferings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffOfferings_HealthcareServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "HealthcareServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffOfferings_StaffMembers_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<long>(type: "INTEGER", nullable: false),
                    OfferingId = table.Column<long>(type: "INTEGER", nullable: false),
                    Start = table.Column<long>(type: "INTEGER", nullable: false),
                    End = table.Column<long>(type: "INTEGER", nullable: false),
                    BookedDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    BookedFeeMinor = table.Column<long>(type: "INTEGER", nullable: false),
                    BookedCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    BookedServiceName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    BookedStaffName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    DecidedByAccountId = table.Column<long>(type: "INTEGER", nullable: true),
                    DecidedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.CheckConstraint("CK_Appointment_Decision", "(Status = 'Pending' AND DecidedByAccountId IS NULL AND DecidedAt IS NULL) OR (Status IN ('Approved','Rejected') AND DecidedByAccountId IS NOT NULL AND DecidedAt IS NOT NULL)");
                    table.CheckConstraint("CK_Appointment_Terms", "Start < End AND BookedDurationMinutes BETWEEN 1 AND 1439 AND End - Start = BookedDurationMinutes * 600000000 AND BookedFeeMinor BETWEEN 0 AND 999999999999 AND BookedCurrency = 'TRY'");
                    table.ForeignKey(
                        name: "FK_Appointments_AspNetUsers_DecidedByAccountId",
                        column: x => x.DecidedByAccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_StaffOfferings_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "StaffOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DecidedByAccountId",
                table: "Appointments",
                column: "DecidedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OfferingId_Status_Start",
                table: "Appointments",
                columns: new[] { "OfferingId", "Status", "Start" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId_Status_Start",
                table: "Appointments",
                columns: new[] { "PatientId", "Status", "Start" });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityWindows_BranchId",
                table: "AvailabilityWindows",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityWindows_StaffId_BranchId_Start",
                table: "AvailabilityWindows",
                columns: new[] { "StaffId", "BranchId", "Start" });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_FacilityId_Name",
                table: "Branches",
                columns: new[] { "FacilityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_BranchId_Name",
                table: "Departments",
                columns: new[] { "BranchId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_Name",
                table: "Facilities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthcareServices_DepartmentId",
                table: "HealthcareServices",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerFacilityGrants_FacilityId",
                table: "ManagerFacilityGrants",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_OpeningWindows_BranchId_Weekday",
                table: "OpeningWindows",
                columns: new[] { "BranchId", "Weekday" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_AccountId",
                table: "Patients",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_Name",
                table: "Specialties",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_AccountId",
                table: "StaffMembers",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_FacilityId",
                table: "StaffMembers",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_SpecialtyId",
                table: "StaffMembers",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffOfferings_ServiceId",
                table: "StaffOfferings",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffOfferings_StaffId_ServiceId",
                table: "StaffOfferings",
                columns: new[] { "StaffId", "ServiceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "AvailabilityWindows");

            migrationBuilder.DropTable(
                name: "DemoSeedMarkers");

            migrationBuilder.DropTable(
                name: "ManagerFacilityGrants");

            migrationBuilder.DropTable(
                name: "OpeningWindows");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "StaffOfferings");

            migrationBuilder.DropTable(
                name: "HealthcareServices");

            migrationBuilder.DropTable(
                name: "StaffMembers");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Specialties");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Facilities");
        }
    }
}
