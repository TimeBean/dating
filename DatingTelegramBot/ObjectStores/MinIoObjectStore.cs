using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using DatingTelegramBot.Models;
using Microsoft.Extensions.Options;

namespace DatingTelegramBot.ObjectStores
{
    public class MinIoObjectStore : IObjectStore
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IOptions<S3Options> _s3Options;

        private static string GetUserPhotosFolder(long userId) => $"users/{userId}/photos/";
        private static string GetUserPhotoKey(long userId, Guid fileGuid) => $"{GetUserPhotosFolder(userId)}{fileGuid}.jpg";

        public MinIoObjectStore(IAmazonS3 s3Client, IOptions<S3Options> options)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _s3Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Get all user's pictures.
        /// </summary>
        /// <remarks>
        /// The calling code MUST Dispose() each returned <see cref="FileStream"/> (streams use DeleteOnClose).
        /// This method downloads objects to temporary files and returns readable FileStreams that delete the temp file on close.
        /// </remarks>
        public async Task<List<FileStream>> Get(UserSession userSession, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(userSession);
            ct.ThrowIfCancellationRequested();

            var prefix = GetUserPhotosFolder(userSession.ChatId);
            var streams = new List<FileStream>();

            var listRequest = new ListObjectsV2Request
            {
                BucketName = _s3Options.Value.BucketName,
                Prefix = prefix
            };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await _s3Client.ListObjectsV2Async(listRequest, ct).ConfigureAwait(false);

                foreach (var s3Object in listResponse.S3Objects)
                {
                    if (s3Object.Key.EndsWith("/")) continue;

                    var stream = await DownloadToTempFile(s3Object.Key, ct).ConfigureAwait(false);
                    streams.Add(stream);
                }

                listRequest.ContinuationToken = listResponse.NextContinuationToken;
            }
            while ((listResponse.IsTruncated ?? false) && !ct.IsCancellationRequested);

            return streams;
        }

        /// <summary>
        /// Uploads the provided stream as a user's photo and returns the generated Guid.
        /// The caller retains ownership of the stream; this method will not close it.
        /// </summary>
        public async Task<Guid> Put(long userId, Stream stream, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ct.ThrowIfCancellationRequested();

            var guid = Guid.NewGuid();
            var key = GetUserPhotoKey(userId, guid);

            if (stream.CanSeek)
                stream.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _s3Options.Value.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = "image/jpeg",
                AutoCloseStream = false
            };

            await _s3Client.PutObjectAsync(request, ct).ConfigureAwait(false);
            return guid;
        }

        /// <summary>
        /// Downloads S3 object into a temp file and returns a FileStream opened for read.
        /// The returned FileStream has FileOptions.DeleteOnClose so the temp file is removed when stream disposed.
        /// </summary>
        private async Task<FileStream> DownloadToTempFile(string objectKey, CancellationToken ct)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

            try
            {
                using var response = await _s3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = _s3Options.Value.BucketName,
                    Key = objectKey
                }, ct).ConfigureAwait(false);

                await using (var tempFile = new FileStream(
                                 tempFilePath,
                                 FileMode.CreateNew,
                                 FileAccess.Write,
                                 FileShare.None,
                                 bufferSize: 81920,
                                 FileOptions.None))
                {
                    await response.ResponseStream.CopyToAsync(tempFile, ct).ConfigureAwait(false);
                    await tempFile.FlushAsync(ct).ConfigureAwait(false);
                }

                return new FileStream(
                    tempFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    FileOptions.DeleteOnClose | FileOptions.SequentialScan);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"S3 object not found: {objectKey}", ex);
            }
        }
    }
}