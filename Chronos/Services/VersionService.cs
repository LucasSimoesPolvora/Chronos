public class VersionService
{
    private readonly IndexService _indexService;
    private readonly static CommitService? _commitService;
    public const uint MAGIC_NUMBER = 0x4348524F;
    private readonly static string HeadFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "HEAD");

    static VersionService()
    {
        _commitService = new CommitService();
    }

    public VersionService()
    {
        _indexService = new IndexService();
        _indexService.LoadIndex();
    }

    public void CommitVersion(string message)
    {
        if(_commitService == null)
        {
            Console.WriteLine("Commit service not initialized.");
            return;
        }
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
            if(_commitService == null)
            {
                Console.WriteLine("Commit service not initialized.");
                return;
            }
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

    public static void DisplayVersionHistory()
    {
        string lastCommitHash = File.ReadAllText(HeadFilePath);

        if(string.IsNullOrEmpty(lastCommitHash))
        {
            Console.WriteLine("No commits found.");
            return;
        } else
        {
            DisplayVersionHistoryRecursive(lastCommitHash);
        }
    }

    public static void DisplayVersionHistoryRecursive(string commitHash, int indentLevel = 0)
    {
        if (_commitService == null)
        {
            Console.WriteLine("Commit service not initialized.");
            return;
        }

        if(string.IsNullOrEmpty(commitHash))
        {
            return;
        }

        Commit? commit = _commitService.LoadCommit(commitHash);
        if (commit == null)
        {
            Console.WriteLine("Invalid commit hash in history.");
            return;
        }

        Console.WriteLine($"- {commit.Timestamp:yyyy-MM-dd HH:mm:ss} {commit.Message} ({commit.Hash})");

        DisplayVersionHistoryRecursive(commit.ParentHash, indentLevel + 1);
        
    }
    
}