using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Careersity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentAttemptsAndAutomaticGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseProgressId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScorePercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    PointsEarned = table.Column<int>(type: "integer", nullable: true),
                    TotalPoints = table.Column<int>(type: "integer", nullable: true),
                    PassedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAttempts", x => x.Id);
                    table.CheckConstraint("CK_AssessmentAttempts_AttemptNumber_Positive", "\"AttemptNumber\" > 0");
                    table.CheckConstraint("CK_AssessmentAttempts_PassedAfterStarted", "\"PassedAtUtc\" IS NULL OR \"PassedAtUtc\" >= \"StartedAtUtc\"");
                    table.CheckConstraint("CK_AssessmentAttempts_PointsEarned_Nonnegative", "\"PointsEarned\" IS NULL OR \"PointsEarned\" >= 0");
                    table.CheckConstraint("CK_AssessmentAttempts_Score_Range", "\"ScorePercentage\" IS NULL OR (\"ScorePercentage\" BETWEEN 0 AND 100)");
                    table.CheckConstraint("CK_AssessmentAttempts_SubmittedAfterStarted", "\"SubmittedAtUtc\" IS NULL OR \"SubmittedAtUtc\" >= \"StartedAtUtc\"");
                    table.CheckConstraint("CK_AssessmentAttempts_TotalPoints_Positive", "\"TotalPoints\" IS NULL OR \"TotalPoints\" > 0");
                    table.ForeignKey(
                        name: "FK_AssessmentAttempts_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentAttempts_CareerEnrollments_CareerEnrollmentId",
                        column: x => x.CareerEnrollmentId,
                        principalTable: "CareerEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentAttempts_CourseProgressRecords_CourseProgressId",
                        column: x => x.CourseProgressId,
                        principalTable: "CourseProgressRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentResponses_AssessmentAttempts_AssessmentAttemptId",
                        column: x => x.AssessmentAttemptId,
                        principalTable: "AssessmentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentResponses_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentResponseOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnswerOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResponseOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentResponseOptions_AnswerOptions_AnswerOptionId",
                        column: x => x.AnswerOptionId,
                        principalTable: "AnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentResponseOptions_AssessmentResponses_AssessmentRes~",
                        column: x => x.AssessmentResponseId,
                        principalTable: "AssessmentResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_AssessmentId",
                table: "AssessmentAttempts",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_CareerEnrollmentId",
                table: "AssessmentAttempts",
                column: "CareerEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_CareerEnrollmentId_AssessmentId_AttemptN~",
                table: "AssessmentAttempts",
                columns: new[] { "CareerEnrollmentId", "AssessmentId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_CourseProgressId",
                table: "AssessmentAttempts",
                column: "CourseProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_UserId",
                table: "AssessmentAttempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_UserId_AssessmentId",
                table: "AssessmentAttempts",
                columns: new[] { "UserId", "AssessmentId" });

            migrationBuilder.CreateIndex(
                name: "UX_AssessmentAttempts_Enrollment_Assessment_InProgress",
                table: "AssessmentAttempts",
                columns: new[] { "CareerEnrollmentId", "AssessmentId" },
                unique: true,
                filter: "\"Status\" = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponseOptions_AnswerOptionId",
                table: "AssessmentResponseOptions",
                column: "AnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponseOptions_AssessmentResponseId_AnswerOption~",
                table: "AssessmentResponseOptions",
                columns: new[] { "AssessmentResponseId", "AnswerOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_AssessmentAttemptId_QuestionId",
                table: "AssessmentResponses",
                columns: new[] { "AssessmentAttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_QuestionId",
                table: "AssessmentResponses",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentResponseOptions");

            migrationBuilder.DropTable(
                name: "AssessmentResponses");

            migrationBuilder.DropTable(
                name: "AssessmentAttempts");
        }
    }
}
