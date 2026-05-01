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

    public void GetVersionState(FileService fs)
    {
        fs.GetFiles(Directory.GetCurrentDirectory(), fs);
        IndexEntry[] entries = _indexService.GetEntries();

        foreach(IndexEntry entry in entries)
        {
            bool isModified = File.Exists(HeadFilePath) ? _commitService.IsFileModified(entry.RelativePath, entry.BlobHash) : false;
            Blob? trackedFile = fs.trackedFiles.Find(f => Path.GetRelativePath(Directory.GetCurrentDirectory(), f.FilePath) == entry.RelativePath);
            if (trackedFile != null)
            {
                if(isModified)
                {
                    trackedFile.Status = FileStatusEnum.modified;
                }
                else
                {
                    trackedFile.Status = FileStatusEnum.staged;
                }
            }
        }
    }

    public static void DisplayVersionState(FileService fs)
    {
         _ = fs.trackedFiles.OrderBy(f => f.Status).ThenBy(f => f.FileName).ToList();
        foreach(Blob file in fs.trackedFiles)
        {
            switch(file.Status)
            {
                case FileStatusEnum.untracked:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case FileStatusEnum.staged:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case FileStatusEnum.modified:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }
            Console.WriteLine($"{file.FileName} - {file.Status}");
        }

        Console.ResetColor();
    }
    
}