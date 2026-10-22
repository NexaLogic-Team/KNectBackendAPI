using System.Globalization;
using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Knect.Application.FileStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Knect.Infrastructure.FileStorage;

public sealed class S3FileStorageService : IFileStorageService
{
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3FileStorageService> _logger;

    public S3FileStorageService(IOptions<S3StorageOptions> options, ILogger<S3FileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SaveAsync(string container, string fileName, Stream content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(container)) throw new ArgumentException("Container is required.", nameof(container));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(_options.BucketName)) throw new InvalidOperationException("S3 bucket is not configured.");
        if (string.IsNullOrWhiteSpace(_options.AccessKey) || string.IsNullOrWhiteSpace(_options.SecretKey)) throw new InvalidOperationException("S3 credentials are not configured.");

        var sanitizedFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(sanitizedFileName) || sanitizedFileName.Contains('\\') || sanitizedFileName.Contains('/'))
        {
            throw new ArgumentException("Invalid file name.", nameof(fileName));
        }

        using var aws = CreateClient();
        var key = $"{container.Trim('/')}/{Guid.NewGuid():N}_{sanitizedFileName}";

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = ms,
            ContentType = "application/octet-stream",
            CannedACL = _options.PublicRead ? S3CannedACL.PublicRead : null,
            AutoCloseStream = true
        };

        var response = await aws.PutObjectAsync(request, cancellationToken);
        if (response.HttpStatusCode != HttpStatusCode.OK && response.HttpStatusCode != HttpStatusCode.Accepted)
        {
            throw new InvalidOperationException("S3 upload failed.");
        }

        return key;
    }

    public async Task<Stream> OpenAsync(string key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        using var aws = CreateClient();
        var response = await aws.GetObjectAsync(_options.BucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        using var aws = CreateClient();
        await aws.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
    }

    public async Task<string> GetSignedUrlAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        using var aws = CreateClient();
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(ttl <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : ttl)
        };

        return await Task.FromResult(aws.GetPreSignedURL(request));
    }

    private AmazonS3Client CreateClient()
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = string.IsNullOrWhiteSpace(_options.Region) ? RegionEndpoint.USEast1 : RegionEndpoint.GetBySystemName(_options.Region),
            ForcePathStyle = _options.ForcePathStyle,
            ServiceURL = string.IsNullOrWhiteSpace(_options.ServiceUrl) ? null : _options.ServiceUrl,
            UseHttp = !string.IsNullOrWhiteSpace(_options.ServiceUrl) && _options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
            Timeout = TimeSpan.FromSeconds(30)
        };

        var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
        return new AmazonS3Client(credentials, config);
    }
}
