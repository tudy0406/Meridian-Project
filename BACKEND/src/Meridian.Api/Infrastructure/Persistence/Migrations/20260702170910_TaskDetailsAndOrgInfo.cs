using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Meridian.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TaskDetailsAndOrgInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "task_templates",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "employee_tasks",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "departments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employee_task_attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeTaskId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_task_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_task_attachments_employee_tasks_EmployeeTaskId",
                        column: x => x.EmployeeTaskId,
                        principalTable: "employee_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_task_comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeTaskId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_task_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_task_comments_employee_tasks_EmployeeTaskId",
                        column: x => x.EmployeeTaskId,
                        principalTable: "employee_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_task_comments_users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_task_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeTaskId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangedById = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_task_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_task_history_employee_tasks_EmployeeTaskId",
                        column: x => x.EmployeeTaskId,
                        principalTable: "employee_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_task_history_users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_task_attachments_EmployeeTaskId",
                table: "employee_task_attachments",
                column: "EmployeeTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_task_comments_AuthorId",
                table: "employee_task_comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_task_comments_EmployeeTaskId",
                table: "employee_task_comments",
                column: "EmployeeTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_task_history_ChangedById",
                table: "employee_task_history",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_employee_task_history_EmployeeTaskId",
                table: "employee_task_history",
                column: "EmployeeTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_task_attachments");

            migrationBuilder.DropTable(
                name: "employee_task_comments");

            migrationBuilder.DropTable(
                name: "employee_task_history");

            migrationBuilder.DropColumn(
                name: "Requirements",
                table: "task_templates");

            migrationBuilder.DropColumn(
                name: "Requirements",
                table: "employee_tasks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "departments");
        }
    }
}
