using System.Net.Http.Headers;
using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class AgendaDoctorPage : ContentPage
{
    private readonly ApiService _api;

    public AgendaDoctorPage(ApiService api)
    {
        InitializeComponent();
        _api = api;

        DesdeDate.Date = DateTime.Today;
        HastaDate.Date = DateTime.Today.AddDays(30);
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeDoctor");
    }
    private async void OnCargarAgenda(object sender, EventArgs e)
    {
        try
        {
            Loading.IsVisible = Loading.IsRunning = true;

            var token = await SecureStorage.GetAsync("token");
            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var desde = DesdeDate.Date.ToString("yyyy-MM-dd");
            var hasta = HastaDate.Date.ToString("yyyy-MM-dd");

            var url = $"api/DoctorAgendas/agenda?desde={desde}&hasta={hasta}";

            var items = await _api.GetJsonAsync<List<TurnoAgendaDto>>(url);

            TurnosList.ItemsSource = items.Select(t => new TurnoVm(t)).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            Loading.IsVisible = Loading.IsRunning = false;
        }
    }

    public class TurnoAgendaDto
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public string Estado { get; set; } = "";
        public PacienteDto? Paciente { get; set; }
    }

    public class PacienteDto
    {
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
    }

    private class TurnoVm
    {
        private readonly TurnoAgendaDto _t;
        public TurnoVm(TurnoAgendaDto t) => _t = t;

        public string FechaTexto => $"{_t.FechaHora:dd/MM/yyyy HH:mm} ({_t.DuracionMinutos} min)";
        public string EstadoTexto => $"Estado: {_t.Estado}";
        public string PacienteTexto =>
            _t.Paciente is null ? "Paciente: (libre)" : $"Paciente: {_t.Paciente.Nombre} {_t.Paciente.Apellido}";
    }
}
