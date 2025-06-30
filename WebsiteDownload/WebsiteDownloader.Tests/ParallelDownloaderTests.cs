using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WebsiteDownloader.Abstractions;
using WebsiteDownloader.Downloader;

namespace WebsiteDownloader.Tests;

public class ParallelDownloaderTests
{
    Mock<ILogger> loggerMock = new Mock<ILogger>();
    Mock<IWebsiteWriter> writerMock = new Mock<IWebsiteWriter>();

    [Fact]
    public async Task Download_AllSuccessDownloads()
    {
        writerMock.Setup(w => w.WriteFromHttpResponseMessage(It.IsAny<HttpResponseMessage>())).Returns(Task.CompletedTask);

        var sut = new ParallelDownloader(loggerMock.Object, writerMock.Object, new HttpClient());
        
        List<string> urls = new List<string>();
        urls.Add("https://www.entaingroup.com/");
        urls.Add("https://www.entaingroup.com/about-entain/");
        urls.Add("https://www.entaingroup.com/what-we-do/");
        urls.Add("https://www.entaingroup.com/investor-relations/");
        urls.Add("https://www.entaingroup.com/sustainability-esg/");

        var successfulDownloads = await sut.Download(urls);

        writerMock.Verify(x => x.WriteFromHttpResponseMessage(It.IsAny<HttpResponseMessage>()), Times.Exactly(5));
        Assert.Equal(5, successfulDownloads);
    }

    [Fact]
    public async Task Download_FailedDownloadIsRetried()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var failedResponse = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };

        handlerMock.Protected().SetupSequence<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(failedResponse).ReturnsAsync(failedResponse).ReturnsAsync(failedResponse);

        var sut = new ParallelDownloader(loggerMock.Object, writerMock.Object, new HttpClient(handlerMock.Object));

        List<string> urls = new List<string> { "https://www.entaingroup.com/" };

        var successfulDownloads = await sut.Download(urls);

        writerMock.Verify(x => x.WriteFromHttpResponseMessage(It.IsAny<HttpResponseMessage>()), Times.Exactly(0));
        Assert.Equal(0, successfulDownloads);
    }

    [Fact]
    public async Task Download_FailedDownloadDoesNotBreakProcess()
    {
        var handlerMock  = new Mock<HttpMessageHandler>();
        var failedResponse = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };

        handlerMock.Protected().SetupSequence<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == new Uri("https://www.BadWebsite.com")),
            ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(failedResponse).ReturnsAsync(failedResponse).ReturnsAsync(failedResponse);

        handlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == new Uri("https://www.GoodWebsite.com")),
            ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage{ StatusCode = System.Net.HttpStatusCode.OK});

        writerMock.Setup(w => w.WriteFromHttpResponseMessage(It.IsAny<HttpResponseMessage>())).Returns(Task.CompletedTask);

        var sut = new ParallelDownloader(loggerMock.Object, writerMock.Object, new HttpClient(handlerMock.Object));

        List<string> urls = new List<string>();
        urls.Add("https://www.BadWebsite.com");
        urls.Add("https://www.GoodWebsite.com");

        var successfulDownloads = await sut.Download(urls);

        writerMock.Verify(x => x.WriteFromHttpResponseMessage(It.IsAny<HttpResponseMessage>()), Times.Exactly(1));
        Assert.Equal(1, successfulDownloads);
    }
}
