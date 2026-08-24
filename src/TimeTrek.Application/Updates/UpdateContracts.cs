namespace TimeTrek.Application.Updates;

public sealed record UpdateInfo(Version Version, Uri ReleaseUri, Uri PackageUri, string Sha256, string Publisher);

public interface IUpdateService
{
    ValueTask<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default);

    ValueTask<string> DownloadAndVerifyAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    ValueTask InstallAsync(string verifiedPackagePath, CancellationToken cancellationToken = default);
}
