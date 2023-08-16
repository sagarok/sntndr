using System.Net.Http.Headers;
using System.Text.Json;
using Sntndr.Api.Services;

namespace Sntndr.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHttpClient("HackerNewsAPI", client =>
            {
                client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            builder.Services.AddOutputCache();
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSingleton<IStoriesService, StoriesService>();
            builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("CacheOptions"));

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();

            app.UseOutputCache();

            app.MapGet("/beststories/{n:int}", (int n, 
                HttpContext httpContext, IStoriesService storiesService, CancellationToken cancellationToken) =>
            {
                var stories = storiesService.GetBestStories(n, cancellationToken);
                return stories;
                
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi()
            .CacheOutput(o =>
            {
                o.SetVaryByRouteValue("n");
                o.Expire(TimeSpan.FromSeconds(10));
            })
            ;

            app.Run();
        }
    }
}