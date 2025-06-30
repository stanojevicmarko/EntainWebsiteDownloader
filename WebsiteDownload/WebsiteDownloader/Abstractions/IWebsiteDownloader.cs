namespace WebsiteDownloader.Abstractions;

public interface IWebsiteDownloader
{
    Task<int> Download(List<string> urls, int partitionCount = 100);
}
