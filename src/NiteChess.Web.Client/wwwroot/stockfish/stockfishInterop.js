const manifestCache = new Map();

export async function requestBestMove(bundleManifestPath, commands) {
  const runtimeBundle = await loadRuntimeBundle(bundleManifestPath);

  return new Promise((resolve, reject) => {
    const worker = new Worker(runtimeBundle.workerEntryPoint);
    const transcript = [];

    worker.onmessage = event => {
      const payload = event.data ?? {};

      if (payload.type === "line" && typeof payload.line === "string") {
        transcript.push(payload.line);
        return;
      }

      if (payload.type === "bestmove") {
        worker.terminate();
        resolve({
          BestMoveNotation: payload.bestMoveNotation ?? "",
          PonderMoveNotation: payload.ponderMoveNotation ?? null,
          Transcript: transcript
        });
        return;
      }

      if (payload.type === "runtime-error") {
        worker.terminate();
        reject(new Error(payload.message ?? "Browser Stockfish runtime is unavailable."));
      }
    };

    worker.onerror = event => {
      worker.terminate();
      reject(new Error(event.message || "Failed to launch the Stockfish browser worker."));
    };

    worker.postMessage({
      type: "analyze",
      commands,
      engineScriptPath: runtimeBundle.engineScript,
      engineWasmPath: runtimeBundle.engineWasm
    });
  });
}

async function loadRuntimeBundle(bundleManifestPath) {
  const normalizedManifestPath = normalizePath(bundleManifestPath);

  if (!manifestCache.has(normalizedManifestPath)) {
    manifestCache.set(
      normalizedManifestPath,
      fetch(normalizedManifestPath, { cache: "no-cache" }).then(async response => {
        if (!response.ok) {
          throw new Error(`Failed to load Stockfish bundle manifest '${normalizedManifestPath}'.`);
        }

        const manifest = await response.json();
        return {
          workerEntryPoint: toAbsoluteUrl(manifest.workerEntryPoint ?? "workers/stockfish.worker.js"),
          engineScript: toAbsoluteUrl(manifest.engineScript ?? "stockfish/stockfish.js"),
          engineWasm: toAbsoluteUrl(manifest.engineWasm ?? "stockfish/stockfish.wasm")
        };
      })
    );
  }

  return manifestCache.get(normalizedManifestPath);
}

function normalizePath(path) {
  const normalized = path.startsWith("wwwroot/") ? path.slice("wwwroot/".length) : path;
  return normalized.startsWith("/") ? normalized : `/${normalized}`;
}

function toAbsoluteUrl(path) {
  return new URL(normalizePath(path), window.location.href).toString();
}