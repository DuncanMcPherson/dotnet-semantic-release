namespace SemanticRelease.Core;

public class RawConfig
{
    public string[]? Branches { get; set; }
    public string? TagFormat { get; set; }
    public List<RawPluginData>? PluginConfigs { get; set; }
}