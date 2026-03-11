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

        if (location.Contains("{rid}", StringComparison.Ordinal))
        {
            location = location.Replace("{rid}", GetDesktopRuntimeIdentifier(), StringComparison.Ordinal);
        }

        if (location.Contains("{platform}", StringComparison.Ordinal))
        {
            location = location.Replace("{platform}", GetMobilePlatformIdentifier(), StringComparison.Ordinal);
        }

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