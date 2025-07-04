using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using SemanticRelease.Abstractions;
using SemanticRelease.Core;

namespace SemanticRelease;

// TODO: Implement optional flags of branch, force-version, no-verify, and verbose
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Run without making any changes",
            Required = false
        };
        var ciOption = new Option<bool>("--ci")
        {
            Required = false,
            Description = "Run in CI mode"
        };
        var configPathOption = new Option<string>("--config-path")
        {
            Aliases = { "-c" },
            Required = false,
            Description = "Path to the configuration file"
        };
        var workingDirOption = new Option<string>("--working-dir")
        {
            Aliases = { "-w" },
            Required = false,
            DefaultValueFactory = (_) => Directory.GetCurrentDirectory(),
            Description = "Path to the working directory"
        };
        
        var rootCommand = new RootCommand("Semantic Release")
        {
            dryRunOption,
            ciOption,
            configPathOption,
            workingDirOption
        };

        rootCommand.SetAction(async parse =>
        {
            var isCi = parse.GetValue(ciOption);
            var isDry = parse.GetValue(dryRunOption);
            var configPath = parse.GetValue(configPathOption);
            var workingDir = parse.GetValue(workingDirOption)!;
            
            var configLoader = new ConfigLoader();
            var config = await configLoader.Load(workingDir, configPath);
            if (config == null)
            {
                await Console.Error.WriteLineAsync("Failed to load config");
                Environment.Exit(1);
                return;
            }
            var context = new ReleaseContext(workingDir, config);
            var releaseHandler = new CoreReleaseHandler(context);
            await releaseHandler.RunRelease(isCi, isDry);
        });

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }
}
