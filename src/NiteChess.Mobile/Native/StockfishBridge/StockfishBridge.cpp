#include <cstdlib>
#include <cstring>
#include <iostream>
#include <mutex>
#include <sstream>
#include <stdexcept>
#include <string>
#include <string_view>

#include "bitboard.h"
#include "position.h"
#include "tune.h"
#include "uci.h"

namespace {

std::once_flag stockfish_init_flag;
std::mutex     stockfish_bridge_mutex;

void initialize_stockfish() {
    Stockfish::Bitboards::init();
    Stockfish::Position::init();
}

char* duplicate_utf8(const std::string& value) {
    auto* buffer = static_cast<char*>(std::malloc(value.size() + 1));
    if (buffer == nullptr) {
        return nullptr;
    }

    std::memcpy(buffer, value.c_str(), value.size() + 1);
    return buffer;
}

std::string extract_bestmove_line(const std::string& transcript) {
    std::istringstream lines(transcript);
    std::string        line;
    std::string        bestmove;

    while (std::getline(lines, line)) {
        if (line.rfind("bestmove ", 0) == 0) {
            bestmove = line;
        }
    }

    if (bestmove.empty()) {
        throw std::runtime_error("Stockfish bridge did not emit a bestmove line.");
    }

    return bestmove;
}

std::string run_stockfish_request(std::string_view command_payload) {
    std::call_once(stockfish_init_flag, initialize_stockfish);
    std::lock_guard<std::mutex> guard(stockfish_bridge_mutex);

    std::istringstream input(std::string(command_payload) + "\nquit\n");
    std::ostringstream output;

    auto* original_cin = std::cin.rdbuf(input.rdbuf());
    auto* original_cout = std::cout.rdbuf(output.rdbuf());

    char  arg0[] = "stockfish";
    char* argv[] = {arg0, nullptr};

    try {
        Stockfish::UCIEngine uci(1, argv);
        Stockfish::Tune::init(uci.engine_options());
        uci.loop();
    }
    catch (...) {
        std::cin.rdbuf(original_cin);
        std::cout.rdbuf(original_cout);
        throw;
    }

    std::cin.rdbuf(original_cin);
    std::cout.rdbuf(original_cout);

    return extract_bestmove_line(output.str());
}

}  // namespace

extern "C" const char* nitechess_stockfish_request_bestmove_utf8(const char* command_payload_utf8) {
    try {
        if (command_payload_utf8 == nullptr || command_payload_utf8[0] == '\0') {
            return duplicate_utf8("bestmove (none)");
        }

        return duplicate_utf8(run_stockfish_request(command_payload_utf8));
    }
    catch (const std::exception& exception) {
        return duplicate_utf8(std::string("error ") + exception.what());
    }
    catch (...) {
        return duplicate_utf8("error unhandled-stockfish-bridge-exception");
    }
}

extern "C" void nitechess_stockfish_free_utf8(const char* value) {
    std::free(const_cast<char*>(value));
}