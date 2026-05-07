FileService fs = new();
VersionService vs = new();
bool isDetached = false;
if(Path.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".chronos")))
{
    fs.GetFiles(Directory.GetCurrentDirectory(), fs);
    vs.GetVersionState(fs);
    string headStatus = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), ".chronos", "status"));
    isDetached = headStatus == HeadStatus.detached.ToString();
}

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
        if(isDetached)
            {
                Console.WriteLine("Cannot add files. HEAD is currently detached. Please attach HEAD to the last committed version before adding files.");
                return 1;
            }
        fs.AddToStaging(args.Length > 1 ? args[1] : ".");
        break;
    case "-c":
    case "commit":
        if(isDetached)
        {
            Console.WriteLine("Cannot commit. HEAD is currently detached. Please attach HEAD to the last committed version before committing.");
            return 1;
        }
        HandleCommit();
        break;
    case "-l":
    case "log":
        VersionService.DisplayVersionHistory();
        break;
    case "-s":
    case "status":
        if(isDetached)
        {
            Console.WriteLine("Cannot commit. HEAD is currently detached. Please attach HEAD to the last committed version before committing.");
            return 1;
        }
        vs.GetVersionState(fs);
        VersionService.DisplayVersionState(fs);
        break;
    case "checkout":
        HandleCheckout();
        break;
    case "-h":
    case "--help":
        Console.WriteLine(CommandHelper.GenerateFullDocumentation());
        break;
    default:
        Console.WriteLine($"Unknown command: {command}");
        return 1;
}

void HandleCommit()
{
    int index = args.ToList().FindIndex(arg => arg == "-m");
    if (index != -1 && index < args.Length - 1)
    {
        string message = args[index + 1];
        vs.CommitVersion(message);
    }
    else
    {
        Console.WriteLine("No commit message provided. Using default message.");
        vs.CommitVersion("No commit message");
    }
}

void HandleCheckout()
{
    if (args.Length < 2)
    {
        Console.WriteLine("No version specified for checkout. Please provide a version hash or reference.");
        return;
    }
    vs.CheckoutVersion(args[1]);
}

return 0;
