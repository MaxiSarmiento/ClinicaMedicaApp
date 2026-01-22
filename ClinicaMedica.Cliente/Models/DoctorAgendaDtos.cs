namespace ClinicaMedica.Cliente.Models;

// ===============================
// REGLAS (DoctorAgenda)
// ===============================
public class DoctorAgendaDtos
{
    public int ID { get; set; }
    public int DoctorID { get; set; }
    public int DiaSemana { get; set; } // 1..7 (Lun..Dom)
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int DuracionMinutos { get; set; }
}

public class DoctorAgendaCreateDto
{
    //public int DoctorID { get; set; }
    public int DiaSemana { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int DuracionMinutos { get; set; }
}

// ===============================
// GENERAR TURNOS
// ===============================
public class GenerarTurnosRequestDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
}

public class GenerarTurnosResponseDto
{
    public string? Mensaje { get; set; }
    public int Creados { get; set; }
}

// ===============================
// TURNOS (agenda real)
// ===============================
public class TurnoAgendaDto
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; }
    public int DuracionMinutos { get; set; }
    public string Estado { get; set; } = "";

    // el backend devuelve Paciente = null o {Nombre, Apellido}
    public PacienteMiniDto? Paciente { get; set; }

    // helper para mostrar lindo en pantalla
    public string? PacienteNombre =>
        Paciente == null ? null : $"{Paciente.Nombre} {Paciente.Apellido}";
}


