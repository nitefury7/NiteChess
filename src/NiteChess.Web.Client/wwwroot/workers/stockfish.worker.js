self.addEventListener("message", async event => {
  const payload = event.data ?? {};

  if (payload.type === "init") {
    self.postMessage({
      type: "ready",
      expectedScript: "/stockfish/stockfish.js",
      expectedWasm: "/stockfish/stockfish.wasm"
    });
    return;
  }

  if (payload.type !== "analyze") {
    return;
  }

  try {
    await analyze(Array.isArray(payload.commands) ? payload.commands : []);
  } catch (error) {
    self.postMessage({
      type: "runtime-error",
      commands: Array.isArray(payload.commands) ? payload.commands : [],
      message: error instanceof Error ? error.message : String(error)
    });
  }
});

async function analyze(commands) {
  const engine = createEngineWorker(commands);
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
  }, 15000);

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
      ponderMoveNotation: parsed.ponderMoveNotation
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

function createEngineWorker(commands) {
  try {
    return new Worker("../stockfish/stockfish.js");
  } catch (error) {
    self.postMessage({
      type: "runtime-unavailable",
      commands,
      message:
        error instanceof Error
          ? error.message
          : "Stockfish WebAssembly assets are not bundled yet. Add /stockfish/stockfish.js and /stockfish/stockfish.wasm to activate offline browser play."
    });
    throw error;
  }
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