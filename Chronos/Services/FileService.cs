using System.Security.Cryptography;

public class FileService
{
    public List<TrackedFile> trackedFiles = [];
    

    public void AddToStaging(string pattern)
    {
        string normalizedPattern = Path.GetFullPath(pattern);
        ProjectService projectService = new();
        projectService.GetFiles(Directory.GetCurrentDirectory(), this);
        
        List<TrackedFile> filesToStage = [];

        Console.WriteLine("Current tracked files:");

        foreach(TrackedFile file in trackedFiles)
        {
            Console.WriteLine($"Tracked file: {file.File.FullName} - {file.FileType}");
        }

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
                    // Update tracked file status
                    UpdateTrackedFiles(file.File.FullName, FileTypeEnum.staged);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error staging file {file.File.Name}: {ex.Message}");
                }
            }
            
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

    public void UpdateTrackedFiles(string path, FileTypeEnum fileType)
    {
        
    }
}