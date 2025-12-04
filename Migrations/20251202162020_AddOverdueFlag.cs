using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkFlowBasic.Migrations
{
    /// <inheritdoc />
    public partial class AddOverdueFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOverdueNotified",
                table: "WorkTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOverdueNotified",
                table: "WorkTasks");
        }
    }
}
