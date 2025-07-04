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
        var pluginName = ExtractPluginName(pluginConfig);
        var dllPath = LocatePluginAssembly(pluginName)
                      ?? _nuGetFetcher.FetchAndExtract(pluginName).GetAwaiter().GetResult()
                      ?? throw new FileNotFoundException($"Unable to locate plugin DLL for '{pluginName}'");
        return LoadSemanticPlugin(dllPath);
    }

    private string ExtractPluginName(RawPluginData pluginConfig)
    {
        return pluginConfig.IsString ? pluginConfig.PluginName! : pluginConfig.Metadata!.Name;
    }

    private string? LocatePluginAssembly(string pluginName)
    {
        return (from basePath in _pluginSearchPaths
                select Path.Combine(basePath, pluginName.ToLower())
                into packageDir
                where Directory.Exists(packageDir)
                select Directory.GetFiles(packageDir, "*.dll", SearchOption.AllDirectories)
                    .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(pluginName, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault();
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