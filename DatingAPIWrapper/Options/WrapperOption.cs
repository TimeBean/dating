namespace DatingAPIWrapper.Options;

public class WrapperOption
{
    public string BaseUrl { get; set; }
    public TimeSpan Timeout { get; set; }

    public WrapperOption() { }
    
    public WrapperOption(string baseUrl, TimeSpan timeout)
    {
        BaseUrl = baseUrl;
        Timeout = timeout;
    }
}