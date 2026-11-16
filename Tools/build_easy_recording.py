"""Create a sparse, single-tap piano chart from the supplied recording."""
import argparse
from pathlib import Path
import json
import subprocess
import wave

import numpy as np
from scipy import ndimage, signal

RATE = 11025
OUTPUT_RATE = 44100
LANES = (9, 11, 1, 2, 4, 6, 8)
MIN_GAP = 1.72
TARGET_GAP = 2.12
DEADLINE_AFTER_CUE = .34


def clean_recording(source, trim):
    raw = subprocess.check_output(["ffmpeg", "-v", "error", "-i", str(source),
                                   "-ac", "2", "-ar", str(OUTPUT_RATE), "-f", "f32le", "-"])
    recording = np.frombuffer(raw, dtype="<f4").reshape(-1, 2)
    band = signal.butter(3, [48, 8500], "bandpass", fs=OUTPUT_RATE, output="sos")
    filtered = signal.sosfiltfilt(band, recording, axis=0)
    output = []
    for channel in filtered.T:
        _, times, bins = signal.stft(channel, OUTPUT_RATE, nperseg=2048, noverlap=1536)
        magnitude = np.abs(bins)
        room = (times >= 2.40) & (times <= 3.60)
        noise = np.percentile(magnitude[:, room], 75, axis=1, keepdims=True)
        local = ndimage.uniform_filter1d(magnitude * magnitude, size=5, axis=1)
        mask = 1 - 3.6 * noise ** 2 / np.maximum(local, 1e-13)
        mask = ndimage.gaussian_filter(np.clip(mask, .17, 1), sigma=(.6, 1))
        _, restored = signal.istft(bins * mask, OUTPUT_RATE, nperseg=2048, noverlap=1536)
        output.append(restored[:len(recording)])
    restored = np.stack(output, axis=1)
    quiet = slice(round(2.40 * OUTPUT_RATE), round(3.60 * OUTPUT_RATE))
    original_noise = np.sqrt(np.mean(recording[quiet] ** 2))
    cleaned_noise = np.sqrt(np.mean(restored[quiet] ** 2))
    print(f"Quiet reference noise change: {20*np.log10(cleaned_noise / original_noise):.1f} dB before gain.")
    start = round(trim * OUTPUT_RATE)
    # Preserve the end of the last audible piano phrase while softening the recording stop.
    result = restored[start:start + 93 * OUTPUT_RATE]
    fade = round(.30 * OUTPUT_RATE)
    result[-fade:] *= np.linspace(1, 0, fade)[:, None]
    peak = np.max(np.abs(result))
    return result * min(2.8, .89 / max(.01, peak))


def build(source, root, trim):
    recording = clean_recording(source, trim)
    audio = signal.resample_poly(recording.mean(axis=1), 1, 4)
    duration = len(recording) / OUTPUT_RATE
    frequencies, times, complex_spectrum = signal.stft(audio, RATE, nperseg=2048,
                                                       noverlap=1938, nfft=4096)
    spectrum = np.abs(complex_spectrum)
    midi = np.arange(56, 85)
    amplitudes = []
    for pitch in midi:
        index = round((440 * 2 ** ((pitch - 69) / 12)) / frequencies[1])
        amplitudes.append(np.max(spectrum[index - 1:index + 2], axis=0))
    amplitudes = ndimage.gaussian_filter1d(np.array(amplitudes), 1, axis=1)
    previous = np.pad(amplitudes[:, :-5], [(0, 0), (5, 0)])
    attacks = np.maximum(amplitudes - previous, 0)
    novelty = attacks.sum(axis=0)
    peaks, _ = signal.find_peaks(novelty, height=max(.0016, np.percentile(novelty, 72)), distance=12)
    peaks = [i for i in peaks if times[i] < duration - .45]
    chosen = []
    while peaks:
        if not chosen:
            pool = [i for i in peaks if times[i] <= times[peaks[0]] + .34]
            selected = max(pool, key=lambda i: novelty[i])
        else:
            limit = times[chosen[-1]] + MIN_GAP
            eligible = [i for i in peaks if times[i] >= limit]
            if not eligible:
                break
            preferred = times[chosen[-1]] + TARGET_GAP
            pool = [i for i in eligible if times[i] <= max(preferred + .34, times[eligible[0]] + .22)]
            selected = max(pool, key=lambda i: novelty[i] / (1 + 3 * abs(times[i] - preferred)))
        chosen.append(selected)
        peaks = [i for i in peaks if i > selected]

    notes = []
    for i in chosen:
        # Confirmation uses the strongest diatonic pitch at the selected attack.
        candidates = np.array([k for k, pitch in enumerate(midi) if pitch % 12 in LANES])
        scores = attacks[:, max(0, i - 1):i + 5].max(axis=1) + amplitudes[:, i] * .18
        pitch = int(midi[candidates[np.argmax(scores[candidates])]])
        onset = round(max(0, float(times[i]) - .035), 4)
        notes.append({"noteId": LANES.index(pitch % 12), "spawnBeat": round(onset + DEADLINE_AFTER_CUE, 4),
                      "displaySeconds": .46, "midiPitch": pitch})
    if not notes:
        raise ValueError("No playable attacks were detected.")

    audio_path = root / "Assets/AirPiano7/Audio/SomeoneLikeYou/RecordingEasy.wav"
    with wave.open(str(audio_path), "wb") as output:
        output.setparams((2, 2, OUTPUT_RATE, 0, "NONE", "not compressed"))
        output.writeframes(np.rint(np.clip(recording, -.98, .98) * 32767).astype("<i2").tobytes())
    chart = {"duration": duration, "pitches": [], "notes": notes}
    path = root / "Assets/AirPiano7/Data/SomeoneLikeYouEasyChart.json"
    path.write_text(json.dumps(chart, indent=2) + "\n")
    print(f"{duration:.2f}s audio, {len(notes)} cues, {min(b['spawnBeat']-a['spawnBeat'] for a,b in zip(notes,notes[1:])):.2f}s minimum gap")
    print([(n["spawnBeat"] - DEADLINE_AFTER_CUE, n["midiPitch"]) for n in notes[:9]])


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("recording", type=Path)
    parser.add_argument("--trim", type=float, default=3.92)
    args = parser.parse_args()
    build(args.recording, Path(__file__).resolve().parents[1], args.trim)
