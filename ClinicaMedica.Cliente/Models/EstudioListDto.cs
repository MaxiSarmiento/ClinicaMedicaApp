namespace ClinicaMedica.Cliente.Models;

public class EstudioListDto
{
    public int Id { get; set; }
    public string NombreArchivo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; } = "";

    public bool EsPdf =>
        NombreArchivo.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public bool EsImagen =>
        NombreArchivo.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
        NombreArchivo.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        NombreArchivo.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

    public string Icono =>
        EsPdf ? "📄" :
        EsImagen ? "🖼️" :
        "📎";
}
