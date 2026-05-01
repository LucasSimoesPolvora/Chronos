using System.Security.Cryptography;

public class FileService
{
    public List<TrackedFile> trackedFiles = [];
    private Index index = new();
    private readonly string IndexPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "index.json");
    private readonly string ObjectsPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "objects");

    public FileService()
    {
        LoadIndex();
    }

    private string CalculateFileHash(string filePath)
    {
        using (var sha256 = SHA256.Create())
        using (var stream = File.OpenRead(filePath))
        {
            byte[] hashBytes = sha256.ComputeHash(stream);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }

    private string SaveBlob(string filePath, string blobHash)
    {
        string blobPath = Path.Combine(ObjectsPath, blobHash);
        
        if (File.Exists(blobPath))
        {
            return blobPath;
        }

        if(!Directory.Exists(ObjectsPath)) Directory.CreateDirectory(ObjectsPath);

        File.Copy(filePath, blobPath, overwrite: false);
        return blobPath;
    }

    private void LoadIndex()
    {
        if (File.Exists(IndexPath))
        {
            try
            {
                string json = File.ReadAllText(IndexPath);
                
                // Check if file is empty
                if (string.IsNullOrWhiteSpace(json))
                {
                    index = new Index();
                    return;
                }
                
                index = Index.FromJson(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading index: {ex.Message}");
                index = new Index();
            }
        }
    }

    private void SaveIndex()
    {
        try
        {
            Directory.CreateDirectory(ObjectsPath);
            string json = index.ToJson();
            File.WriteAllText(IndexPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving index: {ex.Message}");
        }
    }

    public string SaveFileStatus(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        string blobHash = CalculateFileHash(filePath);

        SaveBlob(filePath, blobHash);

        string projectRoot = Directory.GetCurrentDirectory();
        string relativePath = Path.GetRelativePath(projectRoot, filePath);

        index.AddOrUpdateEntry(relativePath, blobHash);

        return blobHash;
    }

    public void AddToStaging(string pattern)
    {
        string normalizedPattern = Path.GetFullPath(pattern);
        ProjectService projectService = new();
        projectService.GetFiles(Directory.GetCurrentDirectory(), this);
        
        List<TrackedFile> filesToStage = [];

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
                string fileDir = Path.GetDirectoryName(f.File.FullName) ?? ".";
                return fileDir.StartsWith(directory, StringComparison.OrdinalIgnoreCase) &&
                       IsMatch(f.File.Name, searchPattern);
            }).ToList();
        }
        else if (Directory.Exists(normalizedPattern))
        {
            filesToStage = trackedFiles.Where(f =>
                f.File.FullName.StartsWith(normalizedPattern, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
        else if (File.Exists(normalizedPattern))
        {
            TrackedFile? trackedFile = trackedFiles.Find(f => f.File.FullName == normalizedPattern);
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
            foreach (TrackedFile file in filesToStage)
            {
                try
                {
                    string blobHash = SaveFileStatus(file.File.FullName);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error staging file {file.File.Name}: {ex.Message}");
                }
            }

            SaveIndex();   

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