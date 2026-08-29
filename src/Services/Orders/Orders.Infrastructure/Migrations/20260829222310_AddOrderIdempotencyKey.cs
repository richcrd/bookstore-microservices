using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE orders SET \"IdempotencyKey\" = gen_random_uuid()::text WHERE \"IdempotencyKey\" = '';");

            migrationBuilder.CreateIndex(
                name: "IX_orders_IdempotencyKey",
                table: "orders",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_IdempotencyKey",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "orders");
        }
    }
}
