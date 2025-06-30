namespace WebsiteDownloader.Abstractions;

public interface IWebsiteWriter
{
    Task WriteFromHttpResponseMessage(HttpResponseMessage response);
    Task WriteFromStream(Stream stream, string destination);
}
