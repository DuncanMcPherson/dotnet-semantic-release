using SemanticRelease.Abstractions;

namespace SemanticRelease.Core;

public static class ConfigExtensions
{
    public static ReleaseConfig ToReleaseConfig(this RawConfig config)
    {
        var plugins = new Dictionary<string, object>();
        if (config.PluginConfigs == null)
            return new ReleaseConfig(config.TagFormat ?? "v{version}", plugins, config.Branches?.ToList() ?? ["main"]);
        foreach (var data in config.PluginConfigs)
        {
            if (data.IsString)
            {
                plugins[data.PluginName!] = null!;
            } else if (data.IsObject)
            {
                plugins[data.Metadata!.Name] = data.Metadata.Options;
            }
        }

        return new ReleaseConfig(config.TagFormat ?? "v{version}", plugins, config.Branches?.ToList() ?? ["main"]);
    }
    
    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);
}