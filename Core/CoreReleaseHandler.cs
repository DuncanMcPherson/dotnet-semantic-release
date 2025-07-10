using SemanticRelease.Abstractions;

namespace SemanticRelease.Core;

public class CoreReleaseHandler
{
    private readonly RawConfig _config;
    private readonly string _workingDirectory;
    private ReleaseContext? _context;

    public CoreReleaseHandler(RawConfig config, string workingDirectory)
    {
        _config = config;
        _workingDirectory = workingDirectory;
    }

    // TODO: handle case of isDry
    public async Task RunRelease(bool isCi, bool isDry)
    {
        var lifecycle = new SemanticLifecycle();
        var nugetFetcher = new NuGetFetcher();
        var pluginLoader = new PluginLoader(nugetFetcher);
        var corePlugin = new CorePlugin();
        corePlugin.Register(lifecycle);
        try
        {
            Console.WriteLine("[semantic-release] Starting release process");
            if (_config.PluginConfigs != null)
            {
                foreach (var loaded in _config.PluginConfigs.Select(plugin => pluginLoader.LoadPlugin(plugin)))
                {
                    loaded.Register(lifecycle);
                }
            }

            _context = new ReleaseContext(_workingDirectory, _config.ToReleaseConfig());
            Console.WriteLine("[semantic-release] Plugins Loaded successfully");
            Console.WriteLine("[semantic-release] Begin step 'verifyConditions'");

            if (!await VerifyGitRepo()) return;
            if (!await VerifyBranch()) return;
            if (!await VerifyCommitHistory()) return;
            await lifecycle.RunStep(LifecycleSteps.VerifyConditions, _context);
            Console.WriteLine("[semantic-release] End step 'verifyConditions'");

            Console.WriteLine("[semantic-release] Begin step 'analyzeCommits'");
            await lifecycle.RunStep(LifecycleSteps.AnalyzeCommits, _context);
            Console.WriteLine("[semantic-release] End step 'analyzeCommits'");

            Console.WriteLine("[semantic-release] Begin step 'generateNotes'");
            await lifecycle.RunStep(LifecycleSteps.GenerateNotes, _context);
            Console.WriteLine("[semantic-release] End step 'generateNotes'");

            Console.WriteLine("[semantic-release] Begin step 'prepare'");
            await lifecycle.RunStep(LifecycleSteps.Prepare, _context);
            Console.WriteLine("[semantic-release] End step 'prepare'");

            Console.WriteLine("[semantic-release] Begin step 'publish'");
            await lifecycle.RunStep(LifecycleSteps.Publish, _context);
            Console.WriteLine("[semantic-release] End step 'publish'");

            Console.WriteLine("[semantic-release] Begin step 'success'");
            await lifecycle.RunStep(LifecycleSteps.Success, _context);
            Console.WriteLine("[semantic-release] End step 'success'");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[semantic-release] Failed to run release: {e.Message}");
            await lifecycle.RunStep(LifecycleSteps.Fail, _context ?? new ReleaseContext(_workingDirectory, _config.ToReleaseConfig()));
        }
    }

    private async Task<bool> VerifyGitRepo()
    {
        var result = await GitCommand("rev-parse --is-inside-work-tree");
        if (result.Success && result.Output.Trim() == "true") return true;
        await Console.Error.WriteLineAsync("[semantic-release] Not a git repository");
        return false;
    }

    private async Task<bool> VerifyBranch()
    {
        var result = await GitCommand("rev-parse --abbrev-ref HEAD");
        if (!result.Success)
        {
            await Console.Error.WriteLineAsync("[semantic-release] Failed to determine current branch");
            return false;
        }

        var branch = result.Output.Trim();
        if (_config.Branches!.Contains(branch)) return true;
        await Console.Error.WriteLineAsync(
            $"[semantic-release] Branch '{branch}' is not a configured release branch");
        return false;
    }

    private async Task<bool> VerifyCommitHistory()
    {
        var result = await GitCommand("rev-list --count HEAD");
        if (result.Success && int.TryParse(result.Output.Trim(), out var count) && count != 0) return true;
        await Console.Error.WriteLineAsync("[semantic-release] No commits found in current branch");
        return false;
    }

    private async Task<(bool Success, string Output)> GitCommand(string command)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? (true, output) : (false, error);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }
}