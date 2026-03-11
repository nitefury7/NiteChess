using System.ComponentModel;
using System.Diagnostics;
using NiteChess.Stockfish.Abstractions;

namespace NiteChess.Stockfish;

public sealed class NativeProcessStockfishEngineClient : IStockfishEngineClient
{
    private readonly StockfishRuntimeDescriptor _runtimeDescriptor;

    public NativeProcessStockfishEngineClient(StockfishRuntimeDescriptor runtimeDescriptor)
    {
        _runtimeDescriptor = runtimeDescriptor ?? throw new ArgumentNullException(nameof(runtimeDescriptor));
    }

    public async ValueTask<StockfishEngineResponse> GetBestMoveAsync(
        StockfishEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var commands = StockfishUciCommandBuilder.Build(request).ToArray();
        var processPath = StockfishRuntimePathResolver.Resolve(_runtimeDescriptor);

        if (!File.Exists(processPath))
        {
            throw new InvalidOperationException(
                $"Stockfish native process runtime was not found at '{processPath}'. " +
                $"Provide a local engine binary for host '{_runtimeDescriptor.HostId}' or update the runtime descriptor path.");
        }

        using var process = StartProcess(processPath);

        try
        {
            var stderrTask = process.StandardError.ReadToEndAsync();
            var bestMoveTask = ReadBestMoveAsync(process.StandardOutput, commands);

            await WriteCommandsAsync(process.StandardInput, commands);

            var response = await bestMoveTask.WaitAsync(cancellationToken);

            if (!process.HasExited)
            {
                await process.StandardInput.WriteLineAsync("quit");
                await process.StandardInput.FlushAsync();
                await process.WaitForExitAsync(cancellationToken);
            }

            var stderr = await stderrTask;
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                response = response with { Commands = response.Commands.Concat(new[] { $"stderr: {stderr.Trim()}" }).ToArray() };
            }

            return response;
        }
        catch (Exception)
        {
            TryTerminate(process);
            throw;
        }
    }

    private static Process StartProcess(string processPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(processPath) ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (OperatingSystem.IsWindows() && IsBatchScript(processPath))
            {
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/c \"{processPath}\"";
            }
            else
            {
                startInfo.FileName = processPath;
            }

            return Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start Stockfish process '{processPath}'.");
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"Unable to start Stockfish process '{processPath}'.", exception);
        }
    }

    private static async Task WriteCommandsAsync(StreamWriter writer, IReadOnlyList<string> commands)
    {
        foreach (var command in commands)
        {
            await writer.WriteLineAsync(command);
        }

        await writer.FlushAsync();
    }

    private static async Task<StockfishEngineResponse> ReadBestMoveAsync(StreamReader reader, IReadOnlyList<string> commands)
    {
        var transcript = new List<string>();

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                throw new InvalidOperationException(
                    $"Stockfish process exited before returning a bestmove. Transcript: {string.Join(" | ", transcript)}");
            }

            transcript.Add(line);

            if (!line.StartsWith("bestmove ", StringComparison.Ordinal))
            {
                continue;
            }

            var (bestMoveNotation, ponderMoveNotation) = ParseBestMove(line);
            return new StockfishEngineResponse(bestMoveNotation, ponderMoveNotation, commands.Concat(transcript).ToArray());
        }
    }

    private static (string BestMoveNotation, string? PonderMoveNotation) ParseBestMove(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            throw new InvalidOperationException($"Stockfish emitted malformed bestmove output '{line}'.");
        }

        var ponderMoveNotation = parts.Length >= 4 && string.Equals(parts[2], "ponder", StringComparison.Ordinal)
            ? parts[3]
            : null;

        return (parts[1], ponderMoveNotation);
    }

    private static bool IsBatchScript(string processPath)
    {
        var extension = Path.GetExtension(processPath);
        return string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase)
               || string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup only.
        }
    }
}