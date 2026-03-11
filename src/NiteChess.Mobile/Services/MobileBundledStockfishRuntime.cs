using System.Runtime.InteropServices;
using Microsoft.Maui.Storage;
using NiteChess.Stockfish;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Mobile.Services;

internal static class MobileBundledStockfishRuntime
{
    private static readonly SemaphoreSlim ExtractionGate = new(1, 1);
    private static string? _androidExecutablePath;

    public static async ValueTask WarmUpAsync(
        StockfishRuntimeDescriptor runtimeDescriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (OperatingSystem.IsAndroid())
        {
            _ = await ResolveAndroidExecutablePathAsync(runtimeDescriptor, cancellationToken);
            return;
        }

        if (OperatingSystem.IsIOS())
        {
            _ = await LoadManifestAsync(runtimeDescriptor, cancellationToken);
        }
    }

    public static async ValueTask<string> ResolveAndroidExecutablePathAsync(
        StockfishRuntimeDescriptor runtimeDescriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsAndroid())
        {
            throw new PlatformNotSupportedException("Android Stockfish process extraction is only supported on Android targets.");
        }

        if (!string.IsNullOrWhiteSpace(_androidExecutablePath) && File.Exists(_androidExecutablePath))
        {
            return _androidExecutablePath;
        }

        var manifest = await LoadManifestAsync(runtimeDescriptor, cancellationToken);
        var platformIdentifier = GetMobilePlatformIdentifier();

        if (manifest.PlatformEntries is null ||
            !manifest.PlatformEntries.TryGetValue(platformIdentifier, out var assetPath) ||
            string.IsNullOrWhiteSpace(assetPath))
        {
            throw new PlatformNotSupportedException(
                $"The bundled mobile Stockfish manifest '{runtimeDescriptor.RuntimeLocation}' does not declare an Android runtime for '{platformIdentifier}'.");
        }

        var packageAssetPath = NormalizePackageAssetPath(assetPath);
        var targetDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "stockfish", platformIdentifier);
        var targetPath = Path.Combine(targetDirectory, Path.GetFileName(packageAssetPath));

        await ExtractionGate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(targetPath) || new FileInfo(targetPath).Length == 0)
            {
                Directory.CreateDirectory(targetDirectory);

                await using var source = await FileSystem.OpenAppPackageFileAsync(packageAssetPath);
                await using var destination = File.Create(targetPath);
                await source.CopyToAsync(destination, cancellationToken);
            }

            File.SetUnixFileMode(
                targetPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

            _androidExecutablePath = targetPath;
            return targetPath;
        }
        finally
        {
            ExtractionGate.Release();
        }
    }

    private static async Task<StockfishBundleManifest> LoadManifestAsync(
        StockfishRuntimeDescriptor runtimeDescriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptor);

        var manifestAssetPath = NormalizePackageAssetPath(runtimeDescriptor.RuntimeLocation);
        await using var stream = await FileSystem.OpenAppPackageFileAsync(manifestAssetPath);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(cancellationToken);
        return StockfishBundleManifest.Parse(json);
    }

    private static string NormalizePackageAssetPath(string assetPath)
    {
        return assetPath.StartsWith("Resources/Raw/", StringComparison.OrdinalIgnoreCase)
            ? assetPath["Resources/Raw/".Length..]
            : assetPath;
    }

    private static string GetMobilePlatformIdentifier()
    {
        if (OperatingSystem.IsIOS())
        {
            return "ios-arm64";
        }

        if (!OperatingSystem.IsAndroid())
        {
            throw new PlatformNotSupportedException("Bundled mobile Stockfish runtimes are only supported on Android and iOS targets.");
        }

        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "android-arm64-v8a",
            Architecture.X64 => "android-x86_64",
            _ => throw new PlatformNotSupportedException(
                $"Android Stockfish is only bundled for arm64-v8a devices; current architecture is '{RuntimeInformation.ProcessArchitecture}'.")
        };
    }
}