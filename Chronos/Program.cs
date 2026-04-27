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
    case "--init":
        ps.initProject();
        break;
    default:
        Console.WriteLine($"Unknown command: {command}");
        return 1;
}

return 0;
