using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkFlowBasic.Migrations
{
    /// <inheritdoc />
    public partial class AddCVPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CVFilePath",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CVFilePath",
                table: "AspNetUsers");
        }
    }
}
