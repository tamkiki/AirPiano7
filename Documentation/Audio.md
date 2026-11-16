# Audio sources and verification

The active game WAVs use seven separate **Meta Piano Vel03** acoustic upright piano recordings. They are licensed under **CC0-1.0**. The manifest records the source format and URLs, SHA-256 of the original downloads and final WAVs, pitch verification, peak and attack timing. The output files are 44,100 Hz mono 16-bit PCM with 79,380 frames each and a 300 ms fade to zero. The downloader checks the expected MIDI pitch against adjacent semitones and rejects trimmed clips with late attacks, clipping or an invalid WAV header.

To reinstall, run **python Tools/download_real_piano.py --source meta** from the workspace with Python and FFmpeg on PATH. The delivered WAVs were converted from official OGG Vorbis previews. This is the default download path. The official lossless release archive is about 752 MB. To use a verified official Meta release ZIP, pass its URL with **--source-archive**. The downloader checks each exact source WAV filename against the official collection manifest. It validates every output before replacing any active audio, backs up the current files under Audio_Backups and publishes the success manifest after replacing the WAVs.

The supplied starter package used temporary TimGM6mb sample-based audio. Its provenance and GPL-2 licensing are preserved in the supplied fallback documentation. The backed-up fallback files retain that licensing.

[Official collection specification](https://github.com/facebookincubator/Meta-open-sound/tree/main/systems/instruments/meta-piano) · [Collection license](https://github.com/facebookincubator/Meta-open-sound/blob/main/systems/instruments/meta-piano/LICENSE)

