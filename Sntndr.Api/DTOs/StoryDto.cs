namespace Sntndr.Api.DTOs
{
    public record struct StoryDto (int id, string Title, string Url, string By, int Time, int Score, int[] Kids);
}
