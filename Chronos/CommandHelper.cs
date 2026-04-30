public static class CommandHelper
{
    public static string GetDescription()
    {
        return "Chronos - Project Versioning Tool";
    }

    public static string GetCommandDescription(string commandName)
    {
        return commandName switch
        {
            "init" => "Initializes a new Chronos project in the current directory.",
            "add" => "Adds files to the staging area. Usage: add [file-pattern]",
            "commit" => "Commits staged changes to the repository with a message. Usage: commit -m \"Your commit message\"",
            "log" => "Displays the commit history of the project.",
            "status" => "Shows the status of tracked files and staging area.",
            "checkout" => "Restores files from a specific snapshot. Usage: checkout [snapshot-id]",
            "--help, -h" => "Displays help information about available commands and options.",
            _ => "Unknown command"
        };
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
    --help, -h  {GetCommandDescription("--help, -h")}
        ";
    }
}
