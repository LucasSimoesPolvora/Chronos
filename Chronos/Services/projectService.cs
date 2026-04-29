public class ProjectService
{
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
    }
}