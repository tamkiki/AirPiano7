using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AirPiano7.Editor
{
    [InitializeOnLoad]
    public static class VerifySevenNoteDemo
    {
        private const string Running = "AirPiano7.Verify.Running";
        private static PianoGame game;
        private static SerialSimulator simulator;
        private static double started;
        private static int next;
        private static int phase;
        private static bool portFailure;
        private static double restartTime;
        private static bool captured;

        static VerifySevenNoteDemo()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += VerifyPlay;
        }

        [MenuItem("Air Piano/Verify Seven-Note Demo")]
        public static void Run()
        {
            VerifyRules();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(BuildSevenNoteDemo.ScenePath);
            game = Object.FindObjectOfType<PianoGame>();
            Require(game && game.song.notes.Length == 13 && game.lanes.Length == 7, "Chart and lane count");
            var expected = new[] { 0, 2, 4, 6, 5, 4, 3, 2, 1, 3, 2, 1, 0 };
            var keys = new[] { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J };
            for (int id = 0; id < 7; id++)
            {
                var lane = game.lanes[id];
                Require(lane.key == keys[id] && lane.track && lane.keyFace && lane.notePrefab, $"Lane and key binding {id}");
                Require(game.piano.clips[id] && game.piano.clips[id].name == "Piano_" + lane.pitch,
                    $"AudioClip mapping {id}");
            }
            for (int i = 0; i < expected.Length; i++)
                Require(game.song.notes[i].noteId == expected[i] && game.song.notes[i].spawnBeat == i, $"Chart entry {i}");
            Require(game.sparkPrefab && game.startPanel.activeSelf && !game.resultsPanel.activeSelf &&
                !Object.FindObjectOfType<ArduinoSerialInput>().enabled, "Initial UI and keyboard-only configuration");
            VerifyStage(game);
            Require(game.fallDuration == 4.5f && game.noteInterval == 3f && game.hintLeadTime == 2f &&
                game.perfectWindow == .4f, "Inspector timing defaults");
            Debug.Log("AIR PIANO VERIFY: model, protocol and serialized scene PASS");
            SessionState.SetBool(Running, true);
            EditorApplication.update -= VerifyPlay;
            EditorApplication.update += VerifyPlay;
            EditorApplication.isPlaying = true;
        }

        private static void VerifyStage(PianoGame controller)
        {
            var scaler = controller.score.canvas.GetComponent<CanvasScaler>();
            Require(scaler.referenceResolution == new Vector2(1920, 1080), "Canvas reference size");
            Require(controller.hitRing && !controller.hitRing.raycastTarget, "Hit effect layer");
            const float smallerScale = 1280f / 1920;
            for (int id = 0; id < controller.lanes.Length; id++)
            {
                var lane = controller.lanes[id];
                var blockName = lane.notePrefab.GetComponentInChildren<Text>();
                var keyName = lane.keyFace.transform.Find("Pitch").GetComponent<Text>();
                var shortcut = lane.keyFace.transform.Find("Keyboard binding").GetComponent<Text>();
                Require(lane.hintStrip && lane.keySurface, "Key feedback " + id);
                Require(Mathf.Abs(lane.track.position.x - lane.keySurface.position.x) < .01f &&
                    lane.notePrefab.pivot.y == 0, "Roll note aligns with the key and uses its bottom edge " + id);
                Require(lane.notePrefab.rect.width < lane.track.rect.width, "Note fits its lane " + id);
                Require(blockName.fontSize * smallerScale >= 20 &&
                    keyName.fontSize * smallerScale >= 24 && shortcut.fontSize * smallerScale >= 16,
                    "Minimum nominal text sizes at 720p for lane " + id);
            }
        }

        private static void VerifyRules()
        {
            var displayLength = new RhythmSession(new[] { new SongNote(0, 0, 1.8f) }, 3, 4.5, 2, .4);
            displayLength.Advance(0);
            Require(displayLength.Active[0].DisplaySeconds == 1.8f && displayLength.Active[0].Target == 4.5,
                "Display duration does not alter hit timing");
            var entry = new[] { new SongNote(0, 0) };
            RhythmSession Create() => new RhythmSession(entry, 3, 4.5, 2, .4);
            foreach (var test in new[] { (2.49, 0), (2.50, 100), (2.90, 100), (3.20, 60), (4.50, 0) })
            {
                var model = Create();
                model.TryHit(0, test.Item1);
                Require(model.Score == test.Item2, "Boundary " + test.Item1);
            }
            var deadline = Create();
            deadline.Advance(4.5);
            Require(deadline.Finished && deadline.Miss == 1, "Miss and finish at deadline");

            var overlap = new RhythmSession(new[] { new SongNote(0, 0), new SongNote(0, .1f),
                new SongNote(1, .2f), new SongNote(2, 1) }, 1, 4.5, 2, .4);
            var transitions = new NoteTransition();
            var protocol = new NoteProtocol();
            double now = 2.49;
            void Feed(string s) => protocol.Feed(s, id => transitions.Receive(id, idToPlay => overlap.TryHit(idToPlay, now)));
            Feed("NOTE,0,31.0\n");
            now = 2.73;
            Feed("NOTE,0,31.0\nNOTE,8,10\nRAW,31,30,0\nNOTE,2,NaN\nNOTE,6,20\n");
            Require(overlap.Resolved == 0 && overlap.Active.Count == 4, "Early held input, wrong ID and simultaneous notes");
            Feed("NOTE,-1,-1\r\nNO");
            Feed("TE,0,31.0\nNOTE,0,31.0\n");
            Require(overlap.Active[0].Spawn > 0 && overlap.Score == 100 && overlap.Combo == 1, "Earliest pitch and deduplication");
            Feed("NOTE,-1,-1\nNOTE,0,31.0\nNOTE,1,73.0\n");
            Require(overlap.Score == 300 && overlap.Combo == 3 && !overlap.Finished, "Release, re-entry and area change");
            overlap.Advance(5.5);
            Require(overlap.Finished && overlap.Miss == 1 && overlap.Combo == 0, "Miss resets combo");

            var parsed = new List<int>();
            foreach (var fragment in new[] { "NO", "TE,6,158.0\r\nNOTE,", "-1,-1.0\nNOTE,7,10\nNOTE,2,NaN\n",
                new string('X', 130) + "NOTE,5,20\nNOTE,4,101.5\n" }) protocol.Feed(fragment, parsed.Add);
            Require(string.Join(",", parsed) == "6,-1,4", "Framing, overflow recovery and malformed values");
        }

        private static void VerifyPlay()
        {
            if (!EditorApplication.isPlaying || !SessionState.GetBool(Running, false)) return;
            try
            {
                if (phase == 0)
                {
                    game = Object.FindObjectOfType<PianoGame>();
                    if (!game || Time.frameCount < 3) return;
                    simulator = Object.FindObjectOfType<SerialSimulator>();
                    VerifyMissingClip();
                    var serial = Object.FindObjectOfType<ArduinoSerialInput>();
                    serial.portName = "COM65535";
                    Application.logMessageReceived += ObservePortFailure;
                    serial.enabled = true;
                    game.fallDuration = 1.2f;
                    game.hintLeadTime = .8f;
                    game.noteInterval = .19f;
                    game.perfectWindow = .5f;
                    var source = game.piano.GetComponent<AudioSource>();
                    game.startButton.onClick.Invoke();
                    simulator.Feed("NOTE,0,31.0\nNOTE,-1,-1.0\nNOTE,6,20.0\n");
                    Require(!source.isPlaying && game.score.text == "00000", "Early and wrong input are silent");
                    started = Time.unscaledTimeAsDouble;
                    phase = 1;
                }
                var time = Time.unscaledTimeAsDouble - started;
                if (phase == 1)
                {
                    while (next < game.song.notes.Length && time >= .47 + next * game.noteInterval)
                    {
                        var source = game.piano.GetComponent<AudioSource>();
                        source.Stop();
                        simulator.Feed("NOTE,-1,-1.0\n");
                        simulator.Feed($"NOTE,{game.song.notes[next++].noteId},31.0\n");
                        Require(source.isPlaying, $"Successful hit starts AudioSource for chart entry {next}");
                    }
                    if (!captured && time >= .73 && Array.IndexOf(Environment.GetCommandLineArgs(), "-airPianoCapture") >= 0)
                    {
                        Require(game.lanes[2].keyFace.enabled && !Object.FindObjectOfType<Camera>() &&
                            !Object.FindObjectOfType<RawImage>(), "Gameplay uses the overlay Canvas without offscreen rendering");
                        StageCapture.Save(game);
                        captured = true;
                    }
                    if (!game.resultsPanel.activeSelf)
                    {
                        Require(time < 7, "Timed out waiting for results");
                        return;
                    }
                    Require(game.progress.text == "13 / 13" && game.score.text == "01300" &&
                        game.combo.text.EndsWith("13") && game.summary.text.Contains("13 PERFECT"), "Complete melody and results UI");
                    game.restartButton.onClick.Invoke();
                    Require(!game.resultsPanel.activeSelf && game.progress.text == "0 / 13" &&
                        game.score.text == "00000", "Restart resets results and counters");
                    restartTime = Time.unscaledTimeAsDouble;
                    phase = 2;
                }
                if (phase == 2 && Time.unscaledTimeAsDouble - restartTime > 1.26)
                {
                    var source = game.piano.GetComponent<AudioSource>();
                    Require(!source.isPlaying && game.feedback.text.Contains("MISS"), "Miss is silent");
                    Require(portFailure, "Missing serial port reported safely");
                    Debug.Log("AIR PIANO VERIFY: Play mode simulator, full melody, audio starts and rejects, results, restart and missing COM PASS");
                    End(0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                End(1);
            }
        }

        private static void ObservePortFailure(string message, string stack, LogType type)
        {
            if (type == LogType.Warning && message.StartsWith("Air Piano serial COM65535:"))
                portFailure = true;
        }

        private static void VerifyMissingClip()
        {
            var temporary = new GameObject("Missing AudioClip verification");
            temporary.SetActive(false);
            var piano = temporary.AddComponent<SevenNoteAudio>();
            piano.clips = (AudioClip[])game.piano.clips.Clone();
            piano.clips[6] = null;
            bool reported = false;
            void Observe(string message, string stack, LogType type)
            {
                if (type == LogType.Error && message.Contains("missing AudioClip for note ID 6")) reported = true;
            }
            Application.logMessageReceived += Observe;
            try { temporary.SetActive(true); }
            finally
            {
                Application.logMessageReceived -= Observe;
                Object.Destroy(temporary);
            }
            Require(reported, "Missing clip is reported");
        }

        private static void End(int code)
        {
            SessionState.EraseBool(Running);
            EditorApplication.update -= VerifyPlay;
            Application.logMessageReceived -= ObservePortFailure;
            portFailure = false;
            captured = false;
            phase = next = 0;
            if (Application.isBatchMode) EditorApplication.Exit(code);
            else EditorApplication.isPlaying = false;
        }

        private static void Require(bool condition, string check)
        {
            if (!condition) throw new InvalidOperationException("Air Piano verification failed: " + check);
        }
    }
}

