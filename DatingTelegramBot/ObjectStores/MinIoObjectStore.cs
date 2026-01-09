using Amazon.S3;
using Amazon.S3.Model;
using DatingTelegramBot.Models;
using Microsoft.Extensions.Options;

namespace DatingTelegramBot.ObjectStores;

public class MinIoObjectStore : IObjectStore
{
    private readonly IAmazonS3 _s3Client;
    private readonly IOptions<S3Options> _s3Options;
    
    public MinIoObjectStore(IAmazonS3 s3Client, IOptions<S3Options> options)
    {
        _s3Client = s3Client;
        _s3Options = options;
    }

    public async Task<string> Put(long userId, FileStream fileStream, CancellationToken ct)
    {
        var guid = Guid.NewGuid();
        var objectKey = $"users/{userId}/photos/{guid}.jpg";

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _s3Options.Value.BucketName,
            Key = objectKey,
            InputStream = fileStream,
            ContentType = "image/jpeg"
        }, ct);

        return objectKey;
    }
    
    /// <summary>
    /// Get all user's pictures.
    /// </summary>
    /// <returns>Streams. The calling code MUST call Dispose.</returns>
    public async Task<FileStream> Get(UserSession userSession, CancellationToken ct)
    {
        var objectKey = $"users/{userSession.ChatId}/photos/{userSession.Pictures}.jpg";
        return await DownloadToTempFile(objectKey, ct);
    }
    
    /// <summary>
    /// Get all user's pictures.
    /// </summary>
    /// <returns>List of streams. The calling code MUST call Dispose for each element.</returns>
    public async Task<List<FileStream>> GetAll(UserSession userSession, CancellationToken ct)
    {
        var prefix = $"users/{userSession.ChatId}/photos/";
        var streams = new List<FileStream>();

        var listRequest = new ListObjectsV2Request
        {
            BucketName = _s3Options.Value.BucketName,
            Prefix = prefix
        };

        var listResponse = await _s3Client.ListObjectsV2Async(listRequest, ct);

        foreach (var s3Object in listResponse.S3Objects)
        {
            if (s3Object.Key.EndsWith("/")) continue;

            var stream = await DownloadToTempFile(s3Object.Key, ct);
            streams.Add(stream);
        }

        return streams;
    }

    private async Task<FileStream> DownloadToTempFile(string objectKey, CancellationToken ct)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

        try
        {
            using var response = await _s3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _s3Options.Value.BucketName,
                Key = objectKey
            }, ct);

            await using (var tempFile = new FileStream(
                             tempFilePath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 81920,
                             FileOptions.None))
            {
                await response.ResponseStream.CopyToAsync(tempFile, ct);
                await tempFile.FlushAsync(ct);
            }

            return new FileStream(
                tempFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.DeleteOnClose);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"S3 object not found: {objectKey}", ex);
        }
    }
}