using Microsoft.Extensions.Logging;
using WebsiteDownloader.Abstractions;
using WebsiteDownloader.Downloader;
using WebsiteDownloader.Writer;

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());

ILogger logger = factory.CreateLogger<Program>();

logger.LogInformation(Directory.GetCurrentDirectory());

if (args.Length < 2)
{
    logger.LogInformation("Arguments missing. 1st arg is destination folder, 2nd is file where urls are stored, optional 3rd is for number of partitions for parallel download");
}

var lines = File.ReadLines(args[1]).ToList();
IWebsiteWriter writer = new LocalFileSystemWriter(args[0]);
IWebsiteDownloader downloader = new ParallelDownloader(logger, writer, new HttpClient());

if (args.Length > 2 && int.TryParse(args[2], out var count))
{
    await downloader.Download(lines, count);
}
else
{
    await downloader.Download(lines);
}

return 0;