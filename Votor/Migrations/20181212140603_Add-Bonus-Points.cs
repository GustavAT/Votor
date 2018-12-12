using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Votor.Migrations
{
    public partial class AddBonusPoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonusPoints",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Points = table.Column<double>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    OptionID = table.Column<Guid>(nullable: true),
                    QuestionID = table.Column<Guid>(nullable: true),
                    EventID = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusPoints", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BonusPoints_Event_EventID",
                        column: x => x.EventID,
                        principalTable: "Event",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BonusPoints_Option_OptionID",
                        column: x => x.OptionID,
                        principalTable: "Option",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BonusPoints_Question_QuestionID",
                        column: x => x.QuestionID,
                        principalTable: "Question",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonusPoints_EventID",
                table: "BonusPoints",
                column: "EventID");

            migrationBuilder.CreateIndex(
                name: "IX_BonusPoints_OptionID",
                table: "BonusPoints",
                column: "OptionID");

            migrationBuilder.CreateIndex(
                name: "IX_BonusPoints_QuestionID",
                table: "BonusPoints",
                column: "QuestionID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonusPoints");
        }
    }
}
