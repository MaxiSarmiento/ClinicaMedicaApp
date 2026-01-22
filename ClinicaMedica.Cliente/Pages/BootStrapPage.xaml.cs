namespace ClinicaMedica.Cliente.Pages;

public partial class BootstrapPage : ContentPage
{
    private bool _navigated;

    public BootstrapPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(2500);
        await Shell.Current.GoToAsync("//LoginPage");
    }

}
