using WebsiteDownloader.Abstractions;
using WebsiteDownloader.Utils;

namespace WebsiteDownloader.Writer;

public class LocalFileSystemWriter : IWebsiteWriter
{
    private readonly string _destinationFolder;

    public LocalFileSystemWriter(string destinationFolder)
    {
        _destinationFolder = destinationFolder;
    }

    public async Task WriteFromHttpResponseMessage(HttpResponseMessage response)
    {
        string fileName = CreateLocalFileNameFromHttpResponseMessage(response);
        using var downloadStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        await WriteFromStream(downloadStream, fileName);
    }

    public async Task WriteFromStream(Stream stream, string destination)
    {
        using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write);

        await stream.CopyToAsync(fileStream);
    }

    private string CreateLocalFileNameFromHttpResponseMessage(HttpResponseMessage response)
    {
        var fileName = PathUtils.GetFilenameFromContentDisposition(response);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = PathUtils.CreateRandomNameWithExtension("html");
        }

        return Path.Combine(_destinationFolder, fileName);
    }
}
