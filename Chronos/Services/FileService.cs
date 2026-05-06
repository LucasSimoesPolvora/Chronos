using System.Security.Cryptography;

public class FileService
{
    public List<Blob> trackedFiles = [];
    private readonly IndexService _indexService;
    private readonly string ObjectsPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "objects");

    public FileService()
    {
        _indexService = new IndexService();
        _indexService.LoadIndex();
    }

    public void GetFiles(string path, FileService fs)
    {
        DirectoryInfo info = new(path);

        foreach (FileInfo file in info.GetFiles())
        {
            if(fs.trackedFiles.Find(f => f.FilePath == file.FullName) == null)
            {
                fs.trackedFiles.Add(new Blob { FilePath = file.FullName, Hash = CalculateFileHash(file.FullName), Status = FileStatusEnum.untracked });
            }
        }

        foreach (DirectoryInfo dir in info.GetDirectories())
        {
            if(dir.FullName.Contains(".chronos"))
            {
                continue;
            }
            GetFiles(dir.FullName, fs);
        }
    }

    public string CalculateFileHash(string filePath)
    {
        try
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                return Convert.ToHexString(hashBytes).ToLower();
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException($"Permission denied: Unable to read file '{filePath}'. Check file permissions.");
        }
    }

    public string SaveBlob(string filePath, string blobHash)
    {
        try
        {
            if(!Directory.Exists(Path.Combine(ObjectsPath, blobHash[..2]))) Directory.CreateDirectory(Path.Combine(ObjectsPath, blobHash[..2]));

            string blobPath = Path.Combine(ObjectsPath, blobHash[..2], blobHash[2..]);
            
            if (File.Exists(blobPath))
            {
                return blobPath;
            }
            byte[] content = ToBinary(File.ReadAllText(filePath), FileTypeEnum.Blob);
            File.WriteAllBytes(blobPath, content);
            return blobPath;
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException($"Permission denied: Unable to access file '{filePath}'. Check file and directory permissions.");
        }
    }

    private static byte[] ToBinary(string content, FileTypeEnum type)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(VersionService.MAGIC_NUMBER);
            writer.Write((byte)type);
            writer.Write(content);

            return ms.ToArray();
        }

    }

    public void AddToStaging(string pattern)
    {
        string normalizedPattern = Path.GetFullPath(pattern);
        trackedFiles.Clear();
        GetFiles(Directory.GetCurrentDirectory(), this);
        VersionService vs = new();
        vs.GetVersionState(this);
        
        List<Blob> filesToStage = [];

        if (pattern == ".")
        {
            filesToStage = [.. trackedFiles.Where(t => t.Status != FileStatusEnum.added)];
        }
        else if (pattern.Contains('*'))
        {
            string directory = Path.GetDirectoryName(normalizedPattern) ?? ".";
            string searchPattern = Path.GetFileName(normalizedPattern);
            filesToStage = trackedFiles.Where(f =>
            {
                string fileDir = Path.GetDirectoryName(f.FilePath) ?? ".";
                return fileDir.StartsWith(directory, StringComparison.OrdinalIgnoreCase) &&
                       IsMatch(f.FileName, searchPattern);
            }).ToList();
        }
        else if (Directory.Exists(normalizedPattern))
        {
            filesToStage = trackedFiles.Where(f =>
                f.FilePath.StartsWith(normalizedPattern, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
        else if (File.Exists(normalizedPattern))
        {
            Blob? trackedFile = trackedFiles.Find(f => f.FilePath == normalizedPattern);
            if (trackedFile != null)
            {
                filesToStage.Add(trackedFile);
            }
        }
        else
        {
            IndexEntry? entry = _indexService.GetEntries().FirstOrDefault(e => e.RelativePath == Path.GetRelativePath(Directory.GetCurrentDirectory(), normalizedPattern));
            if(entry == null)
            {
                Console.WriteLine($"Pattern '{pattern}' does not match any files, directories, or valid pattern.");
                return;
            } else {
                if(entry.Status != FileStatusEnum.deleted)
                {
                    filesToStage.Add(new Blob 
                    { 
                        FilePath = normalizedPattern,
                        Hash = entry.BlobHash,
                        Status = FileStatusEnum.deleted 
                    });    
                }
            }
        }

        filesToStage = [.. filesToStage.Where(f =>
            trackedFiles.Find(t => t.FilePath == f.FilePath)?.Status != FileStatusEnum.added && trackedFiles.Find(t => t.FilePath == f.FilePath)?.Status != FileStatusEnum.commited
        )];

        if (filesToStage.Count > 0)
        {
            foreach (Blob file in filesToStage)
            {
                try
                {
                    if(file.Status == FileStatusEnum.deleted)
                    {
                        _indexService.MarkEntryDeleted(Path.GetRelativePath(Directory.GetCurrentDirectory(), file.FilePath));
                    }
                    else
                    {
                        string blobHash = CalculateFileHash(file.FilePath);

                        SaveBlob(file.FilePath, blobHash);
                    
                        string projectRoot = Directory.GetCurrentDirectory();
                        string relativePath = Path.GetRelativePath(projectRoot, file.FilePath);
                        _indexService.AddOrUpdateEntry(relativePath, blobHash);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error staging file {file.FileName}: {ex.Message}");
                }
            }

            _indexService.SaveIndex();   

            Console.WriteLine($"{filesToStage.Count} file(s) added.");
        }
        else
        {
            Console.WriteLine($"No files added. They either do not exist, are already added, or do not match the pattern '{pattern}'.");
        }
    }

    private bool IsMatch(string filename, string pattern)
    {
        if (pattern == "*")
            return true;

        if (!pattern.Contains("*"))
            return filename == pattern;

        string[] parts = pattern.Split('*');
        if (parts.Length == 2)
        {
            if (parts[0] == "" && parts[1] == "")
                return true;
            if (parts[0] == "")
                return filename.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase);
            if (parts[1] == "")
                return filename.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase);
            return filename.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase) &&
                   filename.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}