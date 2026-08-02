using System.Text.Json.Serialization;

namespace LibraryService.WebAPI.DTO;

public class BookForm
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("libraryId")]
    public int LibraryId { get; set; }
}
