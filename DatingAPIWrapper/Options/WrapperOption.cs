namespace DatingAPIWrapper.Options;

public class WrapperOption
{
    public string BaseUrl { get; set; } = null!;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public WrapperOption() { }
    
    public WrapperOption(string baseUrl, TimeSpan timeout)
    {
        BaseUrl = baseUrl;
        Timeout = timeout;
    }
}