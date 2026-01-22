using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.Extensions.Configuration;

namespace ClinicaMedica.Backend.Services;

public interface IGoogleDriveService
{
    Task<(string fileId, string webViewLink, string downloadLink)> UploadAsync(
        Stream content, string fileName, string contentType, string folderId);

    Task<(byte[] bytes, string contentType, string fileName)> DownloadAsync(string fileId);

    Task DeleteAsync(string fileId);
}

public class GoogleDriveService : IGoogleDriveService
{
    private readonly DriveService _drive;

    public GoogleDriveService(IConfiguration config)
    {
        var keyPath = config["GoogleDrive:ServiceAccountKeyPath"]
                      ?? throw new Exception("Falta GoogleDrive:ServiceAccountKeyPath");

        var credential = GoogleCredential
            .FromFile(keyPath)
            .CreateScoped(DriveService.Scope.Drive);

        _drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ClinicaMedica"
        });
    }

    public async Task<(string fileId, string webViewLink, string downloadLink)> UploadAsync(
        Stream content, string fileName, string contentType, string folderId)
    {
        var fileMeta = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = new List<string> { folderId }
        };

        var request = _drive.Files.Create(fileMeta, content, contentType);
        request.Fields = "id, webViewLink, webContentLink";

        var progress = await request.UploadAsync();

        if (progress.Status != UploadStatus.Completed)
            throw progress.Exception ?? new Exception($"Error subiendo a Drive. Status: {progress.Status}");

        // ✅ ESTA ES LA PROPIEDAD CORRECTA
        var uploaded = request.ResponseBody
                      ?? throw new Exception("Upload completado pero ResponseBody vino null");

        var download = !string.IsNullOrWhiteSpace(uploaded.WebContentLink)
            ? uploaded.WebContentLink
            : $"https://drive.google.com/uc?id={uploaded.Id}&export=download";

        return (uploaded.Id!, uploaded.WebViewLink ?? "", download);
    }

    public async Task<(byte[] bytes, string contentType, string fileName)> DownloadAsync(string fileId)
    {
        var metaReq = _drive.Files.Get(fileId);
        metaReq.Fields = "name, mimeType";
        var meta = await metaReq.ExecuteAsync();

        var getReq = _drive.Files.Get(fileId);

        using var ms = new MemoryStream();
        await getReq.DownloadAsync(ms);

        return (ms.ToArray(), meta.MimeType ?? "application/octet-stream", meta.Name ?? "archivo");
    }

    public async Task DeleteAsync(string fileId)
    {
        var delReq = _drive.Files.Delete(fileId);
        await delReq.ExecuteAsync();
    }
}
