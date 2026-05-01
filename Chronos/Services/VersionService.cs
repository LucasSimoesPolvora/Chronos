public class VersionService
{
    private readonly IndexService _indexService;
    private const uint MAGIC_NUMBER = 0x4348524F;

    public VersionService()
    {
        _indexService = new IndexService();
        _indexService.LoadIndex();
    }

    public static byte[] ToBinary(string content, FileTypeEnum type)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(MAGIC_NUMBER);
            writer.Write((byte)type);
            writer.Write((ushort)content.Length);
            writer.Write(content);

            return ms.ToArray();
        }

    }
}