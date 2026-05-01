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

    public bool isProjectInitialized()
    {
        string projectPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos");
        return Directory.Exists(projectPath);
    }
}