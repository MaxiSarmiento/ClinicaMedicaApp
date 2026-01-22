namespace ClinicaMedica.Cliente.Pages.Admin;

public partial class HomeAdminPage : ContentPage
{
    public HomeAdminPage()
    {
        InitializeComponent();
    }

    private async void OnSubirEstudio(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("SubirEstudio");

    }

    private async void OnEditarEstudios(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("EditarEstudios");
    }

    private async void OnObrasSociales(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ObrasSociales");
    }

    private async void OnEspecialidades(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Especialidades");
    }

    private async void OnUsuarios(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("Usuarios");
}
