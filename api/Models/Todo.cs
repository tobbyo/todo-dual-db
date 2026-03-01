namespace TodoApi.Models;

public class Todo
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Priority { get; set; }  // null | "Low" | "Medium" | "High"
    public int SortOrder { get; set; }
}

public record TodoCreateDto(string Title, string? Description, DateTime? DueDate, string? Priority);
public record TodoUpdateDto(string? Title, string? Description, bool? IsComplete, DateTime? DueDate, string? Priority);
public record TodoReorderDto(int Id, int SortOrder);
