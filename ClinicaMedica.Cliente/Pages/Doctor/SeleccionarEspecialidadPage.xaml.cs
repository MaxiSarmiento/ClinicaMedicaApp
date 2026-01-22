using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class SeleccionarEspecialidadPage : ContentPage
{
    private readonly ApiService _api;
    private List<EspecialidadDto> _especialidadesDisponibles;

    public SeleccionarEspecialidadPage()
    {
        InitializeComponent();

        var services = App.Current.Handler.MauiContext.Services;
        _api = services.GetService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarEspecialidades();
    }

    private async Task CargarEspecialidades()
    {
        var token = await SecureStorage.GetAsync("jwt");

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var resp = await _api.GetAsync("api/Especialidades");

        if (!resp.IsSuccessStatusCode)
        {
            await DisplayAlert("Error", "No se pudieron cargar las especialidades.", "OK");
            return;
        }

        _especialidadesDisponibles = await resp.Content.ReadFromJsonAsync<List<EspecialidadDto>>();

        PickerEspecialidades.ItemsSource = _especialidadesDisponibles;
        PickerEspecialidades.ItemDisplayBinding = new Binding("Nombre");
    }

    private async void OnAgregar(object sender, EventArgs e)
    {
        if (PickerEspecialidades.SelectedItem is not EspecialidadDto seleccionada)
        {
            await DisplayAlert("Error", "Seleccioná una especialidad.", "OK");
            return;
        }

        var doctorId = await SecureStorage.GetAsync("userId");
        var token = await SecureStorage.GetAsync("jwt");

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var dto = new
        {
            DoctorID = int.Parse(doctorId),
            EspID = seleccionada.EspID
        };

        var resp = await _api.PostAsync("api/DoctoresEspecialidades/agregar", dto);

        if (resp.IsSuccessStatusCode)
        {
            await DisplayAlert("OK", "Especialidad agregada.", "Cerrar");

            // volver al perfil y refrescarlo
            await Navigation.PopAsync();
        }
        else
        {
            string error = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Error", error, "Cerrar");
        }
    }
}
