namespace SemanticRelease.Core;

public interface INugetFetcher
{
    Task<string?> FetchAndExtract(string packageName, string? version = null);
}