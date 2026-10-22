namespace Knect.Application.FileStorage;

public interface IFileStorageService
{
    Task<string> SaveAsync(string container, string fileName, Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
    Task<string> GetSignedUrlAsync(string key, TimeSpan ttl, CancellationToken cancellationToken);
}
