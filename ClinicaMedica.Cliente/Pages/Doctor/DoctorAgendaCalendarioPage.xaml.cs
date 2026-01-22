using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class DoctorAgendaCalendarioPage : ContentPage
{
    private readonly ApiService _api;
    private DateTime _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    private List<TurnoDoctorItemDto> _turnosMes = new();
    private List<CalendarDayVm> _days = new();

    public DoctorAgendaCalendarioPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMonthAsync();
    }

    private async Task<(string? token, int userId)> GetSesionAsync()
    {
        var token = await SecureStorage.GetAsync("token") ?? await SecureStorage.GetAsync("jwt");
        var idStr = await SecureStorage.GetAsync("userId") ?? await SecureStorage.GetAsync("id_usuario");

        if (!int.TryParse(idStr, out var id)) id = 0;
        return (token, id);
    }

    private async Task LoadMonthAsync()
    {
        try
        {
            Loading.IsVisible = Loading.IsRunning = true;

            MesLabel.Text = _month.ToString("MMMM yyyy");

            var (token, doctorId) = await GetSesionAsync();
            if (string.IsNullOrWhiteSpace(token) || doctorId <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            var from = _month.Date;
            var to = _month.AddMonths(1).AddDays(-1).Date;

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var url = $"api/turnos/doctor/{doctorId}/agenda?desde={from:yyyy-MM-dd}&hasta={to:yyyy-MM-dd}";
            var resp = await _api.GetAsync(url);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                await DisplayAlert("Sesión", "Sesión expirada o token inválido.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            resp.EnsureSuccessStatusCode();

            _turnosMes = (await resp.Content.ReadFromJsonAsync<List<TurnoDoctorItemDto>>()) ?? new();

            BuildCalendarDays();

            CalendarGrid.ItemsSource = _days;

     
            DiaSeleccionadoLabel.Text = "Seleccioná un día";
            TurnosList.ItemsSource = null;
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

    private void BuildCalendarDays()
    {
        _days.Clear();

       
        int MondayBasedDayOfWeek(DateTime d) => ((int)d.DayOfWeek + 6) % 7; // lunes=0

        var first = _month;
        var firstIndex = MondayBasedDayOfWeek(first);

        var start = first.AddDays(-firstIndex);
        var end = _month.AddMonths(1).AddDays(-1);
        var endIndex = MondayBasedDayOfWeek(end);

       
        var last = end.AddDays(6 - endIndex);

        var reservedDays = _turnosMes
            .Where(t => t.Reservado)
            .Select(t => t.FechaHora.Date)
            .Distinct()
            .ToHashSet();

        for (var d = start; d <= last; d = d.AddDays(1))
        {
            _days.Add(new CalendarDayVm
            {
                Date = d.Date,
                IsCurrentMonth = d.Month == _month.Month,
                IsOccupied = reservedDays.Contains(d.Date),
                IsSelected = false
            });
        }
    }

    private async void OnPrevMonth(object sender, EventArgs e)
    {
        _month = _month.AddMonths(-1);
        await LoadMonthAsync();
    }

    private async void OnNextMonth(object sender, EventArgs e)
    {
        _month = _month.AddMonths(1);
        await LoadMonthAsync();
    }

    private void OnDaySelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is not CalendarDayVm day)
            return;

        foreach (var d in _days) d.IsSelected = false;
        day.IsSelected = true;

        // refrescar binding
        CalendarGrid.ItemsSource = null;
        CalendarGrid.ItemsSource = _days;

        DiaSeleccionadoLabel.Text = day.Date.ToString("dddd dd/MM/yyyy");

        var turnosDia = _turnosMes
            .Where(t => t.FechaHora.Date == day.Date)
            .OrderBy(t => t.FechaHora)
            .ToList();

        TurnosList.ItemsSource = turnosDia;
    }
}
