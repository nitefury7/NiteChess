using System.Runtime.InteropServices;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Stockfish;

public static class StockfishRuntimePathResolver
{
    public static string Resolve(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        ArgumentNullException.ThrowIfNull(runtimeDescriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeDescriptor.RuntimeLocation);

        var location = runtimeDescriptor.RuntimeLocation;

        if (IsBundleManifestPath(location))
        {
            location = ResolveManifestEntryPoint(runtimeDescriptor, location);
        }

        return ResolvePath(location);
    }

    private static string ResolveManifestEntryPoint(StockfishRuntimeDescriptor runtimeDescriptor, string manifestLocation)
    {
        var manifestPath = ResolvePath(manifestLocation);

        if (!File.Exists(manifestPath))
        {
            throw new InvalidOperationException(
                $"Stockfish bundle manifest '{manifestLocation}' could not be found for host '{runtimeDescriptor.HostId}'.");
        }

        var manifest = StockfishBundleManifest.Parse(File.ReadAllText(manifestPath));

        if (runtimeDescriptor.IntegrationMode == StockfishIntegrationMode.NativeProcess)
        {
            return ExpandTokens(
                manifest.EntryPointPattern
                ?? throw new InvalidOperationException(
                    $"Stockfish bundle manifest '{manifestLocation}' does not declare an entryPointPattern for native-process execution."));
        }

        throw new InvalidOperationException(
            $"Stockfish bundle manifest resolution is not supported for integration mode '{runtimeDescriptor.IntegrationMode}' in '{manifestLocation}'.");
    }

    private static string ResolvePath(string location)
    {
        location = ExpandTokens(location);

        if (Path.IsPathRooted(location))
        {
            return location;
        }

        var baseDirectoryCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, location));
        if (File.Exists(baseDirectoryCandidate))
        {
            return baseDirectoryCandidate;
        }

        var workingDirectoryCandidate = Path.GetFullPath(location, Environment.CurrentDirectory);
        return File.Exists(workingDirectoryCandidate)
            ? workingDirectoryCandidate
            : baseDirectoryCandidate;
    }

    private static string ExpandTokens(string location)
    {
        if (location.Contains("{rid}", StringComparison.Ordinal))
        {
            location = location.Replace("{rid}", GetDesktopRuntimeIdentifier(), StringComparison.Ordinal);
        }

        if (location.Contains("{platform}", StringComparison.Ordinal))
        {
            location = location.Replace("{platform}", GetMobilePlatformIdentifier(), StringComparison.Ordinal);
        }

        return location;
    }

    private static bool IsBundleManifestPath(string location)
    {
        return location.EndsWith(".bundle.json", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDesktopRuntimeIdentifier()
    {
        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.X86 => "x86",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
        };

        if (OperatingSystem.IsMacOS())
        {
            return $"osx-{architecture}";
        }

        if (OperatingSystem.IsLinux())
        {
            return $"linux-{architecture}";
        }

        if (OperatingSystem.IsWindows())
        {
            return $"win-{architecture}";
        }

        throw new PlatformNotSupportedException("The current OS does not have a known Stockfish desktop runtime identifier mapping.");
    }

    private static string GetMobilePlatformIdentifier()
    {
        if (OperatingSystem.IsAndroid())
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.X64
                ? "android-x86_64"
                : "android-arm64-v8a";
        }

        if (OperatingSystem.IsIOS())
        {
            return "ios-arm64";
        }

        return RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? "android-x86_64"
            : "android-arm64-v8a";
    }
}