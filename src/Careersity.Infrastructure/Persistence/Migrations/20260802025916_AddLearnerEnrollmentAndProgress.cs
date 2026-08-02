using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Careersity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerEnrollmentAndProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerPathwayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EnrolledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PausedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WithdrawnAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerEnrollments", x => x.Id);
                    table.CheckConstraint("CK_CareerEnrollments_CompletedAfterEnrollment", "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"EnrolledAtUtc\"");
                    table.CheckConstraint("CK_CareerEnrollments_PausedAfterEnrollment", "\"PausedAtUtc\" IS NULL OR \"PausedAtUtc\" >= \"EnrolledAtUtc\"");
                    table.CheckConstraint("CK_CareerEnrollments_WithdrawnAfterEnrollment", "\"WithdrawnAtUtc\" IS NULL OR \"WithdrawnAtUtc\" >= \"EnrolledAtUtc\"");
                    table.ForeignKey(
                        name: "FK_CareerEnrollments_CareerPathways_CareerPathwayId",
                        column: x => x.CareerPathwayId,
                        principalTable: "CareerPathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerEnrollments_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerEnrollments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseProgressRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseProgressRecords", x => x.Id);
                    table.CheckConstraint("CK_CourseProgress_CompletedAfterStarted", "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\"");
                    table.ForeignKey(
                        name: "FK_CourseProgressRecords_CareerEnrollments_CareerEnrollmentId",
                        column: x => x.CareerEnrollmentId,
                        principalTable: "CareerEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseProgressRecords_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonProgressRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseProgressId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonProgressRecords", x => x.Id);
                    table.CheckConstraint("CK_LessonProgress_CompletedAfterStarted", "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\"");
                    table.ForeignKey(
                        name: "FK_LessonProgressRecords_CourseProgressRecords_CourseProgressId",
                        column: x => x.CourseProgressId,
                        principalTable: "CourseProgressRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonProgressRecords_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerEnrollments_CareerId",
                table: "CareerEnrollments",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerEnrollments_CareerPathwayId",
                table: "CareerEnrollments",
                column: "CareerPathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerEnrollments_Status",
                table: "CareerEnrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CareerEnrollments_UserId",
                table: "CareerEnrollments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_CareerEnrollments_User_Pathway_NonWithdrawn",
                table: "CareerEnrollments",
                columns: new[] { "UserId", "CareerPathwayId" },
                unique: true,
                filter: "\"Status\" <> 'Withdrawn'");

            migrationBuilder.CreateIndex(
                name: "IX_CourseProgressRecords_CareerEnrollmentId_CourseId",
                table: "CourseProgressRecords",
                columns: new[] { "CareerEnrollmentId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseProgressRecords_CourseId",
                table: "CourseProgressRecords",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgressRecords_CourseProgressId_LessonId",
                table: "LessonProgressRecords",
                columns: new[] { "CourseProgressId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgressRecords_LessonId",
                table: "LessonProgressRecords",
                column: "LessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonProgressRecords");

            migrationBuilder.DropTable(
                name: "CourseProgressRecords");

            migrationBuilder.DropTable(
                name: "CareerEnrollments");
        }
    }
}
