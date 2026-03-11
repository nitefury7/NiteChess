export function requestBestMove(workerPath, commands) {
  return new Promise((resolve, reject) => {
    const worker = new Worker(normalizePath(workerPath));
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

      if (payload.type === "runtime-unavailable" || payload.type === "runtime-error") {
        worker.terminate();
        reject(new Error(payload.message ?? "Browser Stockfish runtime is unavailable."));
      }
    };

    worker.onerror = event => {
      worker.terminate();
      reject(new Error(event.message || "Failed to launch the Stockfish browser worker."));
    };

    worker.postMessage({ type: "analyze", commands });
  });
}

function normalizePath(path) {
  return path.startsWith("/") ? path : `/${path}`;
}