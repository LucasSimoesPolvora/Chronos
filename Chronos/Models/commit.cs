public class Commit
{
    public required string Hash { get; set; }
    public required string Message { get; set; }
    public required DateTime Timestamp { get; set; }
    public required string TreeHash { get; set; }
    public required string ParentHash { get; set; } = string.Empty;
    public required FileStatusEnum FileType = (FileStatusEnum)FileTypeEnum.Commit;
}