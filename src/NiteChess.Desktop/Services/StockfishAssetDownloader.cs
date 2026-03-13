using System.Formats.Tar;
using System.Runtime.InteropServices;
using NiteChess.Stockfish;

namespace NiteChess.Desktop.Services;

/// <summary>
/// Downloads the platform-appropriate Stockfish binary from GitHub Releases
/// at runtime when the asset is absent from the output directory.
/// </summary>
public sealed class StockfishAssetDownloader
{
    private static readonly SemaphoreSlim DownloadGate = new(1, 1);

    private readonly HttpClient _httpClient;

    public StockfishAssetDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Ensures the Stockfish binary for the current RID is present at
    /// <paramref name="targetPath"/>. Downloads and extracts from the bundle
    /// manifest's <c>ridDownloadAssets</c> map when absent.
    /// </summary>
    public async ValueTask EnsureAsync(
        string manifestPath,
        string targetPath,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(targetPath))
        {
            return;
        }

        await DownloadGate.WaitAsync(cancellationToken);
        try
        {
            // Re-check inside the lock in case another thread already downloaded.
            if (File.Exists(targetPath))
            {
                return;
            }

            var asset = ResolveDownloadAsset(manifestPath);
            await DownloadAndExtractAsync(asset, targetPath, cancellationToken);
            MakeExecutable(targetPath);
        }
        finally
        {
            DownloadGate.Release();
        }
    }

    private static StockfishRidDownloadAsset ResolveDownloadAsset(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(
                $"Stockfish bundle manifest not found at '{manifestPath}'.", manifestPath);
        }

        var manifest = StockfishBundleManifest.Parse(File.ReadAllText(manifestPath));

        if (manifest.RidDownloadAssets is null || manifest.RidDownloadAssets.Count == 0)
        {
            throw new InvalidOperationException(
                $"Stockfish bundle manifest '{manifestPath}' does not declare any 'ridDownloadAssets'. " +
                "Add per-RID download entries to enable automatic asset provisioning.");
        }

        var rid = GetCurrentRid();

        if (!manifest.RidDownloadAssets.TryGetValue(rid, out var asset))
        {
            throw new PlatformNotSupportedException(
                $"No Stockfish download asset is declared for RID '{rid}' in manifest '{manifestPath}'. " +
                $"Available RIDs: {string.Join(", ", manifest.RidDownloadAssets.Keys)}");
        }

        if (string.IsNullOrWhiteSpace(asset.ArchiveUrl))
        {
            throw new InvalidOperationException(
                $"The download asset for RID '{rid}' in manifest '{manifestPath}' has an empty archiveUrl.");
        }

        return asset;
    }

    private async Task DownloadAndExtractAsync(
        StockfishRidDownloadAsset asset,
        string targetPath,
        CancellationToken cancellationToken)
    {
        var targetDirectory = Path.GetDirectoryName(targetPath)
            ?? throw new InvalidOperationException($"Cannot determine parent directory of '{targetPath}'.");

        Directory.CreateDirectory(targetDirectory);

        Console.WriteLine($"[NiteChess] Stockfish binary not found. Downloading from {asset.ArchiveUrl} ...");

        using var response = await _httpClient.GetAsync(
            asset.ArchiveUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var archiveStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await ExtractTarEntryAsync(archiveStream, asset.ArchiveEntryName, targetPath, cancellationToken);

        Console.WriteLine($"[NiteChess] Stockfish binary extracted to '{targetPath}'.");
    }

    private static async Task ExtractTarEntryAsync(
        Stream archiveStream,
        string entryName,
        string targetPath,
        CancellationToken cancellationToken)
    {
        // The archive is a plain .tar (not gzip-compressed) from Stockfish releases.
        using var tarReader = new TarReader(archiveStream, leaveOpen: false);

        while (await tarReader.GetNextEntryAsync(copyData: false, cancellationToken) is { } entry)
        {
            // Match on the declared entry name or just the filename as a fallback.
            var entryNameMatch =
                string.Equals(entry.Name, entryName, StringComparison.Ordinal) ||
                string.Equals(entry.Name, entryName + "/", StringComparison.Ordinal) ||
                string.Equals(Path.GetFileName(entry.Name.TrimEnd('/')),
                               Path.GetFileName(entryName),
                               StringComparison.OrdinalIgnoreCase);

            if (!entryNameMatch || entry.EntryType is TarEntryType.Directory)
            {
                continue;
            }

            var tmpPath = targetPath + ".tmp";
            try
            {
                await using (var dest = new FileStream(
                    tmpPath, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: 81920, useAsync: true))
                {
                    await entry.DataStream!.CopyToAsync(dest, cancellationToken);
                }

                File.Move(tmpPath, targetPath, overwrite: true);
            }
            catch
            {
                if (File.Exists(tmpPath))
                {
                    File.Delete(tmpPath);
                }

                throw;
            }

            return;
        }

        throw new InvalidOperationException(
            $"Entry '{entryName}' was not found in the Stockfish archive. " +
            "Check that 'archiveEntryName' in the bundle manifest matches the actual archive layout.");
    }

    private static void MakeExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Set Unix execute bits (chmod +x) via P/Invoke.
        const int executableMode = 0b_111_101_101; // rwxr-xr-x (0755)
        var result = chmod(path, executableMode);
        if (result != 0)
        {
            throw new InvalidOperationException(
                $"Failed to set executable permission on '{path}' (chmod returned {result}).");
        }
    }

    [System.Runtime.InteropServices.DllImport("libc", SetLastError = true)]
    private static extern int chmod(string path, int mode);

    private static string GetCurrentRid()
    {
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64  => "x64",
            Architecture.Arm  => "arm",
            Architecture.X86  => "x86",
            _                  => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
        };

        if (OperatingSystem.IsMacOS())  return $"osx-{arch}";
        if (OperatingSystem.IsLinux())  return $"linux-{arch}";
        if (OperatingSystem.IsWindows()) return $"win-{arch}";

        throw new PlatformNotSupportedException(
            "Cannot determine the runtime identifier for the current OS.");
    }
}
