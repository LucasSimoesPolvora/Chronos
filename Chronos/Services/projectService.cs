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
        File.Create(Path.Combine(projectPath, "HEAD")).Close();
        File.Create(Path.Combine(projectPath, "status")).Close();
        File.WriteAllText(Path.Combine(projectPath, "status"), HeadStatus.attached.ToString());
        Console.WriteLine("Project initialized successfully.");   
    }

    public static bool IsProjectInitialized()
    {
        string projectPath = Path.Combine(Directory.GetCurrentDirectory(), ".chronos");
        return Directory.Exists(projectPath);
    }

    public static void DeleteAllFilesInDirectory(string directoryPath)
    {
        foreach (string file in Directory.GetFiles(directoryPath))
        {
            File.Delete(file);
        }

        foreach (string dir in Directory.GetDirectories(directoryPath))
        {
            if(dir.Contains(".chronos"))
            {
                continue;
            }
            DeleteAllFilesInDirectory(dir);
            Directory.Delete(dir);
        }
    }
}