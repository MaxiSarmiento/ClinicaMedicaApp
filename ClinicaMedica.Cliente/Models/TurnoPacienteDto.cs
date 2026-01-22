using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
    public class TurnoPacienteDto
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public string Estado { get; set; } = "";
        public string Doctor { get; set; } = "";

        public string FechaHoraString => FechaHora.ToString("dd/MM/yyyy HH:mm");
    }
}
