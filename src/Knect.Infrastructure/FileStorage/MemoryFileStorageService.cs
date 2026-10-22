using System.Globalization;
using Knect.Application.FileStorage;

namespace Knect.Infrastructure.FileStorage;

public sealed class MemoryFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, byte[]> _storage = new();

    public Task<string> SaveAsync(string container, string fileName, Stream content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(container))
        {
            throw new ArgumentException("Container is required.", nameof(container));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        using var ms = new MemoryStream();
        content.CopyTo(ms);
        var key = $"{container.Trim('/')}/{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        _storage[key] = ms.ToArray();
        return Task.FromResult(key);
    }

    public Task<Stream> OpenAsync(string key, CancellationToken cancellationToken)
    {
        if (!_storage.TryGetValue(key, out var bytes))
        {
            throw new FileNotFoundException("File not found.", key);
        }

        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _storage.Remove(key);
        return Task.CompletedTask;
    }

    public Task<string> GetSignedUrlAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        return Task.FromResult($"memory://{key}?expires={Uri.EscapeDataString(ttl.TotalSeconds.ToString(CultureInfo.InvariantCulture))}");
    }
}
