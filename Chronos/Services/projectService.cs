public class ProjectService
{
    public void initProject()
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
        Console.WriteLine("Project initialized successfully.");

    }
}