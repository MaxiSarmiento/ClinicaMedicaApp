using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
    public class TurnoDisponibleDto
    {
        public int Id { get; set; }
        public int IdDoctor { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public string Estado { get; set; } = "";

        public string DoctorNombre { get; set; } = "";
        public string DoctorApellido { get; set; } = "";
        public List<string> Especialidades { get; set; } = new();

        public string DoctorNombreCompleto => $"{DoctorNombre} {DoctorApellido}".Trim();
        public string EspecialidadesTexto => Especialidades != null && Especialidades.Count > 0
            ? string.Join(" • ", Especialidades)
            : "Sin especialidad";
    }

}
