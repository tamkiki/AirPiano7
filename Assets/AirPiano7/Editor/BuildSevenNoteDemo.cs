using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AirPiano7.Editor
{
    public static class BuildSevenNoteDemo
    {
        public const string Root = "Assets/AirPiano7";
        public const string ScenePath = Root + "/Scenes/SevenNoteVisualDemo.unity";
        public const string SomeoneLikeYouScene = Root + "/Scenes/SomeoneLikeYou.unity";
        private static readonly string[] SongPitches = { "A", "B", "C#", "D", "E", "F#", "G#" };
        private static readonly string[] Pitches = { "C5", "D5", "E5", "F5", "G5", "A5", "B5" };
        private static readonly KeyCode[] Keys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J };
        private static readonly string[] Colors = { "2BC99A", "41C6D0", "FFBD45", "FF7774", "FF9E42", "AB8CFF", "50B5FF" };
        private static readonly Color Ink = FromHex("0B1121");
        private static readonly Color White = FromHex("F3F4FF");
        private static readonly Color Muted = FromHex("9EAED0");
        private static readonly Color Gold = FromHex("FFDB73");
        private const float LaneWidth = 228f;
        private const float RunwayHeight = 585f;
        private const float JudgementY = -240f;
        private static Font font;

        [MenuItem("Air Piano/Build Seven-Note Demo")]
        public static void Run() => Build(false);

        [MenuItem("Air Piano/Build Someone Like You")]
        public static void BuildSomeoneLikeYou() => Build(true);

        public static void BuildBothSongs()
        {
            Build(false);
            Build(true);
        }

        private static void Build(bool imported)
        {
            var pathToSave = imported ? SomeoneLikeYouScene : ScenePath;
            var lanePitches = imported ? SongPitches : Pitches;
            if (!Application.isBatchMode)
            {
                if (File.Exists(pathToSave) && !EditorUtility.DisplayDialog("Rebuild piano scene?",
                    "Replace the generated scene layout? Existing song and prefab assets will be reused.",
                    "Rebuild Scene", "Cancel")) return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            }
            else if (File.Exists(pathToSave) && !(imported ? CanUpgradeSomeoneLikeYou() : CanUpgradeOriginal()))
            {
                Debug.Log("Air Piano: existing scene preserved in batch mode.");
                return;
            }

            var clips = new AudioClip[Pitches.Length];
            for (int i = 0; i < Pitches.Length; i++)
            {
                var path = $"{Root}/Audio/Piano_{Pitches[i]}.wav";
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (!clips[i]) throw new InvalidOperationException("Missing or unreadable AudioClip: " + path);
            }
            foreach (var folder in new[] { "Data", "Scenes", "Prefabs" })
                if (!AssetDatabase.IsValidFolder(Root + "/" + folder)) AssetDatabase.CreateFolder(Root, folder);

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var system = new GameObject("Air Piano Controllers");
            system.AddComponent<AudioListener>();
            var audio = system.AddComponent<SevenNoteAudio>();
            audio.clips = clips;
            var source = system.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = .82f;
            var game = system.AddComponent<PianoGame>();
            game.song = imported ? LoadSomeoneLikeYou() : BuildSong();
            game.piano = audio;
            if (imported)
            {
                game.backingSource = new GameObject("Piano backing").AddComponent<AudioSource>();
                game.backingSource.transform.SetParent(system.transform, false);
                game.backingSource.playOnAwake = false;
                game.backingSource.volume = .95f;
                game.noteInterval = 1;
                game.fallDuration = 2.8f;
                game.hintLeadTime = .78f;
                game.perfectWindow = .58f;
            }
            system.AddComponent<SerialSimulator>().game = game;
            var serial = system.AddComponent<ArduinoSerialInput>();
            serial.game = game;
            serial.enabled = false;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvas = new GameObject("Air Piano Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var root = canvas.GetComponent<RectTransform>();
            var background = Fill("Background", root, Ink);
            var wash = Surface("Stage color wash", root, 0, 0, 1920, 1080,
                FromHex("101526"), FromHex("090C15"));
            wash.rectTransform.SetSiblingIndex(background.rectTransform.GetSiblingIndex() + 1);
            var frame = Box("Stage", root, 0, 0, 1648, 1000);
            Surface("Title accent", frame, -815, 431, 4, 64, FromHex("6475FF"), FromHex("A1FAFF"), radius: 2);
            Label("Wordmark", frame, "AIR PIANO", 49, White, -650, 442, 300, 70, TextAnchor.MiddleLeft, FontStyle.Bold);
            game.title = Label("Song title", frame, game.song.title, 28, Muted, -606, 386, 388, 50, TextAnchor.MiddleLeft);
            Label("Score label", frame, "SCORE", 19, Muted, 112, 460, 190, 30);
            game.score = Label("Score", frame, "00000", 43, White, 112, 416, 220, 58);
            Label("Combo label", frame, "COMBO", 19, Muted, 354, 460, 180, 30);
            game.combo = Label("Combo", frame, "×0", 43, White, 354, 416, 180, 58);
            Label("Progress label", frame, "SONG PROGRESS", 19, Muted, 644, 460, 260, 30);
            game.progress = Label("Progress count", frame, "", 31, White, 644, 425, 260, 40);
            var bar = Panel("Progress track", frame, FromHex("253454"), 644, 396, 252, 7).rectTransform;
            game.progressFill = Fill("Progress fill", bar, Gold).rectTransform;

            var runway = Box("Perspective runway", frame, 0, JudgementY + RunwayHeight / 2,
                LaneWidth * 7, RunwayHeight);
            runway.gameObject.AddComponent<RectMask2D>();
            Panel("Roll background", runway, FromHex("101216"), 0, 0, LaneWidth * 7, RunwayHeight);
            game.lanes = new PianoGame.Lane[Pitches.Length];
            for (int id = 0; id <= Pitches.Length; id++)
            {
                float x = (id - Pitches.Length / 2f) * LaneWidth;
                Panel("Vertical key guide " + id, runway, FromHex(id == 0 || id == Pitches.Length ? "353940" : "272A30"),
                    x, 0, 2, RunwayHeight);
            }
            for (int line = 1; line < 5; line++)
            {
                float y = (line / 5f - .5f) * RunwayHeight;
                Panel("Horizontal guide " + line, runway, FromHex("1C1F24"), 0, y, LaneWidth * 7, 1.5f);
            }

            var laneLights = new StageLight[Pitches.Length];
            for (int id = 0; id < Pitches.Length; id++)
                laneLights[id] = Light(lanePitches[id] + " lane light", runway, (id - 3) * LaneWidth, -300,
                    192, 188, Color.clear);
            var noteLayer = Box("Notes and pitch labels", frame, 0, JudgementY + RunwayHeight / 2,
                LaneWidth * 7, RunwayHeight);
            noteLayer.gameObject.AddComponent<RectMask2D>();
            for (int id = 0; id < Pitches.Length; id++)
            {
                Color tint = FromHex(Colors[id]);
                float x = (id - 3) * LaneWidth;
                var track = Box(lanePitches[id] + " note track", noteLayer, x, 0, LaneWidth, RunwayHeight);
                var key = Panel(lanePitches[id] + " white key", frame, FromHex("FAFAF7"), x,
                    JudgementY - 129, LaneWidth - 3, 252);
                var keyRect = key.rectTransform;
                Panel("Ivory lip", keyRect,  FromHex("9FA6B3"), 0, -123, LaneWidth - 3, 6);
                Panel("Top edge", keyRect, WithAlpha(Ink, .15f), 0, 124.5f, LaneWidth - 3, 3);
                var hint = Panel("Lane status", keyRect, WithAlpha(tint, .32f), 0, -111, 90, 3);
                Label("Pitch", keyRect, lanePitches[id], 42, Ink, 0, -48, 132, 54, style: FontStyle.Bold);
                Label("Keyboard binding", keyRect, Keys[id].ToString(), 30, FromHex("212735"),
                    0, -87, 66, 36, style: FontStyle.Bold);
                game.lanes[id] = new PianoGame.Lane
                {
                    pitch = lanePitches[id], key = Keys[id], color = tint, keyFace = key, track = track,
                    notePrefab = BuildBlock(Pitches[id], tint), hintStrip = hint, keySurface = keyRect, laneLight = laneLights[id]
                };
            }

            foreach (int boundary in (imported ? Array.Empty<int>() : new[] { 1, 2, 4, 5, 6 }))
            {
                float x = (boundary - Pitches.Length / 2f) * LaneWidth;
                var black = Panel("Decorative black key " + boundary, frame, FromHex("080A10"), x,
                    JudgementY - 75, 108, 150).rectTransform;
                Panel("Black key face", black, FromHex("191E29"), 0, 3, 96, 132);
                Panel("Black key bottom edge", black, FromHex("424C60"), 0, -66, 96, 6);
            }

            Surface("Judgement halo", frame, 0, JudgementY + 5, 1596, 10,
                WithAlpha(Gold, .13f), WithAlpha(Gold, 0));
            Panel("Judgement line", frame, Gold, 0, JudgementY, 1596, 4);
            game.effectsLayer = Box("Hit effects", frame, 0, JudgementY, 1, 1);
            game.hitFlash = Light("Perfect glow", game.effectsLayer, 0, 0, 154, 66, Color.clear);
            game.hitRing = Box("Hit ring", game.effectsLayer, 0, 0, 138, 32).gameObject.AddComponent<HitRing>();
            game.hitRing.color = Color.clear;
            game.hitRing.raycastTarget = false;
            game.sparkPrefab = BuildSpark();
            game.feedback = Label("Judgement feedback", frame, "", 32, Gold, -248, 416, 350, 46,
                style: FontStyle.Bold);

            var start = Overlay("Start screen", root);
            game.startPanel = start.parent.gameObject;
            Label("Song heading", start, game.song.title, 72, White, 0, 126, 780, 100, style: FontStyle.Bold);
            Label("Playing instructions", start, imported
                ? "Easy • Play the cues with A S D F G H J.\nA simplified piano accompaniment plays along."
                : "Press A S D F G H J as the piano keys light up.\nHit as soon as the key lights for Perfect.",
                28, Muted, 0, 17, 760, 100);
            game.startButton = ActionButton("Start", "PLAY", start, -112);
            UnityEventTools.AddPersistentListener(game.startButton.onClick, game.Begin);
            var results = Overlay("Results screen", root);
            game.resultsPanel = results.parent.gameObject;
            Label("Results heading", results, "SONG COMPLETE", 46, Gold, 0, 154, 780, 66, style: FontStyle.Bold);
            game.summary = Label("Results breakdown", results, "", 36, White, 0, 21, 780, 188);
            game.restartButton = ActionButton("Restart", "PLAY AGAIN", results, -151);
            UnityEventTools.AddPersistentListener(game.restartButton.onClick, game.Begin);
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/SomeoneLikeYouArrangementChart.json"))
            {
                AddSongLink(start, game, imported, -179);
                AddSongLink(results, game, imported, -201);
            }
            game.resultsPanel.SetActive(false);

            if (!EditorSceneManager.SaveScene(scene, pathToSave)) throw new IOException("Could not save " + pathToSave);
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(entry => entry.path == pathToSave))
            {
                scenes.Add(new EditorBuildSettingsScene(pathToSave, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Air Piano: generated " + pathToSave);
        }

        [Serializable]
        private sealed class ImportedChart
        {
            public SongNote[] notes;
            public int[] pitches;
            public float duration;
        }

        private static PianoSong LoadSomeoneLikeYou()
        {
            const string songPath = Root + "/Data/SomeoneLikeYouArrangement.asset";
            const string audioRoot = Root + "/Audio/SomeoneLikeYou/Arrangement/";
            var chartFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/SomeoneLikeYouArrangementChart.json");
            if (!chartFile) throw new InvalidOperationException("Missing Someone Like You chart JSON.");
            var chart = JsonUtility.FromJson<ImportedChart>(chartFile.text);
            var backing = AssetDatabase.LoadAssetAtPath<AudioClip>(audioRoot + "Backing.wav");
            if (!backing || Mathf.Abs(backing.length - chart.duration) > .01f)
                throw new InvalidOperationException("Missing backing track or unexpected duration.");
            var pitched = new PianoSong.PitchedClip[chart.pitches.Length];
            for (int i = 0; i < pitched.Length; i++)
            {
                int midi = chart.pitches[i];
                var sample = AssetDatabase.LoadAssetAtPath<AudioClip>(audioRoot + $"Pitch_{midi}.wav");
                if (!sample) throw new InvalidOperationException($"Missing piano clip for MIDI {midi}.");
                pitched[i] = new PianoSong.PitchedClip { midi = midi, clip = sample };
            }

            var song = AssetDatabase.LoadAssetAtPath<PianoSong>(songPath);
            if (!song)
            {
                song = ScriptableObject.CreateInstance<PianoSong>();
                song.title = "Someone Like You";
                AssetDatabase.CreateAsset(song, songPath);
            }
            song.notes = chart.notes;
            song.backing = backing;
            song.backingIncludesPlayableNotes = false;
            song.pitchedClips = pitched;
            EditorUtility.SetDirty(song);
            return song;
        }

        private static void AddSongLink(RectTransform card, PianoGame game, bool imported, int y)
        {
            var button = ActionButton("Switch song", imported ? "AFTERGLOW" : "SOMEONE LIKE YOU", card, y);
            button.GetComponent<Image>().color = FromHex("35466A");
            button.GetComponentInChildren<Text>().color = White;
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 37);
            button.GetComponentInChildren<Text>().fontSize = 20;
            UnityEventTools.AddStringPersistentListener(button.onClick, game.OpenSong, imported ? ScenePath : SomeoneLikeYouScene);
        }

        private static PianoSong BuildSong()
        {
            const string path = Root + "/Data/Afterglow.asset";
            var song = AssetDatabase.LoadAssetAtPath<PianoSong>(path);
            if (song) return song;
            song = ScriptableObject.CreateInstance<PianoSong>();
            int[] melody = { 0, 2, 4, 6, 5, 4, 3, 2, 1, 3, 2, 1, 0 };
            float[] lengths = { 1.8f, .7f, .54f, 1.35f, .92f, .72f, 1.6f, .68f, .9f, 1.15f, .72f, .64f, 2.3f };
            song.notes = new SongNote[melody.Length];
            for (var i = 0; i < melody.Length; i++) song.notes[i] = new SongNote(melody[i], i, lengths[i]);
            AssetDatabase.CreateAsset(song, path);
            return song;
        }

        private static RectTransform BuildBlock(string pitch, Color tint)
        {
            var path = $"{Root}/Prefabs/RollNote_{pitch}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return (RectTransform)existing.transform;
            var block = Box(pitch + " Note", null, 0, 0, LaneWidth * .64f, 94);
            block.pivot = new Vector2(.5f, 0);
            block.anchorMin = block.anchorMax = new Vector2(.5f, 0);
            var rim = Surface("Note outline", block, 0, 0, 0, 0,
                Color.Lerp(tint, Ink, .43f), Color.Lerp(tint, Ink, .27f), radius: 13);
            Stretch(rim.rectTransform, 0);
            var face = Surface("Solid note color", block, 0, 0, 0, 0,
                Color.Lerp(tint, White, .13f), tint, radius: 11);
            Stretch(face.rectTransform, 2);
            var label = Label("Pitch", block, pitch, 36, White, 0, 24, 126, 48, style: FontStyle.Bold);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 0);
            label.rectTransform.anchoredPosition = new Vector2(0, 25);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = FromHex("17191E");
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return SavePrefab(block, path);
        }

        private static RectTransform BuildSpark()
        {
            const string path = Root + "/Prefabs/HitSpark.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return (RectTransform)existing.transform;
            var spark = Panel("Hit spark", null, Gold, 0, 0, 7, 7).rectTransform;
            spark.localRotation = Quaternion.Euler(0, 0, 45);
            return SavePrefab(spark, path);
        }

        private static RectTransform SavePrefab(RectTransform rect, string path)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(rect.gameObject, path);
            UnityEngine.Object.DestroyImmediate(rect.gameObject);
            if (!saved) throw new IOException("Could not save " + path);
            return (RectTransform)saved.transform;
        }

        private static RectTransform Overlay(string name, RectTransform root)
        {
            var veil = Fill(name, root, WithAlpha(Ink, .78f));
            veil.raycastTarget = true;
            Panel("Card rim", veil.rectTransform, FromHex("394C70"), 0, 35, 870, 466);
            var card = Panel("Card", veil.rectTransform, FromHex("141F38"), 0, 35, 866, 462).rectTransform;
            Panel("Gold accent", card, Gold, 0, 229, 866, 5);
            return card;
        }

        private static Button ActionButton(string name, string caption, RectTransform parent, int y)
        {
            var face = Panel(name, parent, Gold, 0, y, 270, 66);
            face.raycastTarget = true;
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            Label("Caption", face.rectTransform, caption, 25, Ink, 0, 0, 265, 60, style: FontStyle.Bold);
            return button;
        }

        private static RectTransform Box(string name, RectTransform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        private static Image Panel(string name, RectTransform parent, Color color, float x, float y, float w, float h)
        {
            var image = Box(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image Fill(string name, RectTransform parent, Color color)
        {
            var image = Panel(name, parent, color, 0, 0, 0, 0);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            return image;
        }

        private static Text Label(string name, RectTransform parent, string content, int size, Color color,
            float x, float y, float w, float h, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var label = Box(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.text = content;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            return label;
        }

        private static StageSurface Surface(string name, RectTransform parent, float x, float y,
            float width, float height, Color bottom, Color top, float taper = 1, float offset = 0, float radius = 0)
        {
            var surface = Box(name, parent, x, y, width, height).gameObject.AddComponent<StageSurface>();
            surface.color = bottom;
            surface.topColor = top;
            surface.topWidth = taper;
            surface.topOffset = offset;
            surface.cornerRadius = radius;
            surface.raycastTarget = false;
            return surface;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = -Vector2.one * inset;
        }

        private static StageLight Light(string name, RectTransform parent, float x, float y,
            float width, float height, Color color)
        {
            var light = Box(name, parent, x, y, width, height).gameObject.AddComponent<StageLight>();
            light.color = color;
            light.raycastTarget = false;
            return light;
        }

        private static bool CanUpgradeSomeoneLikeYou()
        {
            if (!Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-airPianoUpgradeRecording"))
                return false;
            using (var sha = SHA256.Create())
            {
                var hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(SomeoneLikeYouScene))).Replace("-", "");
                return hash == "9F1D5C35904E98A3C4C5A0CBB06A48E50D18D774FB015709600D6A80BC38747B";
            }
        }

        private static bool CanUpgradeOriginal()
        {
            if (!Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-airPianoUpgradeOriginal"))
                return false;
            using (var sha = SHA256.Create())
            {
                var hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(ScenePath))).Replace("-", "");
                return hash == "93311A3F8FB23F05B88E9CB176CCA8D82960EC713E0B7B9F904B8D07CC07800B";
            }
        }

        private static Color FromHex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}


