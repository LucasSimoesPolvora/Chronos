public static class CommandHelper
{
    private static readonly Dictionary<string, (string description, string usage, string examples)> CommandHelpers = 
        new(StringComparer.OrdinalIgnoreCase)
        {
            {
                "init", 
                (
                    "Initializes a new Chronos project in the current directory.",
                    "chronos init",
                    "  chronos init"
                )
            },
            {
                "add", 
                (
                    "Adds files to the staging area.",
                    "chronos add [file-pattern]",
                    "  chronos add .\n  chronos add src/\n  chronos add *.txt"
                )
            },
            {
                "commit", 
                (
                    "Commits staged changes to the repository with a message.",
                    "chronos commit -m \"Your commit message\"",
                    "  chronos commit -m \"Initial commit\"\n  chronos commit -m \"Fix bug in parser\""
                )
            },
            {
                "log", 
                (
                    "Displays the commit history of the project.",
                    "chronos log",
                    "  chronos log"
                )
            },
            {
                "status", 
                (
                    "Shows the status of tracked files and staging area.",
                    "chronos status",
                    "  chronos status"
                )
            },
            {
                "checkout", 
                (
                    "Restores files from a specific snapshot.",
                    "chronos checkout <snapshot-id>",
                    "  chronos checkout abc123def\n"
                )
            }
        };

    public static string GetDescription()
    {
        return "Chronos - Project Versioning Tool";
    }

    public static string GetCommandDescription(string commandName)
    {
        return CommandHelpers.TryGetValue(commandName, out var helper) 
            ? helper.description 
            : "Unknown command";
    }

    public static string GetCommandHelp(string commandName)
    {
        if (CommandHelpers.TryGetValue(commandName, out var helper))
        {
            return $@"Command: {commandName}
Description: {helper.description}

Usage:
    {helper.usage}

Examples:
{helper.examples}";
        }
        return $"Unknown command: {commandName}";
    }

    public static bool IsValidCommand(string commandName)
    {
        return CommandHelpers.ContainsKey(commandName);
    }

    public static string GenerateFullDocumentation()
    {
        return $@"{GetDescription()}
Usage: chronos <command> [options]

Commands:
    init        {GetCommandDescription("init")}
    add         {GetCommandDescription("add")}
    commit      {GetCommandDescription("commit")}
    log         {GetCommandDescription("log")}
    status      {GetCommandDescription("status")}
    checkout    {GetCommandDescription("checkout")}

Help:
    chronos help [command]          Show help for a specific command
    chronos <command> --help        Show help for a specific command
    chronos --help, -h              Show this help message";
    }
}
