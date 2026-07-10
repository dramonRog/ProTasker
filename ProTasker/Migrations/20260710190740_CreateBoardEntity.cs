using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProTasker.Migrations
{
    /// <inheritdoc />
    public partial class CreateBoardEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "TaskItems");

            migrationBuilder.AddColumn<Guid>(
                name: "BoardId",
                table: "TaskItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boards_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_BoardId",
                table: "TaskItems",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_Boards_ProjectId_OrderIndex",
                table: "Boards",
                columns: new[] { "ProjectId", "OrderIndex" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_Boards_BoardId",
                table: "TaskItems",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_Boards_BoardId",
                table: "TaskItems");

            migrationBuilder.DropTable(
                name: "Boards");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_BoardId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "BoardId",
                table: "TaskItems");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TaskItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
