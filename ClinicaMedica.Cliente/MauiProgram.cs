using ClinicaMedica.Cliente.Pages;
using ClinicaMedica.Cliente.Pages.Admin;
using ClinicaMedica.Cliente.Pages.Doctor;
using ClinicaMedica.Cliente.Pages.Paciente;
using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

#if ANDROID
        var baseUrl = new Uri("http://192.168.0.107:5293/");
#else
        var baseUrl = new Uri("http://localhost:5293/");
#endif

        // Handler JWT 
        builder.Services.AddTransient<AuthHttpMessageHandler>();

        // HttpClients
        builder.Services.AddHttpClient<AuthService>(c => c.BaseAddress = baseUrl);

        builder.Services.AddHttpClient<ApiService>(c => c.BaseAddress = baseUrl)
            .AddHttpMessageHandler<AuthHttpMessageHandler>();

        builder.Services.AddHttpClient<UsuariosService>(c => c.BaseAddress = baseUrl)
            .AddHttpMessageHandler<AuthHttpMessageHandler>();

        builder.Services.AddHttpClient<EstudiosService>(c => c.BaseAddress = baseUrl)
            .AddHttpMessageHandler<AuthHttpMessageHandler>();

    

        // PÁGINAS (DI)
       
        // Login / Registro
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();

        // Homes
        builder.Services.AddTransient<HomePacientePage>();
        builder.Services.AddTransient<HomeDoctorPage>();
        builder.Services.AddTransient<HomeAdminPage>();

        // Paciente
        builder.Services.AddTransient<EditarPerfilPacientePage>();
        builder.Services.AddTransient<SeleccionarObraSocialPacientePage>();
        builder.Services.AddTransient<VerEstudiosPacientePage>();
        builder.Services.AddTransient<MisTurnosPage>();
        builder.Services.AddTransient<ReservarTurnoPage>();
        builder.Services.AddTransient<BuscarDoctorTurnoPage>();
        builder.Services.AddTransient<TurnosDisponiblesPage>();

        // Doctor

        builder.Services.AddTransient<DoctorAgendaPage>();
        builder.Services.AddSingleton<DoctorAgendaApiService>();
        builder.Services.AddTransient<ConfigurarAgendaPage>();
        builder.Services.AddTransient<AgendaDoctorPage>();
        builder.Services.AddTransient<BuscarPacientePage>();
        builder.Services.AddTransient<VerEstudiosDoctorPage>();
        builder.Services.AddTransient<PerfilDoctorPage>();
        builder.Services.AddTransient<CrearTurnoSingularPage>();
        builder.Services.AddTransient<GenerarTurnosMasivosPage>();
        builder.Services.AddTransient<SeleccionarObraSocialPage>();
        builder.Services.AddTransient<SeleccionarEspecialidadPage>();
        builder.Services.AddTransient<DoctorAgendaCalendarioPage>();




        // Admin
        builder.Services.AddTransient<SubirEstudioPage>();
        builder.Services.AddTransient<EditarEstudiosPage>();
        builder.Services.AddTransient<ObrasSocialesPage>();
        builder.Services.AddTransient<EspecialidadesPage>();
        builder.Services.AddTransient<UsuariosPage>();

        return builder.Build();
    }
}
