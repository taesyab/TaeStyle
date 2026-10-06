using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaeStyle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGarmentsAndPrivatePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GarmentPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarmentPhotos", x => x.Id);
                    table.UniqueConstraint("AK_GarmentPhotos_Id_UserId", x => new { x.Id, x.UserId });
                    table.ForeignKey(
                        name: "FK_GarmentPhotos_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Garments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Color = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Archived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Garments", x => x.Id);
                    table.CheckConstraint("CK_Garment_Price", "\"PurchasePrice\" IS NULL OR \"PurchasePrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_Garments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Garments_GarmentPhotos_PhotoId_UserId",
                        columns: x => new { x.PhotoId, x.UserId },
                        principalTable: "GarmentPhotos",
                        principalColumns: new[] { "Id", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GarmentPhotos_ExpiresAt",
                table: "GarmentPhotos",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_GarmentPhotos_UserId",
                table: "GarmentPhotos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Garments_PhotoId",
                table: "Garments",
                column: "PhotoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Garments_PhotoId_UserId",
                table: "Garments",
                columns: new[] { "PhotoId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Garments_UserId_CreatedAt_Id",
                table: "Garments",
                columns: new[] { "UserId", "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Garments");

            migrationBuilder.DropTable(
                name: "GarmentPhotos");
        }
    }
}
