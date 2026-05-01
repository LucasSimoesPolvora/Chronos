public class VersionService
{
    private readonly IndexService _indexService;
    public const uint MAGIC_NUMBER = 0x4348524F;

    public VersionService()
    {
        _indexService = new IndexService();
        _indexService.LoadIndex();
    }

    public void CommitVersion(string message)
    {
        
    }

    
}