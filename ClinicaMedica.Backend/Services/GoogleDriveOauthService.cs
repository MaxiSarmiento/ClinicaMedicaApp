using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;

namespace ClinicaMedica.Backend.Services;

public class GoogleDriveOAuthService
{
    private readonly DriveService _drive;

    public GoogleDriveOAuthService(IConfiguration config, IWebHostEnvironment env)
    {
        var secretsPathCfg = config["GoogleDrive:OAuthClientSecretsPath"] ?? "Keys/google-oauth.json";
        var tokenStoreCfg = config["GoogleDrive:OAuthTokenStorePath"] ?? "token-store";

        var secretsPath = Path.IsPathRooted(secretsPathCfg)
            ? secretsPathCfg
            : Path.Combine(env.ContentRootPath, secretsPathCfg);

        var tokenStorePath = Path.IsPathRooted(tokenStoreCfg)
            ? tokenStoreCfg
            : Path.Combine(env.ContentRootPath, tokenStoreCfg);

        using var stream = new FileStream(secretsPath, FileMode.Open, FileAccess.Read);

        var cred = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            new[] { DriveService.Scope.Drive },
            "clinica-medica-user",
            CancellationToken.None,
            new FileDataStore(tokenStorePath, true)
        ).GetAwaiter().GetResult();

        _drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = cred,
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
    public async Task<bool> PingAsync()
    {
        var req = _drive.About.Get();
        req.Fields = "user,storageQuota";
        var about = await req.ExecuteAsync();
        return about != null;
    }
}