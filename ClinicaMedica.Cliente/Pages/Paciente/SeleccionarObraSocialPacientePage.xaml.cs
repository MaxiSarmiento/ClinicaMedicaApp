using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class SeleccionarObraSocialPacientePage : ContentPage
{
    private readonly ApiService _api;
    private readonly Action<ObraSocialItem> _onSelected;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SeleccionarObraSocialPacientePage(ApiService api, Action<ObraSocialItem> onSelected)
    {
        InitializeComponent();
        _api = api;
        _onSelected = onSelected;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync(null);
    }

    private async Task CargarAsync(string? q)
    {
        try
        {
            Loading.IsVisible = Loading.IsRunning = true;

            var endpoint = string.IsNullOrWhiteSpace(q)
                ? "api/ObrasSociales"
                : $"api/ObrasSociales?q={Uri.EscapeDataString(q.Trim())}";

      
            var res = await _api.Client.GetAsync(endpoint);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", $"No pude cargar obras sociales (HTTP {(int)res.StatusCode})", "OK");
                List.ItemsSource = null;
                return;
            }

            var list = JsonSerializer.Deserialize<List<ObraSocialItem>>(body, _jsonOpts) ?? new();
            List.ItemsSource = list;
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

    private CancellationTokenSource? _cts;

    private async void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
           
            await Task.Delay(350, _cts.Token);
            await CargarAsync(Search.Text);
        }
        catch (TaskCanceledException) { }
    }

    private async void OnElegir(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ObraSocialItem item)
            return;

        _onSelected(item);

        // Como llegaste con Navigation.PushAsync, volvé así:
        await Navigation.PopAsync();
    }

    public class ObraSocialItem
    {
        public int OSID { get; set; }
        public string Nombre { get; set; } = "";
    }
}
