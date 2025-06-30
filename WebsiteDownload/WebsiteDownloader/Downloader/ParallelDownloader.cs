using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net;
using WebsiteDownloader.Abstractions;
using WebsiteDownloader.Utils;

namespace WebsiteDownloader.Downloader;

public class ParallelDownloader : IWebsiteDownloader
{
    private ILogger _logger;
    private IWebsiteWriter _writer;
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public ParallelDownloader(ILogger logger, IWebsiteWriter writer, HttpClient httpClient)
    {
        _logger = logger;
        _writer = writer;
        _httpClient = httpClient;
        _pipeline = GetDefaultResilientPipeline();
    }

    public async Task<int> Download(List<string> urls, int partitionCount = 100)
    {
        int succesfullCount = 0;

        var downloadTasks = urls.Select(u => new Func<Task<bool>>(() => DownloadFromUrlIntoFile(u))).ToList();

        await ParallelExectioner.ParallelForEachAsync(downloadTasks, 100, async func =>
        {
            if (await func())
            {
                succesfullCount++;
            }
        });

        _logger.LogInformation("Succesfully downloaded {Count} out of {Total}.", succesfullCount, urls.Count);
        return succesfullCount;
    }

    private async Task<bool> DownloadFromUrlIntoFile(string url)
    {
        using var response = await _pipeline.ExecuteAsync(async ct =>
            await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false));

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Failed to download website from URL {Url} - status code: {StatusCode}", url, response.StatusCode);
            return false;
        }

        try
        {
            await _writer.WriteFromHttpResponseMessage(response);
            return true;
        }
        catch(Exception ex)
        {
            _logger.LogError("Error during writing for URL {Url} - error Message: {Message}", url, ex.Message);
            return false;
        }
    }

    private ResiliencePipeline<HttpResponseMessage> GetDefaultResilientPipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = args => HandleTransientHttpError(args.Outcome),
            Delay = TimeSpan.FromMilliseconds(200),
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .Build();

    }

    private ValueTask<bool> HandleTransientHttpError(
    Outcome<HttpResponseMessage> outcome)
    => outcome switch
    {
        { Exception: HttpRequestException } => PredicateResult.True(),
        { Result.StatusCode: HttpStatusCode.RequestTimeout } => PredicateResult.True(),
        { Result.StatusCode: >= HttpStatusCode.InternalServerError } => PredicateResult.True(),
        _ => PredicateResult.False()
    };
}
