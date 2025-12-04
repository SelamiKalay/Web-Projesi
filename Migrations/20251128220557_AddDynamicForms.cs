using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkFlowBasic.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormData",
                table: "WorkflowRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormSchema",
                table: "ProcessDefinitions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormData",
                table: "WorkflowRequests");

            migrationBuilder.DropColumn(
                name: "FormSchema",
                table: "ProcessDefinitions");
        }
    }
}
