using ClinicaMedica.Cliente.Services;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class ConfigurarAgendaPage : ContentPage
{
    private readonly ApiService _api;
    private List<AgendaVm> _items = new();

    
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new TimeSpanJsonConverter() }
    };

 
    private const string DoctorAgendaBase = "api/DoctorAgendas";

    public ConfigurarAgendaPage(ApiService api)
    {
        InitializeComponent();
        _api = api;

        CargarDiasPicker();
        DesdeDate.Date = DateTime.Today;
        HastaDate.Date = DateTime.Today.AddDays(30);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefrescarAsync();
    }

    private void CargarDiasPicker()
    {
        DiaPicker.ItemsSource = new List<KeyValuePair<int, string>>
        {
            new(1,"Lunes"),
            new(2,"Martes"),
            new(3,"Miércoles"),
            new(4,"Jueves"),
            new(5,"Viernes"),
            new(6,"Sábado"),
            new(7,"Domingo"),
        };
        DiaPicker.ItemDisplayBinding = new Binding("Value");
        DiaPicker.SelectedIndex = 0;

        HoraInicioPicker.Time = new TimeSpan(9, 0, 0);
        HoraFinPicker.Time = new TimeSpan(13, 0, 0);
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
        await Shell.Current.GoToAsync("HomeDoctor");
    }
    private void DebugShow(string text)
    {
        LblDebug.Text = text;
        LblDebug.IsVisible = true;
    }

    private void DebugHide()
    {
        LblDebug.Text = "";
        LblDebug.IsVisible = false;
    }

    private async Task RefrescarAsync()
    {
        DebugHide();

        try
        {
            Loading.IsVisible = Loading.IsRunning = true;

            var sesion = await GetSesionAsync();
            if (sesion == null) return;

            
            var endpoint = $"{DoctorAgendaBase}/doctor/{sesion.Value.doctorId}";
            var abs = _api.Client.BaseAddress != null
                ? new Uri(_api.Client.BaseAddress, endpoint).ToString()
                : endpoint;

           
            var req = new HttpRequestMessage(HttpMethod.Get, endpoint);
            req.Headers.Accept.ParseAdd("*/*");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Value.token);

            HttpResponseMessage res;
            string body;

            try
            {
                res = await _api.Client.SendAsync(req);
                body = await res.Content.ReadAsStringAsync();
            }
            catch (Exception exReq)
            {
                AgendaList.ItemsSource = null;
                DebugShow($"EXCEPCIÓN REQUEST\n{abs}\n\n{exReq}");
                return;
            }

            if (!res.IsSuccessStatusCode)
            {
                AgendaList.ItemsSource = null;
                DebugShow($"HTTP {(int)res.StatusCode} {res.ReasonPhrase}\n{abs}\n\n{body}");
                return;
            }

       
            var list = JsonSerializer.Deserialize<List<DoctorAgendaItemDto>>(body, _jsonOpts)
                       ?? new List<DoctorAgendaItemDto>();

            _items = list.Select(x => new AgendaVm
            {
                Id = x.Id,
                DoctorID = x.DoctorID,
                DiaSemana = x.DiaSemana,
                HoraInicio = x.HoraInicio,
                HoraFin = x.HoraFin,
                DuracionMinutos = x.DuracionMinutos
            }).ToList();

            AgendaList.ItemsSource = _items;

          
            DebugHide();
        }
        catch (Exception ex)
        {
            AgendaList.ItemsSource = null;
            DebugShow($"EXCEPCIÓN GENERAL\n\n{ex}");
        }
        finally
        {
            Loading.IsVisible = Loading.IsRunning = false;
        }

    }

    private async void OnGuardarReglaClicked(object sender, EventArgs e)
    {
        DebugHide();

        try
        {
            var sesion = await GetSesionAsync();
            if (sesion == null) return;

            if (DiaPicker.SelectedItem is not KeyValuePair<int, string> diaSel)
            {
                await DisplayAlert("Atención", "Seleccioná un día", "OK");
                return;
            }

            if (!int.TryParse(DuracionEntry.Text, out var dur) || dur <= 0)
            {
                await DisplayAlert("Atención", "Duración inválida", "OK");
                return;
            }

            var hi = HoraInicioPicker.Time;
            var hf = HoraFinPicker.Time;

            if (hf <= hi)
            {
                await DisplayAlert("Atención", "Hora fin debe ser mayor que hora inicio", "OK");
                return;
            }

            if (hi.Add(TimeSpan.FromMinutes(dur)) > hf)
            {
                await DisplayAlert("Atención", "Con esa duración no entra ningún turno dentro del horario", "OK");
                return;
            }

            var dto = new DoctorAgendaCreateDto
            {
               
                DiaSemana = diaSel.Key,
                HoraInicio = hi,
                HoraFin = hf,
                DuracionMinutos = dur
            };

            Loading.IsVisible = Loading.IsRunning = true;

            var json = JsonSerializer.Serialize(dto, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage(HttpMethod.Post, DoctorAgendaBase);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Value.token);
            req.Content = content;

            var res = await _api.Client.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync();
                DebugShow($"POST ERROR\nHTTP {(int)res.StatusCode} {res.ReasonPhrase}\n\n{msg}");
                return;
            }

            DuracionEntry.Text = "";
            await RefrescarAsync();
        }
        catch (Exception ex)
        {
            DebugShow($"EXCEPCIÓN GUARDAR\n\n{ex}");
        }
        finally
        {
            Loading.IsVisible = Loading.IsRunning = false;
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        DebugHide();

        if (sender is not Button btn || btn.CommandParameter is not int idAgenda)
            return;

        var ok = await DisplayAlert("Confirmar", "¿Eliminar esta regla?", "Sí", "No");
        if (!ok) return;

        try
        {
            var sesion = await GetSesionAsync();
            if (sesion == null) return;

            Loading.IsVisible = Loading.IsRunning = true;

            var endpoint = $"{DoctorAgendaBase}/{idAgenda}";
            var req = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Value.token);

            var res = await _api.Client.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync();
                DebugShow($"DELETE ERROR\nHTTP {(int)res.StatusCode} {res.ReasonPhrase}\n\n{msg}");
                return;
            }

            await RefrescarAsync();
        }
        catch (Exception ex)
        {
            DebugShow($"EXCEPCIÓN ELIMINAR\n\n{ex}");
        }
        finally
        {
            Loading.IsVisible = Loading.IsRunning = false;
        }
    }

    private async void OnGenerarTurnosClicked(object sender, EventArgs e)
    {
        DebugHide();

        try
        {
            var sesion = await GetSesionAsync();
            if (sesion == null) return;

            var desde = DesdeDate.Date.Date;
            var hasta = HastaDate.Date.Date;

            if (hasta < desde)
            {
                await DisplayAlert("Atención", "Hasta no puede ser menor que Desde", "OK");
                return;
            }

            Loading.IsVisible = Loading.IsRunning = true;

            var dto = new GenerarTurnosDto
            {
              
                FechaInicio = desde,
                FechaFin = hasta
            };

            var json = JsonSerializer.Serialize(dto, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage(HttpMethod.Post, "api/Turnos/generar");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Value.token);
            req.Content = content;

           
            var res = await _api.Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            var resp = JsonSerializer.Deserialize<GenerarResp>(body, _jsonOpts);
            var creados = resp?.creados ?? 0;
            var nota = resp?.nota ?? "";


            if (!res.IsSuccessStatusCode)
            {
                DebugShow($"GENERAR ERROR\nHTTP {(int)res.StatusCode} {res.ReasonPhrase}\n\n{body}");
                return;
            }

            await DisplayAlert("Listo",
    $"Turnos creados: {creados}" + (string.IsNullOrWhiteSpace(nota) ? "" : $"\n{nota}"),
    "OK");


        }
        catch (Exception ex)
        {
            DebugShow($"EXCEPCIÓN GENERAR\n\n{ex}");
        }
        finally
        {
            Loading.IsVisible = Loading.IsRunning = false;
        }

    }
    private async void OnVerAgendaClicked(object sender, EventArgs e)
    => await Shell.Current.GoToAsync("AgendaDoctor");


    // DTOs locales
    private class DoctorAgendaItemDto
    {
        public int Id { get; set; }
        public int DoctorID { get; set; }
        public int DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public int DuracionMinutos { get; set; }
    }
    private class GenerarResp
    {
        public int creados { get; set; }
        public string? nota { get; set; }
    }

    private class DoctorAgendaCreateDto
    {
        public int DoctorID { get; set; }
        public int DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public int DuracionMinutos { get; set; }
    }

    private class GenerarTurnosDto
    {
        public int DoctorID { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }

    private class AgendaVm
    {
        public int Id { get; set; }
        public int DoctorID { get; set; }
        public int DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public int DuracionMinutos { get; set; }

        public string DiaTexto => DiaSemana switch
        {
            1 => "Lunes",
            2 => "Martes",
            3 => "Miércoles",
            4 => "Jueves",
            5 => "Viernes",
            6 => "Sábado",
            7 => "Domingo",
            _ => $"Día {DiaSemana}"
        };

        public string HorarioTexto => $"{HoraInicio:hh\\:mm} - {HoraFin:hh\\:mm}";
        public string DuracionTexto => $"Duración: {DuracionMinutos} min";
    }

    //  Converter TimeSpan
    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s)) return TimeSpan.Zero;

            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
                return ts;

            return TimeSpan.Parse(s, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("c", CultureInfo.InvariantCulture));
        }

    }
}
