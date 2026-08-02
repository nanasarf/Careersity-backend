using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Careersity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalLearningResourcesAndProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Instructors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Biography = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    ProfileUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instructors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Instructors_LearningProviders_LearningProviderId",
                        column: x => x.LearningProviderId,
                        principalTable: "LearningProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalLearningResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstructorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AccessType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SourceLabel = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    LastReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLearningResources", x => x.Id);
                    table.CheckConstraint("CK_ExternalLearningResources_Duration_Positive", "\"EstimatedDurationMinutes\" IS NULL OR \"EstimatedDurationMinutes\" > 0");
                    table.ForeignKey(
                        name: "FK_ExternalLearningResources_Instructors_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Instructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalLearningResources_LearningProviders_LearningProvide~",
                        column: x => x.LearningProviderId,
                        principalTable: "LearningProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseExternalResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalLearningResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseExternalResources", x => x.Id);
                    table.CheckConstraint("CK_CourseExternalResources_Order_Nonnegative", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_CourseExternalResources_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseExternalResources_ExternalLearningResources_ExternalL~",
                        column: x => x.ExternalLearningResourceId,
                        principalTable: "ExternalLearningResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalResourceProgressRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseProgressId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseExternalResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalResourceProgressRecords", x => x.Id);
                    table.CheckConstraint("CK_ExternalResourceProgress_CompletedAfterStarted", "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\"");
                    table.ForeignKey(
                        name: "FK_ExternalResourceProgressRecords_CourseExternalResources_Cou~",
                        column: x => x.CourseExternalResourceId,
                        principalTable: "CourseExternalResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalResourceProgressRecords_CourseProgressRecords_Cours~",
                        column: x => x.CourseProgressId,
                        principalTable: "CourseProgressRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseExternalResources_CourseId_ExternalLearningResourceId",
                table: "CourseExternalResources",
                columns: new[] { "CourseId", "ExternalLearningResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseExternalResources_CourseId_Order",
                table: "CourseExternalResources",
                columns: new[] { "CourseId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseExternalResources_ExternalLearningResourceId",
                table: "CourseExternalResources",
                column: "ExternalLearningResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLearningResources_AccessType",
                table: "ExternalLearningResources",
                column: "AccessType");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLearningResources_InstructorId",
                table: "ExternalLearningResources",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLearningResources_LearningProviderId",
                table: "ExternalLearningResources",
                column: "LearningProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLearningResources_ResourceType",
                table: "ExternalLearningResources",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLearningResources_Status",
                table: "ExternalLearningResources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResourceProgressRecords_CourseExternalResourceId",
                table: "ExternalResourceProgressRecords",
                column: "CourseExternalResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalResourceProgressRecords_CourseProgressId_CourseExte~",
                table: "ExternalResourceProgressRecords",
                columns: new[] { "CourseProgressId", "CourseExternalResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instructors_LearningProviderId",
                table: "Instructors",
                column: "LearningProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Instructors_Status",
                table: "Instructors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LearningProviders_Name",
                table: "LearningProviders",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LearningProviders_Slug",
                table: "LearningProviders",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningProviders_Status",
                table: "LearningProviders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalResourceProgressRecords");

            migrationBuilder.DropTable(
                name: "CourseExternalResources");

            migrationBuilder.DropTable(
                name: "ExternalLearningResources");

            migrationBuilder.DropTable(
                name: "Instructors");

            migrationBuilder.DropTable(
                name: "LearningProviders");
        }
    }
}
