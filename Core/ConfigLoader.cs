using System.Text.Json;
using SemanticRelease.Abstractions;

namespace SemanticRelease.Core;

public class ConfigLoader
{
    public async Task<RawConfig?> Load(string workingDir, string? configPath)
    {
        RawConfig config;
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
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new PluginConfigConverter());
                config = JsonSerializer.Deserialize<RawConfig>(json, options) ??
                         throw new InvalidOperationException("Failed to deserialize config");
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync($"Failed to load config file: {e.Message}");
                return null;
            }
        }
        else
        {
            config = new RawConfig
            {
                TagFormat = "v{version}",
                PluginConfigs = [],
                Branches = ["main"]
            };
            await Console.Out.WriteLineAsync("No config file found, using default config");
        }

        return config;
    }
}