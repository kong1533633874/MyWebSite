using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changeUsernameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "T_Users");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "T_Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "T_Users");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "T_Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }
    }
}
