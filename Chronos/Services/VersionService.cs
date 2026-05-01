public class VersionService
{
    private readonly IndexService _indexService;
    private readonly CommitService _commitService;
    public const uint MAGIC_NUMBER = 0x4348524F;
    public string HeadFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "HEAD");

    public VersionService()
    {
        _indexService = new IndexService();
        _commitService = new CommitService();
        _indexService.LoadIndex();
    }

    public void CommitVersion(string message)
    {
        Commit commit = _commitService.CreateProjectCommit(message);

        File.WriteAllText(HeadFilePath, commit.Hash);
        _commitService.SaveCommit(commit);
    }

    
}