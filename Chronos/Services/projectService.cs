public class ProjectService
{
    public FileService fileService;

    public ProjectService()
    {
        fileService = new();
    }
    
    public static void InitProject()
    {
        string basepath = Directory.GetCurrentDirectory();
        string projectPath = Path.Combine(basepath, ".chronos");
        if (Directory.Exists(projectPath))
        {
            Console.WriteLine("Project already initialized.");
            return;
        }
        DirectoryInfo dir = Directory.CreateDirectory(projectPath);
        dir.Attributes |= FileAttributes.Hidden;

        dir.CreateSubdirectory("objects");
        Console.WriteLine("Project initialized successfully.");   
    }

    public void GetFiles(string path, FileService versionService)
    {
        DirectoryInfo info = new(path);

        foreach (FileInfo file in info.GetFiles())
        {
            if(versionService.trackedFiles.Find(f => f.File.FullName == file.FullName) == null)
            {
                versionService.trackedFiles.Add(new TrackedFile { File = file, FileType = FileTypeEnum.untracked });
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

    public bool isProjectInitialized()
    {
        string projectPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos");
        return Directory.Exists(projectPath);
    }
}