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
int argsLength = args.Length;

// Handle help command
if (command == "help" || command == "-h" || command == "--help")
{
    if (argsLength > 1 && CommandHelper.IsValidCommand(args[1]))
    {
        Console.WriteLine(CommandHelper.GetCommandHelp(args[1]));
    }
    else
    {
        Console.WriteLine(CommandHelper.GenerateFullDocumentation());
    }
    return 0;
}

if(command != "-i" && command != "init")
{
    ProjectService projectService = new();
    if (!ProjectService.IsProjectInitialized())
    {
        Console.WriteLine("No project found. Please initialize a project first using 'chronos init'.");
        return 1;
    }
}

switch (command)
{
    case "-i":
    case "init":
        if (argsLength > 1 && (args[1] == "--help" || args[1] == "-h"))
        {
            Console.WriteLine(CommandHelper.GetCommandHelp("init"));
        }
        else
        {
            ProjectService.InitProject();
        }
        break;
    case "-a":
    case "add":
        if (argsLength > 1 && (args[1] == "--help" || args[1] == "-h"))
        {
            Console.WriteLine(CommandHelper.GetCommandHelp("add"));
        }
        else if(isDetached)
        {
            Console.WriteLine("Cannot add files. HEAD is currently detached. Please attach HEAD to the last committed version before adding files.");
            return 1;
        }
        else
        {
            fs.AddToStaging(argsLength > 1 ? args[1] : ".");
        }
        break;
    case "-c":
    case "commit":
        if (argsLength > 1 && (args[1] == "--help" || args[1] == "-h"))
        {
            Console.WriteLine(CommandHelper.GetCommandHelp("commit"));
        }
        else if(isDetached)
        {
            Console.WriteLine("Cannot commit. HEAD is currently detached. Please attach HEAD to the last committed version before committing.");
            return 1;
        }
        else
        {
            HandleCommit();
        }
        break;
    case "-l":
    case "log":
        if (argsLength > 1 && (args[1] == "--help" || args[1] == "-h"))
        {
            Console.WriteLine(CommandHelper.GetCommandHelp("log"));
        }
        else
        {
            VersionService.DisplayVersionHistory();
        }
        break;
    case "-s":
    case "status":
        if (argsLength > 1 && (args[1] == "--help" || args[1] == "-h"))
        {
            Console.WriteLine(CommandHelper.GetCommandHelp("status"));
        }
        else if(isDetached)
        {
            Console.WriteLine("Cannot check the status. HEAD is currently detached. Please attach HEAD to the last committed version before checking status.");
            return 1;
        }
        else
        {
            vs.GetVersionState(fs);
            VersionService.DisplayVersionState(fs);
        }
        break;
    case "checkout":
        if (argsLength > 1 && (args[1] == "--help" || args[1] == "-h"))
        {
            Console.WriteLine(CommandHelper.GetCommandHelp("checkout"));
        }
        else
        {
            HandleCheckout();
        }
        break;
    default:
        Console.WriteLine($"Unknown command: {command}");
        CommandHelper.GenerateFullDocumentation();
        return 1;
}

void HandleCommit()
{
    int index = -1;
    for (int i = 0; i < argsLength; i++)
    {
        if (args[i] == "-m")
        {
            index = i;
            break;
        }
    }
    
    if (index != -1 && index < argsLength - 1)
    {
        string message = args[index + 1];
        vs.CommitVersion(message);
    }
    else
    {
        Console.WriteLine("No commit message provided. Aborting...");
    }
}

void HandleCheckout()
{
    if (argsLength < 2)
    {
        Console.WriteLine("No version specified for checkout. Please provide a version hash or reference.");
        return;
    }
    vs.CheckoutVersion(args[1]);
}

return 0;
