public class Tree
{
    public required string Hash { get; set; }
    public required List<Blob> Blobs { get; set; } = [];
    public required FileStatusEnum FileType = (FileStatusEnum)FileTypeEnum.Tree;
}