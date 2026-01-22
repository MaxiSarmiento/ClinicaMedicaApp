using ClinicaMedica.Cliente.Pages;
using ClinicaMedica.Cliente.Pages.Paciente;
using ClinicaMedica.Cliente.Pages.Doctor;
using ClinicaMedica.Cliente.Pages.Admin;

namespace ClinicaMedica.Cliente;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        // Login / Register
        Routing.RegisterRoute("Login", typeof(LoginPage));
        Routing.RegisterRoute("Register", typeof(RegisterPage));

        // Homes
        Routing.RegisterRoute("HomePaciente", typeof(HomePacientePage));
        Routing.RegisterRoute("HomeDoctor", typeof(HomeDoctorPage));
        Routing.RegisterRoute("HomeAdmin", typeof(HomeAdminPage));

        // Paciente
        Routing.RegisterRoute("EditarPerfilPaciente", typeof(EditarPerfilPacientePage));
        Routing.RegisterRoute("SeleccionarObraSocialPaciente", typeof(SeleccionarObraSocialPacientePage));
        Routing.RegisterRoute("VerEstudiosPaciente", typeof(VerEstudiosPacientePage));
        Routing.RegisterRoute("MisTurnosPaciente", typeof(MisTurnosPage));
        Routing.RegisterRoute("ReservarTurno", typeof(ReservarTurnoPage));
        Routing.RegisterRoute("BuscarDoctorTurno", typeof(BuscarDoctorTurnoPage));
        Routing.RegisterRoute("TurnosDisponibles", typeof(ClinicaMedica.Cliente.Pages.Paciente.TurnosDisponiblesPage));


        // Doctor
        Routing.RegisterRoute("AgendaDoctor", typeof(AgendaDoctorPage));
        
        Routing.RegisterRoute("BuscarPaciente", typeof(BuscarPacientePage));
        Routing.RegisterRoute("VerEstudiosDoctor", typeof(VerEstudiosDoctorPage));
        Routing.RegisterRoute("PerfilDoctor", typeof(PerfilDoctorPage));
        Routing.RegisterRoute("SeleccionarObraSocial", typeof(SeleccionarObraSocialPage));
        Routing.RegisterRoute("SeleccionarEspecialidad", typeof(SeleccionarEspecialidadPage));
        Routing.RegisterRoute("ConfigurarAgenda", typeof(DoctorAgendaPage));
        Routing.RegisterRoute("CrearTurnoSingular", typeof(CrearTurnoSingularPage));
        Routing.RegisterRoute("GenerarTurnosMasivos", typeof(GenerarTurnosMasivosPage));
        Routing.RegisterRoute("CrearAgenda", typeof(ConfigurarAgendaPage));
        Routing.RegisterRoute("AgendaDoctorCalendario", typeof(DoctorAgendaCalendarioPage));




        // Admin
        Routing.RegisterRoute("SubirEstudio", typeof(SubirEstudioPage));
        Routing.RegisterRoute("EditarEstudios", typeof(EditarEstudiosPage));
        Routing.RegisterRoute("ObrasSociales", typeof(ObrasSocialesPage));
        Routing.RegisterRoute("Especialidades", typeof(EspecialidadesPage));
        Routing.RegisterRoute("Usuarios", typeof(UsuariosPage));
    }
}
