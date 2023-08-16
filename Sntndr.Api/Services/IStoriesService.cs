using Sntndr.Api.Models;

namespace Sntndr.Api.Services
{
    public interface IStoriesService
    {
        Task<IList<Story>> GetBestStories(int amount, CancellationToken cancellationToken);
    }
}