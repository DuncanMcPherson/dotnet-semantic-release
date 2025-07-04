using System.Text.Json;
using SemanticRelease.Abstractions;

namespace SemanticRelease.Core;

public class ConfigLoader
{
    public async Task<ReleaseConfig?> Load(string workingDir, string? configPath)
    {
        ReleaseConfig config;
        string? resolvedConfigPath = null;

        if (!string.IsNullOrEmpty(configPath))
        {
            resolvedConfigPath = Path.Combine(workingDir, configPath);
            if (!File.Exists(resolvedConfigPath))
            {
                await Console.Error.WriteLineAsync(
                    $"Config file path specified but file does not exist: {resolvedConfigPath}");
                return null;
            }
        }
        else
        {
            var defaultPath = Path.Combine(workingDir, "semantic-release.json");
            if (File.Exists(defaultPath))
            {
                resolvedConfigPath = defaultPath;
            }
        }

        if (resolvedConfigPath != null)
        {
            try
            {
                var json = await File.ReadAllTextAsync(resolvedConfigPath);
                config = JsonSerializer.Deserialize<ReleaseConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize config");
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync($"Failed to load config file: {e.Message}");
                return null;
            }
        }
        else
        {
            config = new ReleaseConfig("v{version}", null, ["main"]);
            await Console.Out.WriteLineAsync("No config file found, using default config");
        }

        return config;
    }
}