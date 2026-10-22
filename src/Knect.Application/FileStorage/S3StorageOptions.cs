namespace Knect.Application.FileStorage;

public class S3StorageOptions
{
    public const string SectionName = "S3";

    public string Provider { get; set; } = "S3";
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; } = true;
    public bool PublicRead { get; set; } = false;
}
