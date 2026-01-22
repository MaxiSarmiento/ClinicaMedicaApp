using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Runtime.CompilerServices;
using System;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class GenerarTurnosMasivosPage : ContentPage
{
    private readonly ApiService _api;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new TimeSpanJsonConverter() }
    };

    public GenerarTurnosMasivosPage(ApiService api)
    {
        InitializeComponent();
        _api = api;

        DesdeDate.Date = DateTime.Today;
        HastaDate.Date = DateTime.Today.AddDays(30);
        HoraInicioPicker.Time = new TimeSpan(8, 0, 0);
        HoraFinPicker.Time = new TimeSpan(12, 0, 0);
    }

    private async Task<(string token, int doctorId)?> GetSesionAsync()
    {
        var token = await SecureStorage.GetAsync("token");
        var idStr = await SecureStorage.GetAsync("userId");

        if (string.IsNullOrWhiteSpace(token) || !int.TryParse(idStr, out var id) || id <= 0)
        {
            await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
            await Shell.Current.GoToAsync("//Login");
            return null;
        }

        return (token, id);
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ConfigurarAgenda");
    }
    private List<int> GetExcluirDias()
    {
        var list = new List<int>();
        if (ChkLun.IsChecked) list.Add(1);
        if (ChkMar.IsChecked) list.Add(2);
        if (ChkMie.IsChecked) list.Add(3);
        if (ChkJue.IsChecked) list.Add(4);
        if (ChkVie.IsChecked) list.Add(5);
        if (ChkSab.IsChecked) list.Add(6);
        if (ChkDom.IsChecked) list.Add(7);
        return list;
    }

    private async void OnGenerarClicked(object sender, EventArgs e)
    {
        try
        {
            Loading.IsVisible = Loading.IsRunning = true;
            LblResultado.Text = "";

            var sesion = await GetSesionAsync();
            if (sesion == null) return;

            if (!int.TryParse(DuracionEntry.Text, out var dur) || dur <= 0)
            {
                await DisplayAlert("Atención", "Duración inválida", "OK");
                return;
            }

            int? maxPorDia = null;
            if (!string.IsNullOrWhiteSpace(MaxPorDiaEntry.Text))
            {
                if (!int.TryParse(MaxPorDiaEntry.Text, out var m) || m <= 0)
                {
                    await DisplayAlert("Atención", "Máx por día inválido", "OK");
                    return;
                }
                maxPorDia = m;
            }

            var dto = new GenerarMasivoDto
            {
                FechaInicio = DesdeDate.Date.Date,
                FechaFin = HastaDate.Date.Date,
                HoraInicio = HoraInicioPicker.Time,
                HoraFin = HoraFinPicker.Time,
                DuracionMinutos = dur,
                ExcluirDiasSemana = GetExcluirDias(),
                MaxTurnosPorDia = maxPorDia
            };

            var json = JsonSerializer.Serialize(dto, _jsonOpts);
            var req = new HttpRequestMessage(HttpMethod.Post, "api/Turnos/generar-masivo");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Value.token);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await _api.Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", body, "OK");
                return;
            }

            var resp = JsonSerializer.Deserialize<GenerarResp>(body, _jsonOpts);
            await DisplayAlert("Listo", $"Turnos creados: {resp?.creados ?? 0}\n{resp?.nota}", "OK");
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

    private class GenerarMasivoDto
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public int DuracionMinutos { get; set; }
        public List<int> ExcluirDiasSemana { get; set; } = new();
        public int? MaxTurnosPorDia { get; set; }
    }

    private class GenerarResp
    {
        public int creados { get; set; }
        public string? nota { get; set; }
    }

    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s)) return TimeSpan.Zero;
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts)) return ts;
            return TimeSpan.Parse(s, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("c", CultureInfo.InvariantCulture));
    }
}
