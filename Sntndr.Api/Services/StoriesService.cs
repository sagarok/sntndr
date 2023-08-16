using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Sntndr.Api.DTOs;
using Sntndr.Api.Models;

namespace Sntndr.Api.Services
{
    public sealed class StoriesService : IStoriesService
    {
        const string CachedStoriesKey = "CachedStories";

        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions;
        private readonly JsonSerializerOptions _serializerOptions;

        public StoriesService(IHttpClientFactory httpClientFactory, IDistributedCache cache, IOptions<CacheOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("HackerNewsAPI");
            _cache = cache;
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(options.Value.StoriesCacheTimeInSec)
            };
            _serializerOptions = new JsonSerializerOptions
            {
                IncludeFields = true
            };
        }

        public async Task<IList<Story>> GetBestStories(int amount, CancellationToken cancellationToken)
        {
            var cachedStoriesSerialized = await _cache.GetStringAsync(CachedStoriesKey, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<int, Story>? bestStories = null;
            int amountSerialized;
            if (cachedStoriesSerialized != null)
            {
                (amountSerialized, bestStories) = JsonSerializer.Deserialize<(int Amount, Dictionary<int, Story> Stories)>(cachedStoriesSerialized, _serializerOptions);
                if (amountSerialized >= amount)
                    return bestStories.Values.OrderByDescending(s => s.Score).Take(amount).ToArray();
            }

            var bestStoryIdsResponse = await _httpClient.GetAsync("beststories.json");
            var bestStoryIds = await bestStoryIdsResponse.Content.ReadFromJsonAsync<int[]>();
            cancellationToken.ThrowIfCancellationRequested();

            if (bestStoryIds is null || bestStoryIds.Length == 0)
                return Array.Empty<Story>();

            var firstNStories = bestStoryIds.Take(amount).ToArray();
            var idsToDownload = (bestStories?.Keys.Count > 0) ? firstNStories.Except(bestStories.Keys) : firstNStories;
                
            var downloadedStories = await DownloadStories(idsToDownload, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (bestStories is null)
                bestStories = new(amount);

            if (downloadedStories?.Length > 0)
                foreach(var (id, story) in downloadedStories)
                    bestStories.TryAdd(id, story);

            cachedStoriesSerialized = JsonSerializer.Serialize((amount, bestStories), _serializerOptions);
            _ = _cache.SetStringAsync(CachedStoriesKey, cachedStoriesSerialized, _cacheOptions ,cancellationToken);

            return bestStories.Values.OrderByDescending(s => s.Score).ToArray();
        }

        private async Task<(int, Story)[]?> DownloadStories(IEnumerable<int> storiesIds, CancellationToken cancellationToken)
        {
            var tasks = storiesIds.Select(id => _httpClient.GetFromJsonAsync<StoryDto>($"item/{id}.json", cancellationToken));

            var downloadedStories = await Task.WhenAll(tasks);
            cancellationToken.ThrowIfCancellationRequested();

            return downloadedStories?.OfType<StoryDto>()
                .Select(d => (d.id, new Story
                {
                    Title = d.Title,
                    Uri = d.Url,
                    PostedBy = d.By,
                    Time = DateTimeOffset.FromUnixTimeSeconds(d.Time).DateTime,
                    Score = d.Score,
                    CommentCount = d.Kids?.Length ?? 0
                }))                
                .ToArray();
        }
    }
}
