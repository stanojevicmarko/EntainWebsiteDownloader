using System.Net.Http.Headers;

namespace WebsiteDownloader.Utils;

internal static class PathUtils
{
    internal static string CreateRandomNameWithExtension(string extension)
    {
        return Path.ChangeExtension(Guid.NewGuid().ToString(), extension);
    }

    internal static string GetFilenameFromContentDisposition(HttpResponseMessage response)
    {
        return response.Content.Headers.TryGetValues("Content-Disposition", out var contentDispositionString) &&
            ContentDispositionHeaderValue.TryParse(contentDispositionString.FirstOrDefault(), out var contentDisposition)
            ? contentDisposition?.FileName
            : string.Empty;
    }
}

