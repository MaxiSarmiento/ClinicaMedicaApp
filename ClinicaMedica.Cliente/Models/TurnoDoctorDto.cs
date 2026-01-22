using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
    public class TurnoDoctorDto
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }

        // Libre / Reservado / Cancelado (lo que uses)
        public string Estado { get; set; } = "Libre";

        // Nombre del paciente si está reservado
        public string? PacienteNombre { get; set; }
        public string? PacienteApellido { get; set; }

        // ✅ Helpers para UI
        public bool EstaLibre => Estado == "Libre" || string.IsNullOrWhiteSpace(PacienteNombre);

        public string TextoPaciente =>
            EstaLibre ? "Disponible" : $"{PacienteNombre} {PacienteApellido}";

        public string TextoHorario =>
            FechaHora.ToString("dd/MM/yyyy HH:mm");
    }
}
