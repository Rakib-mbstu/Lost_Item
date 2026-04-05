using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lost_Item.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintReviewedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Complaints",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Complaints");
        }
    }
}
