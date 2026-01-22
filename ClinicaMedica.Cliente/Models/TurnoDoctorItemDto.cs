using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
    internal class TurnoDoctorItemDto
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public bool Reservado { get; set; }
        public string? PacienteApellido { get; set; }

        public string Hora => FechaHora.ToString("HH:mm");
        public string Estado => Reservado ? $"Reservado ({PacienteApellido})" : "Libre";
    }
}
