namespace SemanticRelease.Core;

public class RawPluginData
{
    public string? PluginName { get; set; }
    public PluginMetadata? Metadata { get; set; }
    
    public bool IsString => PluginName != null;
    public bool IsObject => Metadata != null;
}