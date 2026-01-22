using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
   
        public class TurnoDto
        {
            public int Id { get; set; }
            public int IdDoctor { get; set; }
            public int? IdPaciente { get; set; }

            public DateTime FechaHora { get; set; }
            public int DuracionMinutos { get; set; }

            public string Estado { get; set; } = "Libre";

            // Para mostrar sin traer objeto Doctor completo
            public string? DoctorNombre { get; set; }
            public string? DoctorApellido { get; set; }

            public string? PacienteNombre { get; set; }
            public string? PacienteApellido { get; set; }
        }
    


    public class PacienteMiniDto
    {
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
    }
}