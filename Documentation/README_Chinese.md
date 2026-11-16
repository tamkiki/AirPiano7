# Air Piano 7 — 钢琴 WAV 文件与真实录音下载工具

## 已提供的本地 WAV 文件是什么？

`Assets/AirPiano7/Audio/Piano_{C5,D5,E5,F5,G5,A5,B5}.wav` 中已经有七个 **基于录音采样的钢琴 SoundFont（TimGM6mb）渲染音频**，可以立即在 Unity 和 Codex 中测试；**这并不等同于七个琴键分别单独录制的原始钢琴录音**。旧的合成音七音符包没有被当成真实录音重命名。

如需真正逐音符的钢琴演奏录音，请在**有互联网连接的本地 Windows 电脑或 Codex 环境**运行下载工具（本次制作环境无法直接下载外部二进制音频）：

1. 将整个 ZIP 解压到 `AirPiano_7Note_Work` 的项目根目录（文件夹内应该有 `Assets/`、`Tools/`、`Documentation/`）。
2. 确保 Windows 已安装 Python 3 和 FFmpeg，并且在命令行运行 `ffmpeg -version` 正常。
3. 双击 `Tools/Download_Real_Piano_Windows.bat`，下载完成后会自动把真正的钢琴录音转换为 WAV，并覆盖项目中的这七个音效文件。
4. 若想让 Codex 执行，指令为：`python Tools/download_real_piano.py`。
5. 如 Meta 音源无法获取，工具会尝试 University of Iowa 的同一套钢琴录音，**不会混合不同钢琴的音符**。
6. 成功下载前不会覆盖旧 WAV。覆盖前会将原 WAV 备份到 `Audio_Backups/Before_Real_Recordings/`。
7. 成功后查看 `Documentation/Audio_Real_Recordings_Manifest.json`，确认 `downloaded: true`、原始音源名称及各文件的校验信息。

## 音频规格

- 文件名：`Piano_C5.wav` ... `Piano_B5.wav`
- PCM WAV；44,100 Hz；16-bit；单声道 (Mono)
- 默认 1.8 秒，保留真实琴声的起音与部分自然衰减，并做短淡出。
- 如确实需要与之前三音符测试**相同 0.72 秒长度**，运行：`python Tools/download_real_piano.py --duration 0.72`。
- 不需要额外的 Unity 插件，在 `PianoAudioPlayer.cs` / AudioClip Inspector 中关联即可。

## 推荐的真实录音来源

1. Meta Open Sound — Meta Piano，88 个逐音符录制的 Upright Piano 样本，采用 MF 力度层 OGG 录音作为输入；原音源 CC0 1.0；下载后转换为 WAV。来源：https://github.com/facebookincubator/Meta-open-sound
2. University of Iowa Musical Instrument Samples — Steinway Model B，MF 录音，来源明确声明可用于任何项目、不受限制。来源：https://theremin.music.uiowa.edu/MISpiano.html；授权说明：https://theremin.music.uiowa.edu/MIS.html

## 当前本地备用采样版权

本地备用声源来自 TimGM6mb SoundFont，Copyright (c) 2004 Tim Brechbill, 2010 David Bolton，按 GPL v2 提供。文档内附 `LICENSE_TimGM6mb_GPL2.txt`。如果未来公开发布游戏，建议优先运行下载工具改用 CC0 的 Meta 录音。

## Codex 使用建议

从项目根目录先运行 `python Tools/download_real_piano.py`；等出现 **SUCCESS** 再把 `Assets/AirPiano7/Audio/Piano_*.wav` 导入或自动关联为 Unity AudioClips。如果没有网络，当前本地备用 WAV 可用于先完成代码和界面，但不能标记为“已下载 Meta/Iowa 真实钢琴录音”。
