using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Careersity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueCareerSkillDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CareerSkills_CareerId_DisplayOrder",
                table: "CareerSkills",
                columns: new[] { "CareerId", "DisplayOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CareerSkills_CareerId_DisplayOrder",
                table: "CareerSkills");
        }
    }
}
