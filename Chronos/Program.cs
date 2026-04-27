ProjectService ps = new();

if (args.Length == 0)
{
    Console.WriteLine("No command provided");
    return 0;
}
string command = args[0];

switch (command)
{
    case "-i":
    case "init":
        ps.initProject();
        break;
    case "-a":
    case "add":
        Console.WriteLine("Add command not implemented yet.");
        break;
    case "-c":
    case "commit":
        Console.WriteLine("Commit command not implemented yet.");
        break;
    case "-l":
    case "log":
        Console.WriteLine("Log command not implemented yet.");
        break;
    case "-s":
    case "status":
        Console.WriteLine("Status command not implemented yet.");
        break;
    case "checkout":
        Console.WriteLine("Checkout command not implemented yet.");
        break;
    case "-h":
    case "--help":
        Console.WriteLine("Helper command not implemented yet.");
        break;
    default:
        Console.WriteLine($"Unknown command: {command}");
        return 1;
}

return 0;
