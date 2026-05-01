using System.Security.Cryptography;

public class FileService
{
    public List<Blob> trackedFiles = [];
    private readonly IndexService _indexService;
    private readonly VersionService _versionService;
    private readonly string ObjectsPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "objects");

    public FileService()
    {
        _indexService = new IndexService();
        _versionService = new VersionService();
    }

    public void GetFiles(string path, FileService versionService)
    {
        DirectoryInfo info = new(path);

        foreach (FileInfo file in info.GetFiles())
        {
            if(versionService.trackedFiles.Find(f => f.FilePath == file.FullName) == null)
            {
                versionService.trackedFiles.Add(new Blob { FilePath = file.FullName, Hash = CalculateFileHash(file.FullName), FileType = FileStatusEnum.untracked });
            }
        }

        foreach (DirectoryInfo dir in info.GetDirectories())
        {
            if(dir.FullName.Contains(".chronos"))
            {
                continue;
            }
            GetFiles(dir.FullName, versionService);
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
            byte[] content = VersionService.ToBinary(File.ReadAllText(filePath), FileTypeEnum.Blob);
            File.WriteAllBytes(blobPath, content);
            return blobPath;
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException($"Permission denied: Unable to access file '{filePath}'. Check file and directory permissions.");
        }
    }

    public void AddToStaging(string pattern)
    {
        string normalizedPattern = Path.GetFullPath(pattern);
        GetFiles(Directory.GetCurrentDirectory(), this);
        
        List<Blob> filesToStage = [];

        if (pattern == ".")
        {
            filesToStage = trackedFiles;
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
            Console.WriteLine($"Pattern '{pattern}' does not match any files, directories, or valid pattern.");
            return;
        }

        if (filesToStage.Count > 0)
        {
            foreach (Blob file in filesToStage)
            {
                try
                {

                    string blobHash = CalculateFileHash(file.FilePath);

                    SaveBlob(file.FilePath, blobHash);
                    
                    string projectRoot = Directory.GetCurrentDirectory();
                    string relativePath = Path.GetRelativePath(projectRoot, file.FilePath);
                    _indexService.AddOrUpdateEntry(relativePath, blobHash);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error staging file {file.FileName}: {ex.Message}");
                }
            }

            _indexService.SaveIndex();   

            Console.WriteLine($"{filesToStage.Count} file(s) added to staging.");
        }
        else
        {
            Console.WriteLine($"No files staged.");
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