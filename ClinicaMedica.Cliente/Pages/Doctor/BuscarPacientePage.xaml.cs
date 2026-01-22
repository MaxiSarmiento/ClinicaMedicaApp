using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class BuscarPacientePage : ContentPage
{
    private readonly ApiService _api;

   
    public BuscarPacientePage()
    {
        InitializeComponent();

        var services = App.Current.Handler.MauiContext.Services;
        _api = services.GetService<ApiService>();
    }

    private async void OnBuscar(object sender, EventArgs e)
    {
        var texto = BuscarEntry.Text?.Trim();
        if (string.IsNullOrEmpty(texto))
            return;

        var token = await SecureStorage.GetAsync("jwt");
        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var pacientes = await _api.GetJsonAsync<List<Usuario>>(
            $"api/usuarios/buscar?texto={texto}");

        ListaPacientes.ItemsSource = pacientes;
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeDoctor");
    }
    private async void OnVerEstudios(object sender, EventArgs e)
    {
        var idPaciente = (int)((Button)sender).CommandParameter;

        await Shell.Current.GoToAsync(
            $"VerEstudiosDoctorPage?idPaciente={idPaciente}");
    }
}
