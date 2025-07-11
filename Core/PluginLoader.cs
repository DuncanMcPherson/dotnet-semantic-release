using System.Reflection;
using SemanticRelease.Abstractions;

namespace SemanticRelease.Core;

public class PluginLoader
{
    private readonly string[] _pluginSearchPaths;
    private readonly INugetFetcher _nuGetFetcher;

    public PluginLoader(INugetFetcher nugetFetcher)
    {
        _nuGetFetcher = nugetFetcher;
        _pluginSearchPaths =
        [
            Path.Combine(AppContext.BaseDirectory, "plugins"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")
        ];
    }

    public ISemanticPlugin LoadPlugin(RawPluginData pluginConfig)
    {
        var (pluginName, version) = ExtractPluginName(pluginConfig);
        var dllPath = LocatePluginAssembly(pluginName, version)
                      ?? _nuGetFetcher.FetchAndExtract(pluginName, version).GetAwaiter().GetResult()
                      ?? throw new FileNotFoundException($"Unable to locate plugin DLL for '{pluginName}'");
        return LoadSemanticPlugin(dllPath);
    }

    private (string, string?) ExtractPluginName(RawPluginData pluginConfig)
    {
        var plugin = pluginConfig.IsString ? pluginConfig.PluginName! : pluginConfig.Metadata!.Name;
        var version = plugin.Contains("@") ? plugin.Split('@')[1] : null;
        if (!version.IsNullOrEmpty() && !NuGet.Versioning.NuGetVersion.TryParse(version, out _))
        {
            throw new ArgumentException($"Invalid version specified for plugin '{plugin}'");
        }
        return (plugin.Split('@')[0], version);
    }

    private string? LocatePluginAssembly(string pluginName, string? version)
    {
        return (from basePath in _pluginSearchPaths
                let pluginDir = Path.Combine(basePath, pluginName.ToLower())
                let versionDir = version != null ? Path.Combine(pluginDir, version) : null
                let searchRoot = versionDir ?? pluginDir
                where Directory.Exists(searchRoot)
                let dllPath = Directory.GetFiles(searchRoot, $"{pluginName}.dll", SearchOption.AllDirectories)
                    .FirstOrDefault()
                where dllPath != null
                select dllPath).FirstOrDefault();
    }

    private ISemanticPlugin LoadSemanticPlugin(string dllPath)
    {
        var assembly = Assembly.LoadFrom(dllPath);
        var pluginType = assembly.GetTypes().FirstOrDefault(x => typeof(ISemanticPlugin).IsAssignableFrom(x) && !x.IsAbstract)
            ?? throw new InvalidOperationException($"No valid ISemanticPlugin found in '{dllPath}'");
        return (ISemanticPlugin)Activator.CreateInstance(pluginType)!
            ?? throw new InvalidOperationException($"Plugin instantiation failed for type '{pluginType.FullName}'");
    }
}