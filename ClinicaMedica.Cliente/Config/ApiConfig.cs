namespace ClinicaMedica.Cliente.Config
{
    public static class ApiConfig
    {
#if ANDROID
public const string BaseUrl="192.168.0.107:5293";
#else
        //cambiar aca si no conecta
        public const string BaseUrl = "http://localhost:5293/";
#endif
    }
}
