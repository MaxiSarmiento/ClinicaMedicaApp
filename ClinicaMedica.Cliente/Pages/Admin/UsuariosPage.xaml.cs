using System.Collections.ObjectModel;
using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;

namespace ClinicaMedica.Cliente.Pages.Admin;

public partial class UsuariosPage : ContentPage
{
    private readonly UsuariosService _usuariosService;

    public ObservableCollection<UsuarioAdminDto> Usuarios { get; } = new();

    public UsuariosPage(UsuariosService usuariosService)
    {
        InitializeComponent();
        _usuariosService = usuariosService;

        UsuariosList.ItemsSource = Usuarios;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var lista = await _usuariosService.GetUsuarios();

            Usuarios.Clear();
            foreach (var u in lista)
            {
                // normalizar rol por si viene raro
                u.Rol = NormalizarRol(u.Rol);

                Usuarios.Add(u);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private static string NormalizarRol(string? rol)
    {
        rol = (rol ?? "").Trim();

        if (rol.Equals("paciente", StringComparison.OrdinalIgnoreCase)) return "Paciente";
        if (rol.Equals("doctor", StringComparison.OrdinalIgnoreCase) ||
            rol.Equals("medico", StringComparison.OrdinalIgnoreCase) ||
            rol.Equals("médico", StringComparison.OrdinalIgnoreCase)) return "Doctor";
        if (rol.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            rol.Equals("administrador", StringComparison.OrdinalIgnoreCase)) return "Admin";

        return rol;
    }

    private async void OnGuardarRol(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not UsuarioAdminDto u)
        {
            await DisplayAlert("Error", "No se pudo identificar el usuario.", "OK");
            return;
        }

        try
        {
            await _usuariosService.CambiarRol(u.Id, u.Rol);
            await DisplayAlert("OK", "Rol actualizado", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnToggleBloqueo(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not UsuarioAdminDto u)
        {
            await DisplayAlert("Error", "No se pudo identificar el usuario.", "OK");
            return;
        }

        var accion = u.Bloqueado ? "desbloquear" : "bloquear";
        var ok = await DisplayAlert("Confirmar",
            $"¿Seguro que querés {accion} a {u.NombreCompleto}?",
            "Sí", "No");

        if (!ok) return;

        try
        {
            var nuevoEstado = !u.Bloqueado;
            await _usuariosService.SetBloqueo(u.Id, nuevoEstado);

            
            u.Bloqueado = nuevoEstado;

            await DisplayAlert("OK",
                $"Usuario {(u.Bloqueado ? "bloqueado" : "desbloqueado")}.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnEliminarUsuario(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not UsuarioAdminDto u)
        {
            await DisplayAlert("Error", "No se pudo identificar el usuario.", "OK");
            return;
        }

        var ok = await DisplayAlert("Eliminar usuario",
            $"¿Seguro que querés eliminar a {u.NombreCompleto}?\n\nEsta acción no se puede deshacer.",
            "Eliminar", "Cancelar");

        if (!ok) return;

        try
        {
            await _usuariosService.EliminarUsuario(u.Id);

            
            Usuarios.Remove(u);

            await DisplayAlert("OK", "Usuario eliminado.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeAdmin");
    }
}
