using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
    public class DoctorDisponibleDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public bool AtiendeObraSocial { get; set; }
        public string Advertencia { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}
