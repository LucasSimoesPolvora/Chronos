public class Blob
{
    public required string FilePath { get; set; }
    public string FileName { get { return Path.GetFileName(FilePath); } }
    public required string Hash { get; set; }
    public required FileStatusEnum Status = (FileStatusEnum)FileTypeEnum.Blob;
}