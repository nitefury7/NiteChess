using System.Text.Json;

namespace NiteChess.Stockfish;

public sealed class StockfishBundleManifest
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string HostId { get; init; } = string.Empty;

    public string IntegrationMode { get; init; } = string.Empty;

    public string BundleRoot { get; init; } = string.Empty;

    public string? EntryPointPattern { get; init; }

    public Dictionary<string, string>? PlatformEntries { get; init; }

    public string? WorkerEntryPoint { get; init; }

    public string? EngineScript { get; init; }

    public string? EngineWasm { get; init; }

    public string[] RequiredAssets { get; init; } = Array.Empty<string>();

    public string? Notes { get; init; }

    public string? StockfishVersion { get; init; }

    public Dictionary<string, StockfishRidDownloadAsset>? RidDownloadAssets { get; init; }

    public static StockfishBundleManifest Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<StockfishBundleManifest>(json, SerializerOptions)
               ?? throw new InvalidOperationException("Failed to deserialize the Stockfish bundle manifest.");
    }
}

public sealed class StockfishRidDownloadAsset
{
    public string ArchiveUrl { get; init; } = string.Empty;

    public string ArchiveEntryName { get; init; } = string.Empty;
}
