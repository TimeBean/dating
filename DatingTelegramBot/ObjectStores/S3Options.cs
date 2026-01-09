namespace DatingTelegramBot.ObjectStores;

public class S3Options {
    public string ServiceUrl { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string BucketName { get; set; }
}