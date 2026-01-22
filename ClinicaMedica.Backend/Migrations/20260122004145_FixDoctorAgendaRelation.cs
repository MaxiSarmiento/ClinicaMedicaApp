using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaMedica.Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixDoctorAgendaRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorAgenda_Usuarios_DoctorID",
                table: "DoctorAgenda");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorAgenda_Usuarios_DoctorId",
                table: "DoctorAgenda");

            migrationBuilder.DropIndex(
                name: "IX_DoctorAgenda_DoctorID",
                table: "DoctorAgenda");

            migrationBuilder.DropColumn(
                name: "DoctorID",
                table: "DoctorAgenda");

            migrationBuilder.RenameColumn(
                name: "DoctorId",
                table: "DoctorAgenda",
                newName: "DoctorID");

            migrationBuilder.RenameIndex(
                name: "IX_DoctorAgenda_DoctorId",
                table: "DoctorAgenda",
                newName: "IX_DoctorAgenda_DoctorID");

            migrationBuilder.AlterColumn<int>(
                name: "DoctorID",
                table: "DoctorAgenda",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorAgenda_Usuarios_DoctorID",
                table: "DoctorAgenda",
                column: "DoctorID",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorAgenda_Usuarios_DoctorID",
                table: "DoctorAgenda");

            migrationBuilder.RenameColumn(
                name: "DoctorID",
                table: "DoctorAgenda",
                newName: "DoctorId");

            migrationBuilder.RenameIndex(
                name: "IX_DoctorAgenda_DoctorID",
                table: "DoctorAgenda",
                newName: "IX_DoctorAgenda_DoctorId");

            migrationBuilder.AlterColumn<int>(
                name: "DoctorId",
                table: "DoctorAgenda",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DoctorID",
                table: "DoctorAgenda",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAgenda_DoctorID",
                table: "DoctorAgenda",
                column: "DoctorID");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorAgenda_Usuarios_DoctorID",
                table: "DoctorAgenda",
                column: "DoctorID",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorAgenda_Usuarios_DoctorId",
                table: "DoctorAgenda",
                column: "DoctorId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }
    }
}
