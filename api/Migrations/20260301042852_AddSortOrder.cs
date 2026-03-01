using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Todos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Initialise existing rows to 0, 1, 2 … ordered by CreatedAt
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT Id, (ROW_NUMBER() OVER (ORDER BY CreatedAt) - 1) AS rn
                    FROM Todos
                )
                UPDATE Todos SET SortOrder = (SELECT rn FROM ranked WHERE ranked.Id = Todos.Id)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Todos");
        }
    }
}
