FileService fs = new();
VersionService vs = new();
if (args.Length == 0)
{
    Console.WriteLine("No command provided");
    return 0;
}
string command = args[0];

if(command != "-i" && command != "init")
{
    ProjectService projectService = new();
    if (!projectService.isProjectInitialized())
    {
        Console.WriteLine("No project found. Please initialize a project first using 'chronos init'.");
        return 1;
    }
}

switch (command)
{
    case "-i":
    case "init":
        ProjectService.InitProject();
        break;
    case "-a":
    case "add":
        fs.AddToStaging(args.Length > 1 ? args[1] : ".");
        break;
    case "-c":
    case "commit":
        vs.CommitVersion(args.Length > 1 ? args[1] : "No commit message");
        break;
    case "-l":
    case "log":
        Console.WriteLine("Log command not implemented yet.");
        break;
    case "-s":
    case "status":
        vs.GetVersionState(fs);
        VersionService.DisplayVersionState(fs);
        break;
    case "checkout":
        Console.WriteLine("Checkout command not implemented yet.");
        break;
    case "-h":
    case "--help":
        Console.WriteLine(CommandHelper.GenerateFullDocumentation());
        break;
    default:
        Console.WriteLine($"Unknown command: {command}");
        return 1;
}

return 0;
