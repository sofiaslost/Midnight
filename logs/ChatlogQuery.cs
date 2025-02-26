namespace CollabVMBot.logs;

public class ChatlogQuery
{
    public string? VM { get; set; }
    public string? Username { get; set; }
    public DateTime? FromTimestamp { get; set; }
    public DateTime? ToTimeStamp { get; set; }
    public int? Count { get; set; }
    public bool Random { get; set; }
    public string? Regex { get; set; }
    public string[]? CustomWhere { get; set; }
}