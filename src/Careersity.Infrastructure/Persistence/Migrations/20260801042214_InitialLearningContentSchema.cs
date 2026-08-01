using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Careersity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLearningContentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DetailedDescription = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.CheckConstraint("CK_Courses_EstimatedDurationMinutes_Positive", "\"EstimatedDurationMinutes\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "character varying(170)", maxLength: 170, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Careers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "character varying(170)", maxLength: 170, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DetailedDescription = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Responsibilities = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    EstimatedDurationWeeks = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Careers", x => x.Id);
                    table.CheckConstraint("CK_Careers_EstimatedDurationWeeks_Positive", "\"EstimatedDurationWeeks\" IS NULL OR \"EstimatedDurationWeeks\" > 0");
                    table.ForeignKey(
                        name: "FK_Careers_CareerCategories_CareerCategoryId",
                        column: x => x.CareerCategoryId,
                        principalTable: "CareerCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PassingScorePercentage = table.Column<int>(type: "integer", nullable: false),
                    MaximumAttempts = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                    table.CheckConstraint("CK_Assessments_MaximumAttempts_Positive", "\"MaximumAttempts\" IS NULL OR \"MaximumAttempts\" > 0");
                    table.CheckConstraint("CK_Assessments_PassingScorePercentage_Range", "\"PassingScorePercentage\" BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_Assessments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoursePrerequisites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrerequisiteCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursePrerequisites", x => x.Id);
                    table.CheckConstraint("CK_CoursePrerequisites_NoSelfReference", "\"CourseId\" <> \"PrerequisiteCourseId\"");
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_PrerequisiteCourseId",
                        column: x => x.PrerequisiteCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Content = table.Column<string>(type: "text", maxLength: 50000, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExternalResourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.CheckConstraint("CK_Lessons_EstimatedDurationMinutes_Positive", "\"EstimatedDurationMinutes\" > 0");
                    table.CheckConstraint("CK_Lessons_Order_Nonnegative", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_Lessons_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Instructions = table.Column<string>(type: "text", maxLength: 10000, nullable: false),
                    ExpectedOutput = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    EvaluationCriteria = table.Column<string>(type: "text", maxLength: 5000, nullable: true),
                    SubmissionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.CheckConstraint("CK_Projects_EstimatedDurationMinutes_Positive", "\"EstimatedDurationMinutes\" > 0");
                    table.ForeignKey(
                        name: "FK_Projects_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProficiencyLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSkills_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerPathways",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerPathways", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerPathways_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredProficiencyLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerSkills", x => x.Id);
                    table.CheckConstraint("CK_CareerSkills_DisplayOrder_Nonnegative", "\"DisplayOrder\" >= 0");
                    table.ForeignKey(
                        name: "FK_CareerSkills_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CareerSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    QuestionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.CheckConstraint("CK_Questions_Order_Nonnegative", "\"Order\" >= 0");
                    table.CheckConstraint("CK_Questions_Points_Positive", "\"Points\" > 0");
                    table.ForeignKey(
                        name: "FK_Questions_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PathwayLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerPathwayId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwayLevels", x => x.Id);
                    table.CheckConstraint("CK_PathwayLevels_Order_Nonnegative", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_PathwayLevels_CareerPathways_CareerPathwayId",
                        column: x => x.CareerPathwayId,
                        principalTable: "CareerPathways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerOptions", x => x.Id);
                    table.CheckConstraint("CK_AnswerOptions_Order_Nonnegative", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_AnswerOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PathwayLevelCourses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PathwayLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwayLevelCourses", x => x.Id);
                    table.CheckConstraint("CK_PathwayLevelCourses_Order_Nonnegative", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_PathwayLevelCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PathwayLevelCourses_PathwayLevels_PathwayLevelId",
                        column: x => x.PathwayLevelId,
                        principalTable: "PathwayLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerOptions_QuestionId",
                table: "AnswerOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerOptions_QuestionId_Order",
                table: "AnswerOptions",
                columns: new[] { "QuestionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CourseId",
                table: "Assessments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_Status",
                table: "Assessments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CareerCategories_Name",
                table: "CareerCategories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CareerCategories_Slug",
                table: "CareerCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerCategories_Status",
                table: "CareerCategories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CareerPathways_CareerId_Version",
                table: "CareerPathways",
                columns: new[] { "CareerId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerPathways_OnePrimaryPerCareer",
                table: "CareerPathways",
                column: "CareerId",
                unique: true,
                filter: "\"IsPrimary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_CareerPathways_Status",
                table: "CareerPathways",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Careers_CareerCategoryId",
                table: "Careers",
                column: "CareerCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Careers_Slug",
                table: "Careers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Careers_Status",
                table: "Careers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CareerSkills_CareerId",
                table: "CareerSkills",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerSkills_CareerId_SkillId",
                table: "CareerSkills",
                columns: new[] { "CareerId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerSkills_SkillId",
                table: "CareerSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_CourseId_PrerequisiteCourseId",
                table: "CoursePrerequisites",
                columns: new[] { "CourseId", "PrerequisiteCourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_PrerequisiteCourseId",
                table: "CoursePrerequisites",
                column: "PrerequisiteCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Difficulty",
                table: "Courses",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Slug",
                table: "Courses",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Status",
                table: "Courses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSkills_CourseId_SkillId",
                table: "CourseSkills",
                columns: new[] { "CourseId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseSkills_SkillId",
                table: "CourseSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId",
                table: "Lessons",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId_Order",
                table: "Lessons",
                columns: new[] { "CourseId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId_Slug",
                table: "Lessons",
                columns: new[] { "CourseId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PathwayLevelCourses_CourseId",
                table: "PathwayLevelCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayLevelCourses_PathwayLevelId_CourseId",
                table: "PathwayLevelCourses",
                columns: new[] { "PathwayLevelId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PathwayLevelCourses_PathwayLevelId_Order",
                table: "PathwayLevelCourses",
                columns: new[] { "PathwayLevelId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PathwayLevels_CareerPathwayId",
                table: "PathwayLevels",
                column: "CareerPathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayLevels_CareerPathwayId_Order",
                table: "PathwayLevels",
                columns: new[] { "CareerPathwayId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CourseId",
                table: "Projects",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AssessmentId_Order",
                table: "Questions",
                columns: new[] { "AssessmentId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Category",
                table: "Skills",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Slug",
                table: "Skills",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Status",
                table: "Skills",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerOptions");

            migrationBuilder.DropTable(
                name: "CareerSkills");

            migrationBuilder.DropTable(
                name: "CoursePrerequisites");

            migrationBuilder.DropTable(
                name: "CourseSkills");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "PathwayLevelCourses");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "PathwayLevels");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "CareerPathways");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Careers");

            migrationBuilder.DropTable(
                name: "CareerCategories");
        }
    }
}
