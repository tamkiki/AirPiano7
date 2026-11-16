using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AirPiano7.Editor
{
    [InitializeOnLoad]
    public static class VerifySomeoneLikeYou
    {
        private const string Running = "AirPiano7.VerifySomeoneLikeYou";
        private static PianoGame game;
        private static int next;
        private static int phase;
        private static bool heardBacking;
        private static bool playedKeyTone;
        private static bool captured;
        private static bool beforeCaptured;
        private static bool midTurnCaptured;
        private static bool Polished => Array.IndexOf(Environment.GetCommandLineArgs(), "-airPianoPolished") >= 0;

        static VerifySomeoneLikeYou()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene(BuildSevenNoteDemo.SomeoneLikeYouScene);
            if (Polished)
            {
                var original = Object.FindObjectOfType<PianoGame>();
                var originalSong = original.song;
                float fall = original.fallDuration;
                float lead = original.hintLeadTime;
                float perfect = original.perfectWindow;
                EditorSceneManager.OpenScene(PolishSomeoneLikeYou.ScenePath);
                var styled = Object.FindObjectOfType<PianoGame>();
                Require(styled.song == originalSong && styled.fallDuration == fall &&
                    styled.hintLeadTime == lead && styled.perfectWindow == perfect,
                    "Visual copy keeps the established song and hit timing");
                var gardens = Object.FindObjectsOfType<BloomGarden>();
                Require(gardens.Length == 2 && Array.TrueForAll(gardens, garden => !garden.raycastTarget),
                    "Both flower layers pass all input through");
                var display = styled.GetComponentInChildren<Camera>();
                Require(display && display.enabled && display.targetDisplay == 0 &&
                    !display.targetTexture && display.cullingMask == 0 &&
                    styled.score.canvas.renderMode == RenderMode.ScreenSpaceOverlay,
                    "Display 1 has a screen camera behind the 2D Canvas");
                foreach (var lane in styled.lanes)
                {
                    var key = lane.keySurface;
                    Require(styled.score.canvas.pixelPerfect &&
                        Mathf.Abs(key.parent.localScale.x * key.localScale.x - 1) < .001f &&
                        Mathf.Abs(key.position.x - lane.track.position.x) < .01f &&
                        key.Find("Keyboard binding").GetComponent<Text>().fontSize == 30,
                        "Keyboard labels stay at native proportions and centered on their lanes");
                }
            }
            var candidate = Object.FindObjectOfType<PianoGame>();
            Require(candidate.song.notes.Length >= 23 && candidate.song.notes.Length <= 39 && !candidate.song.backingIncludesPlayableNotes &&
                candidate.song.pitchedClips.Length > 0 &&
                Math.Abs(candidate.song.backing.length - 90.6933) < .01, "Sparse chart and complete accompaniment duration");
            var model = new RhythmSession(candidate.song.notes, 1, candidate.fallDuration, candidate.hintLeadTime, candidate.perfectWindow);
            for (var i = 1; i < candidate.song.notes.Length; i++)
                Require(candidate.song.notes[i].spawnBeat - candidate.song.notes[i - 1].spawnBeat >= 2.219f,
                    "Spaced playable notes");
            var ending = Array.FindAll(candidate.song.notes, note => note.spawnBeat >= 81.5f);
            Require(ending.Length == 3 && ending[0].midiPitch == 76 &&
                ending[1].midiPitch == 73 && ending[2].midiPitch == 69, "Three-note descending ending");
            foreach (var note in candidate.song.notes)
                Require(model.TryHit(note.noteId, 2.8 + note.spawnBeat - .34), "Chart note at recording cue");
            Require(model.Finished && model.Perfect == candidate.song.notes.Length, "All chart notes are playable in sequence");
            if (!Polished) EditorSceneManager.OpenScene(BuildSevenNoteDemo.ScenePath);
            SessionState.SetBool(Running, true);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || !SessionState.GetBool(Running, false)) return;
            try
            {
                game = Object.FindObjectOfType<PianoGame>();
                if (!game || Time.frameCount < 3) return;
                if (phase == 0)
                {
                    if (!Polished) SwitchSong();
                    phase = 1;
                    return;
                }
                if (phase == 1)
                {
                    if (!game.song.backing) return;
                    game.startButton.onClick.Invoke();
                    phase = 2;
                    return;
                }
                if (phase == 2)
                {
                    double now = game.SongTime;
                    heardBacking |= game.backingSource.isPlaying && now > game.fallDuration + .1;
                    if (Polished && !beforeCaptured && now >= 2.05 && now < 2.40)
                    {
                        Require(Object.FindObjectOfType<BloomGarden>().BeatPulse == 0, "Garden is resting before first beat");
                        StageCapture.Save(game, "FloatingFlowers-Before");
                        beforeCaptured = true;
                    }
                    if (Polished && !captured && now >= 3.21)
                    {
                        Require(Object.FindObjectOfType<BloomGarden>().HitBlooms >= 1,
                            "Successful note opened the flowers");
                        StageCapture.Save(game, "FloatingFlowers-OnBeat");
                        captured = true;
                    }
                    if (Polished && !midTurnCaptured && now >= 3.73)
                    {
                        bool highResolution = Array.IndexOf(Environment.GetCommandLineArgs(), "-airPianoHighRes") >= 0;
                        StageCapture.Save(game, highResolution ? "GardenHighRes" : "FlowerSpin-MidTurn", highResolution);
                        midTurnCaptured = true;
                    }
                    while (next < game.song.notes.Length &&
                        now >= game.fallDuration + game.song.notes[next].spawnBeat - .34 +
                        (Polished && next == 0 ? .065 : 0))
                    {
                        if (Polished && next == 0)
                        {
                            var garden = Object.FindObjectOfType<BloomGarden>();
                            Require(garden.HitBlooms == 0 && garden.BeatPulse > .40f, "Audio beat opens flowers before input");
                        }
                        var note = game.song.notes[next++];
                        game.Submit(note.noteId);
                        playedKeyTone |= game.piano.GetComponent<AudioSource>().isPlaying;
                        if (next <= 3) Debug.Log($"SOMEONE LIKE YOU VERIFY: onset {note.spawnBeat:F4}, clock {now:F4}, score {game.score.text}, {game.feedback.text}");
                    }
                    if (next >= 3 && Array.IndexOf(Environment.GetCommandLineArgs(), "-airPianoSongSmoke") >= 0)
                    {
                        Require(heardBacking && game.score.text == "00300" && game.piano.GetComponent<AudioSource>().isPlaying, "Opening playback and first three hits");
                        if (Polished) Require(captured && beforeCaptured && midTurnCaptured, "Captured flower opening and mid-turn");
                        Debug.Log("SOMEONE LIKE YOU VERIFY: PASS — opening accompaniment and three cues");
                        End(0);
                        return;
                    }
                    if (!game.resultsPanel.activeSelf) return;
                    Debug.Log($"SOMEONE LIKE YOU VERIFY: DSP {now:F3}, backing {heardBacking}, notes {next}, {game.summary.text.Replace("\n", " / ")}");
                    Require(heardBacking && playedKeyTone && now >= 93.4 && next == game.song.notes.Length &&
                        game.score.text == (next * 100).ToString("D5"), "Full playback, backing and scores");
                    if (Polished)
                        Require(Array.TrueForAll(Object.FindObjectsOfType<BloomGarden>(),
                            garden => garden.HitBlooms == next), "Every cue triggered both floating-flower layers");
                    SwitchSong();
                    phase = 3;
                    return;
                }
                if (phase == 3 && game.song.title == "Afterglow")
                {
                    Debug.Log($"SOMEONE LIKE YOU VERIFY: PASS — both song links, {next} hits, full accompaniment and results");
                    End(0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                End(1);
            }
        }

        private static void SwitchSong()
        {
            var parent = phase == 2 ? game.resultsPanel : game.startPanel;
            var button = Array.Find(parent.GetComponentsInChildren<Button>(), item => item.name == "Switch song");
            Require(button, "Song switch button");
            button.onClick.Invoke();
        }

        private static void End(int code)
        {
            SessionState.EraseBool(Running);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(code);
        }

        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Someone Like You verification failed: " + label);
        }
    }
}
