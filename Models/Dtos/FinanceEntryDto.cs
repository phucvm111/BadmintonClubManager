namespace BadmintonClub.Models.Dtos;

public sealed class FinanceEntryDto
{
    public int? EntryId { get; init; }       // null nếu là dòng hội phí dự kiến
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = "";
    public string? CategoryName { get; init; }
    public string? MemberName { get; init; }
    public string? AttachmentPath { get; init; }  // lấy từ sidecar
    public string? Note { get; init; }            // lấy từ sidecar
    public bool IsProjected { get; init; }
}
