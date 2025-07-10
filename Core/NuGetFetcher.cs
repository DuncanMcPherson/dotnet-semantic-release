using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;

namespace SemanticRelease.Core;

public class NuGetFetcher : INugetFetcher
{
    private readonly string _pluginCachePath = Path.Combine(AppContext.BaseDirectory, "plugins");

    public async Task<string?> FetchAndExtract(string packageName, string? version = null)
    {
        var providers = new List<Lazy<INuGetResourceProvider>>();
        providers.AddRange(Repository.Provider.GetCoreV3());

        var settings = Settings.LoadDefaultSettings(root: null);
        var packageSourceProvider = new PackageSourceProvider(settings);
        var sources = packageSourceProvider.LoadPackageSources().Where(i => i.IsEnabled);
        using var cacheContext = new SourceCacheContext();

        foreach (var source in sources)
        {
            var repo = new SourceRepository(source, providers);
            var resource = await repo.GetResourceAsync<FindPackageByIdResource>();

            var versions =
                (await resource.GetAllVersionsAsync(packageName, cacheContext, new NullLogger(),
                    CancellationToken.None)).ToList();
            if (versions.Count == 0) continue;

            var resolvedVersion = version != null
                ? versions.FirstOrDefault(v => v.ToNormalizedString() == version)
                : versions.Max();

            if (resolvedVersion == null) continue;

            var packagePath = Path.Combine(Path.GetTempPath(), $"{packageName}.{resolvedVersion}.nupkg");

            await using var packageStream = File.Create(packagePath);
            var success = await resource.CopyNupkgToStreamAsync(
                packageName,
                resolvedVersion,
                packageStream,
                cacheContext,
                new NullLogger(),
                CancellationToken.None);
            if (!success) continue;
            return await ExtractPluginDll(packageName, resolvedVersion.ToNormalizedString(), packageStream);
        }

        return null;
    }

    private async Task<string?> ExtractPluginDll(string packageName, string version, Stream packageStream)
    {
        var targetDir = Path.Combine(_pluginCachePath, packageName.ToLower(), version);
        Directory.CreateDirectory(targetDir);

        using var reader = new PackageArchiveReader(packageStream);
        reader.CopyFiles(
            targetDir,
            await reader.GetPackageFilesAsync(PackageSaveMode.Files, CancellationToken.None),
            (sourcePath, destinationPath, stream) =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                using var destinationStream = File.Create(destinationPath);
                stream.CopyTo(destinationStream);
                return destinationPath;
            },
            NullLogger.Instance,
            CancellationToken.None);
        var dllPath = Directory.GetFiles(targetDir, "*.dll", SearchOption.AllDirectories)
            .FirstOrDefault(path =>
                Path.GetFileNameWithoutExtension(path).Equals(packageName, StringComparison.OrdinalIgnoreCase));
        return dllPath;
    }
}