"""Read the note timing and pitches from a standard MIDI file."""
from collections import defaultdict, deque
from pathlib import Path
import struct


def read_midi(path):
    content = Path(path).read_bytes()
    if content[:4] != b"MThd":
        raise ValueError("Expected a standard MIDI file.")
    size, kind, tracks, division = struct.unpack_from(">IHHH", content, 4)
    if kind not in (0, 1) or not 0 < division < 32768:
        raise ValueError("Only format 0/1 MIDI files with PPQN timing are supported.")
    offset = 8 + size
    tempos = [(0, 500000)]
    raw_notes = []
    labels = {}
    programs = {}
    for track in range(tracks):
        if content[offset:offset + 4] != b"MTrk":
            raise ValueError("Missing MIDI track header.")
        length, = struct.unpack_from(">I", content, offset + 4)
        stream = iter(content[offset + 8:offset + 8 + length])
        offset += length + 8
        time = 0
        running = 0
        pending = defaultdict(deque)

        def variable():
            number = 0
            for _ in range(4):
                byte = next(stream)
                number = (number << 7) | (byte & 127)
                if byte < 128:
                    return number
            raise ValueError("Invalid MIDI variable-length value.")

        while True:
            time += variable()
            first = next(stream)
            if first == 255:
                tag = next(stream)
                data = bytes(next(stream) for _ in range(variable()))
                if tag == 47:
                    break
                if tag == 81:
                    tempos.append((time, int.from_bytes(data, "big")))
                elif tag == 3:
                    labels[track] = data.decode("utf-8", "replace")
                continue
            if first in (240, 247):
                for _ in range(variable()):
                    next(stream)
                running = 0
                continue
            if first >= 128:
                running = first
                first = next(stream)
            command, channel = running >> 4, running & 15
            if command in (12, 13):
                if command == 12:
                    programs[track, channel] = first
                continue
            if command not in (8, 9, 10, 11, 14):
                raise ValueError(f"Unknown MIDI command {command}.")
            second = next(stream)
            key = (channel, first)
            if command == 9 and second:
                pending[key].append((time, second))
            elif command in (8, 9) and pending[key]:
                start, velocity = pending[key].popleft()
                if time > start:
                    raw_notes.append((start, time, first, velocity, track, channel))

    timeline = []
    elapsed = 0
    prev_tick, previous_tempo = 0, 500000
    for tick, tempo in sorted(tempos, key=lambda pair: pair[0]):
        elapsed += (tick - prev_tick) * previous_tempo / (1_000_000 * division)
        timeline.append((tick, elapsed, tempo))
        prev_tick, previous_tempo = tick, tempo

    def seconds(tick):
        start, elapsed, tempo = next(item for item in reversed(timeline) if item[0] <= tick)
        return elapsed + (tick - start) * tempo / (1_000_000 * division)

    notes = [(seconds(a), seconds(b), pitch, velocity, track, channel)
             for a, b, pitch, velocity, track, channel in sorted(raw_notes)]
    return notes, labels, programs
