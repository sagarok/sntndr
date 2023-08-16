using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using Sntndr.Api.Models;
using Sntndr.Api.Services;

namespace Sntndr.Api.Tests.Services
{
    public class StoriesServiceTests
    {
        [Fact]
        public async Task GetBestStories_ReturnsBestStoriesFromCache_WhenCacheContainsEnoughStories()
        {
            // Arrange
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var cacheMock = new Mock<IDistributedCache>();
            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.SetupGet(o => o.Value).Returns(new CacheOptions { StoriesCacheTimeInSec = 3600 });
            var cachedStories = GetSampleCachedStories();
            cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(SerializeStories(cachedStories));
            var service = new StoriesService(httpClientFactoryMock.Object, cacheMock.Object, optionsMock.Object);

            // Act
            var result = await service.GetBestStories(2, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(cachedStories.Values.OrderByDescending(s => s.Score).Take(2));
        }

        [Fact]
        public async Task GetBestStories_ReturnsFreshStories_WhenCacheDoesNotContainEnoughStories()
        {
            // Arrange
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClientFake = new HttpClient(new MockHttpHandler(req =>
            {
                if (req.RequestUri?.AbsolutePath == "/beststories.json")
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[1, 2, 3]") };
                var id = req.RequestUri?.Segments.Last()[0];
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($$"""{"id": {{id}}}""") };
            }));
            httpClientFake.BaseAddress = new Uri("https://fake/");
            httpClientFactoryMock.Setup(f => f.CreateClient("HackerNewsAPI")).Returns(httpClientFake);
            var cacheMock = new Mock<IDistributedCache>();
            var optionsMock = new Mock<IOptions<CacheOptions>>();
            optionsMock.SetupGet(o => o.Value).Returns(new CacheOptions { StoriesCacheTimeInSec = 3600 });
            cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
            var service = new StoriesService(httpClientFactoryMock.Object, cacheMock.Object, optionsMock.Object);

            // Act
            var result = await service.GetBestStories(2, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
        }

        private Dictionary<int, Story> GetSampleCachedStories()
        {
            return new Dictionary<int, Story>
            {
                { 1, new Story {} },
                { 2, new Story {} },
            };
        }

        private byte[] SerializeStories(Dictionary<int, Story> stories)
        {
            var str = JsonSerializer.Serialize((stories.Count, stories), new JsonSerializerOptions { IncludeFields = true });
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
