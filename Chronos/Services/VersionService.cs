using System.Text;

public class VersionService
{
    private readonly static IndexService? _indexService;
    private readonly static CommitService? _commitService;
    private readonly static TreeService? _treeService;
    public const uint MAGIC_NUMBER = 0x4348524F;
    private readonly static string HeadFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "HEAD");

    static VersionService()
    {
        _commitService = new CommitService();
        _treeService = new TreeService();
        _indexService = new IndexService();
    }

    public VersionService()
    {
        _indexService?.LoadIndex();
    }

    public void CommitVersion(string message)
    {
        if(_commitService == null)
        {
            Console.WriteLine("Commit service not initialized.");
            return;
        }

        if(!CheckIfFilesStaged(new FileService()))
        {
            Console.WriteLine("Cannot commit. No files added.");
            return;
        }

        Commit commit = _commitService.CreateProjectCommit(message);

        File.WriteAllText(HeadFilePath, commit.Hash);
        _indexService?.ClearIndex();
        _indexService?.SaveIndex();
        _commitService.SaveCommit(commit);
    }

    public bool CheckIfFilesStaged(FileService fs)
    {
        GetVersionState(fs);

        bool hasDeletedStaged = _indexService?.GetEntries().ToList().Any(e => e.Status == FileStatusEnum.deleted) ?? false;

        return fs.trackedFiles.Any(f => f.Status == FileStatusEnum.added) || hasDeletedStaged;
    }

    public void GetVersionState(FileService fs)
    {
        fs.GetFiles(Directory.GetCurrentDirectory(), fs);
        IndexEntry[]? entries = _indexService?.GetEntries();

        if(entries == null)
        {
            return;
        }
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
                    trackedFile.Status = CheckIfStaged(entry);
                }
            } else
            {
                fs.trackedFiles.Add(new Blob
                {
                    FilePath = Path.Combine(Directory.GetCurrentDirectory(), entry.RelativePath),
                    Hash = entry.BlobHash,
                    Status = FileStatusEnum.deleted
                });
            }
        }
    }

    public FileStatusEnum CheckIfStaged(IndexEntry entry)
    {
        if (_commitService == null)
        {
            Console.WriteLine("Commit service not initialized.");
            return FileStatusEnum.untracked;
        }
        if (_treeService == null)
        {
            Console.WriteLine("Tree service not initialized.");
            return FileStatusEnum.untracked;
        }

        if(string.IsNullOrEmpty(File.ReadAllText(HeadFilePath)))
        {
            return FileStatusEnum.added;
        }
        
        Tree? previousCommitTree = _treeService.LoadTree(_commitService.LoadCommit(File.ReadAllText(HeadFilePath)).TreeHash);
        Blob? previousBlob = previousCommitTree.Blobs.Find(b => b.FilePath == entry.RelativePath);
        if (previousBlob != null && previousBlob.Hash == entry.BlobHash)
        {
            return FileStatusEnum.commited;
        } else
        {
            return FileStatusEnum.added;
        }
        
    }

    public static void DisplayVersionState(FileService fs)
    {
        if(fs.trackedFiles.FindAll(t => t.Status != FileStatusEnum.commited).Count == 0)
        {
            Console.WriteLine("No changes to display. All files are committed.");
            return;
        }
        foreach(Blob file in fs.trackedFiles.OrderBy(f => f.Status).ThenBy(f => f.FileName))
        {
            switch(file.Status)
            {
                case FileStatusEnum.untracked:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case FileStatusEnum.added:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case FileStatusEnum.modified:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case FileStatusEnum.deleted:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
            }

            if(file.Status == FileStatusEnum.deleted)
            {
                IndexEntry? entry = _indexService?.GetEntries()
                .ToList()
                .Find(e => e.RelativePath == Path.GetRelativePath(Directory.GetCurrentDirectory(), file.FilePath));

                if(entry != null && entry.Status == FileStatusEnum.deleted)
                {
                    Console.WriteLine($"{Path.GetRelativePath(Directory.GetCurrentDirectory(), file.FilePath)} - {file.Status} (previously committed)");
                }
                else
                {
                    Console.WriteLine($"{Path.GetRelativePath(Directory.GetCurrentDirectory(), file.FilePath)} - {file.Status}");
                }
            }
             else if(file.Status != FileStatusEnum.commited)
            {
                Console.WriteLine($"{Path.GetRelativePath(Directory.GetCurrentDirectory(), file.FilePath)} - {file.Status}");
            }
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

    public void CheckoutVersion(string commitHash)
    {
        if (_commitService == null)
        {
            Console.WriteLine("Commit service not initialized.");
            return;
        }
        FileService fs = new();
        GetVersionState(fs);

        string headStatusPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "status");

        string headStatus = File.ReadAllText(headStatusPath);
        if(headStatus == HeadStatus.detached.ToString())
        {
            Console.WriteLine("Cannot checkout. HEAD is currently detached. Please attach HEAD to the last committed version before checking out another version.");
            return;
        }

        if(fs.trackedFiles.Any(f => f.Status == FileStatusEnum.modified || f.Status == FileStatusEnum.added || f.Status == FileStatusEnum.deleted))
        {
            Console.WriteLine("Cannot checkout. You have uncommitted changes. Please commit your changes before checking out another version.");
            return;
        }

        Commit? commit = _commitService.LoadCommit(commitHash);
        if (commit == null)
        {
            Console.WriteLine("Invalid commit hash.");
            return;
        }

        Tree? tree = _treeService?.LoadTree(commit.TreeHash);
        if (tree == null)        {
            Console.WriteLine("Tree associated with the commit not found.");
            return;
        }

        foreach (Blob blob in tree.Blobs)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), blob.FilePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            string content = fs.LoadBlob(blob.Hash);
            string? directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes(content));
        }

        File.WriteAllText(headStatusPath, HeadStatus.detached.ToString());
        File.WriteAllText(HeadFilePath, commitHash);
    }
}