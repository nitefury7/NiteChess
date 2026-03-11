self.addEventListener("message", async event => {
  const payload = event.data ?? {};

  if (payload.type === "init") {
    self.postMessage({
      type: "ready",
      expectedScript: payload.engineScriptPath ?? "/stockfish/stockfish.js",
      expectedWasm: payload.engineWasmPath ?? "/stockfish/stockfish.wasm"
    });
    return;
  }

  if (payload.type !== "analyze") {
    return;
  }

  try {
    await analyze(
      Array.isArray(payload.commands) ? payload.commands : [],
      payload.engineScriptPath,
      payload.engineWasmPath);
  } catch (error) {
    self.postMessage({
      type: "runtime-error",
      commands: Array.isArray(payload.commands) ? payload.commands : [],
      message: error instanceof Error ? error.message : String(error)
    });
  }
});

async function analyze(commands, engineScriptPath, engineWasmPath) {
  const engine = createEngineWorker(engineScriptPath, engineWasmPath);
  let settled = false;
  const timeoutHandle = setTimeout(() => {
    if (settled) {
      return;
    }

    settled = true;
    engine.terminate();
    self.postMessage({
      type: "runtime-error",
      commands,
      message: "Timed out waiting for a Stockfish WebAssembly bestmove response."
    });
  }, 60000);

  engine.onmessage = event => {
    const line = normalizeEngineMessage(event.data);
    if (!line) {
      return;
    }

    self.postMessage({ type: "line", line });

    if (!line.startsWith("bestmove ")) {
      return;
    }

    const parsed = parseBestMove(line);
    settled = true;
    clearTimeout(timeoutHandle);
    engine.terminate();
    self.postMessage({
      type: "bestmove",
      bestMoveNotation: parsed.bestMoveNotation,
      ponderMoveNotation: parsed.ponderMoveNotation,
      engineWasmPath
    });
  };

  engine.onerror = event => {
    if (settled) {
      return;
    }

    settled = true;
    clearTimeout(timeoutHandle);
    engine.terminate();
    self.postMessage({
      type: "runtime-error",
      commands,
      message: event.message || "The Stockfish WebAssembly worker failed while loading or evaluating."
    });
  };

  for (const command of commands) {
    engine.postMessage(command);
  }
}

function createEngineWorker(engineScriptPath, engineWasmPath) {
  if (typeof engineScriptPath !== "string" || engineScriptPath.length === 0) {
    throw new Error("Stockfish engine script path was not provided to the browser worker.");
  }

  if (typeof engineWasmPath !== "string" || engineWasmPath.length === 0) {
    throw new Error("Stockfish engine wasm path was not provided to the browser worker.");
  }

  const engineScriptUrl = new URL(engineScriptPath, self.location.origin).href;
  const engineWasmUrl = new URL(engineWasmPath, self.location.origin).href;
  return new Worker(`${engineScriptUrl}#${encodeURIComponent(engineWasmUrl)}`);
}

function normalizeEngineMessage(message) {
  if (typeof message === "string") {
    return message;
  }

  if (message && typeof message.message === "string") {
    return message.message;
  }

  return message == null ? "" : String(message);
}

function parseBestMove(line) {
  const parts = line.trim().split(/\s+/);
  return {
    bestMoveNotation: parts[1] ?? "",
    ponderMoveNotation: parts[2] === "ponder" ? parts[3] ?? null : null
  };
}