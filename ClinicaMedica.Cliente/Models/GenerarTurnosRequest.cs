using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaMedica.Cliente.Models
{
    public class GenerarTurnosRequest
    {
        public DateTime Desde { get; set; }   // ej: 2026-01-01
        public DateTime Hasta { get; set; }   // ej: 2026-01-31
    }
}
