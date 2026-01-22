using ClinicaMedica.Cliente.Services;
using System.Globalization;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class CrearTurnoPage : ContentPage
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    public CrearTurnoPage(ApiService api, AuthService auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;
        FechaPicker.Date = DateTime.Today;
        HoraPicker.Time = TimeSpan.FromHours(9);
    }

    private async void OnGuardar(object sender, EventArgs e)
    {
        if (!int.TryParse(DuracionEntry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dur))
        {
            await DisplayAlert("Error", "Duración inválida", "OK");
            return;
        }

        var fechaHora = FechaPicker.Date + HoraPicker.Time;

      
        var dto = new
        {
            FechaHora = fechaHora,
            DuracionMinutos = dur
        };


        var token = await SecureStorage.GetAsync("jwt");
        if (token == null)
        {
            await DisplayAlert("Error", "Sesión expirada", "OK");
            return;
        }

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        
        var resp = await _api.PostAsync("api/doctor/crear-turnos", dto);

        if (resp.IsSuccessStatusCode)
            await DisplayAlert("OK", "Turno creado", "Cerrar");
        else
            await DisplayAlert("Error", "No se pudo crear", "Cerrar");
    }

}
