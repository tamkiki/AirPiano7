using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AirPiano7.Editor
{
    public static class PolishSomeoneLikeYou
    {
        public const string ScenePath = BuildSevenNoteDemo.Root + "/Scenes/SomeoneLikeYouPolished.unity";
        private const string PrefabFolder = BuildSevenNoteDemo.Root + "/Prefabs/SomeoneLikeYouPolished";
        private static readonly Color Ivory = Hex("FFF7E8");
        private static readonly Color Gold = Hex("F5CC83");
        private static readonly Color Muted = Hex("A9B5C9");
        private static readonly Color Ink = Hex("161A23");
        private static readonly string[] LaneColors =
            { "68D9C4", "78BCF5", "F6CB87", "F69D92", "A5E0AE", "BCADFB", "92D4ED" };

        [MenuItem("Air Piano/Create Polished Song Scene")]
        public static void Run()
        {
            if (File.Exists(ScenePath))
            {
                Debug.Log("The polished song scene already exists. Open it to edit the layout.");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (!AssetDatabase.CopyAsset(BuildSevenNoteDemo.SomeoneLikeYouScene, ScenePath))
                throw new IOException("Could not copy the original song scene.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = Object.FindObjectOfType<PianoGame>();
            if (!game || !game.song.backing)
                throw new InvalidOperationException("The source scene is missing its piano controller or accompaniment.");

            var frame = (RectTransform)game.title.transform.parent;
            var canvas = (RectTransform)frame.parent;
            canvas.Find("Background").GetComponent<Image>().color = Hex("0A0D16");
            var wash = canvas.Find("Stage color wash").GetComponent<StageSurface>();
            wash.color = Hex("101626");
            wash.topColor = Hex("090C14");

            Panel("Stage chassis edge", frame, 0, -71, 1632, 876, Hex("384762"), 19).SetAsFirstSibling();
            Panel("Stage chassis", frame, 0, -71, 1628, 872, Hex("0C1120"), 17).SetSiblingIndex(1);
            Panel("Counter panel", frame, 397, 426, 834, 116, Hex("34415A"), 16).SetSiblingIndex(2);
            Panel("Counter panel inset", frame, 397, 426, 830, 112, Hex("111929"), 14).SetSiblingIndex(3);
            Panel("Score divider", frame, 232, 430, 2, 62, Hex("303D56"), 0);
            Panel("Combo divider", frame, 476, 430, 2, 62, Hex("303D56"), 0);
            var accent = frame.Find("Title accent").GetComponent<StageSurface>();
            accent.color = Gold;
            accent.topColor = Hex("FFE8B2");
            accent.rectTransform.anchoredPosition = new Vector2(-814, 424);
            var wordmark = frame.Find("Wordmark").GetComponent<Text>();
            Place(wordmark, -637, 459, 304, 32, 20, Gold);
            Place(game.title, -550, 411, 480, 64, 48, Ivory);
            game.title.fontStyle = FontStyle.Bold;
            Place(game.feedback, -153, 423, 258, 42, 26, Gold);
            foreach (var label in new[] { "Score label", "Combo label", "Progress label" })
            {
                var text = frame.Find(label).GetComponent<Text>();
                text.color = Muted;
                text.fontSize = 17;
            }
            game.score.color = Ivory;
            game.combo.color = Ivory;
            game.progress.color = Ivory;
            game.progressFill.GetComponent<Image>().color = Gold;
            game.progressFill.parent.GetComponent<Image>().color = Hex("34415A");

            var roll = (RectTransform)frame.Find("Perspective runway");
            roll.name = "Piano roll";
            roll.Find("Roll background").GetComponent<Image>().color = Hex("101625");
            foreach (Transform child in roll)
            {
                if (child.name.StartsWith("Vertical key guide", StringComparison.Ordinal))
                    child.GetComponent<Image>().color = Hex("313B51");
                if (child.name.StartsWith("Horizontal guide", StringComparison.Ordinal))
                    child.GetComponent<Image>().color = Hex("252E42");
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder(BuildSevenNoteDemo.Root + "/Prefabs", "SomeoneLikeYouPolished");
            for (var i = 0; i < game.lanes.Length; i++)
            {
                var lane = game.lanes[i];
                lane.color = Hex(LaneColors[i]);
                var field = Panel(lane.pitch + " lane foundation", roll,
                    lane.track.anchoredPosition.x, 0, 224, 585,
                    Hex(i % 2 == 0 ? "141D30" : "111929"), 0);
                field.SetSiblingIndex(i + 1);
                lane.keyFace.color = Hex(i % 2 == 0 ? "FFF6E6" : "F2EFE9");
                lane.keySurface.Find("Ivory lip").GetComponent<Image>().color = Hex("969CA8");
                lane.keySurface.Find("Top edge").GetComponent<Image>().color = lane.color;
                lane.hintStrip.color = lane.color;
                lane.keySurface.Find("Pitch").GetComponent<Text>().color = Ink;
                var pitch = lane.keySurface.Find("Pitch").GetComponent<Text>();
                Place(pitch, 0, -23, 132, 58, 45, Ink);
                var shortcut = lane.keySurface.Find("Keyboard binding").GetComponent<Text>();
                var keycap = Panel("Keyboard shortcut chip", lane.keySurface, 0, -80, 63, 41, Hex("1C273B"), 8);
                keycap.SetSiblingIndex(shortcut.rectTransform.GetSiblingIndex());
                Place(shortcut, 0, -80, 63, 36, 26, Ivory);
                lane.notePrefab = StyleNotePrefab(lane.notePrefab, i, lane.color);
            }

            frame.Find("Judgement line").GetComponent<Image>().color = Gold;
            StyleOverlay(game.startPanel);
            StyleOverlay(game.resultsPanel);
            var instructions = game.startPanel.transform.Find("Card/Playing instructions").GetComponent<Text>();
            instructions.text = "Play one key at a time · A  S  D  F  G  H  J\n27 cues over a soft piano accompaniment";
            instructions.color = Muted;
            InstallGarden(game);

            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the polished scene.");
            if (!EditorBuildSettings.scenes.Any(item => item.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(
                    new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("AIR PIANO POLISH: saved " + ScenePath);
        }

        [MenuItem("Air Piano/Add Flower Garden to Polished Scene")]
        public static void AddFlowerGarden()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = Object.FindObjectOfType<PianoGame>();
            if (!game) throw new InvalidOperationException("The polished scene has no piano controller.");
            if (Object.FindObjectOfType<BloomGarden>()) return;
            InstallGarden(game);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the flower garden.");
            Debug.Log("AIR PIANO GARDEN: saved " + ScenePath);
        }

        private static void InstallGarden(PianoGame game)
        {
            var frame = (RectTransform)game.title.transform.parent;
            var canvas = (RectTransform)frame.parent;
            frame.localScale = new Vector3(.88f, 1, 1);
            var wash = canvas.Find("Stage color wash").GetComponent<StageSurface>();
            wash.color = Hex("160F26");
            wash.topColor = Hex("0D1222");
            foreach (var overlay in new[] { game.startPanel, game.resultsPanel })
            {
                var image = overlay.GetComponent<Image>();
                var veil = image.color;
                veil.a = .76f;
                image.color = veil;
            }
            AddGardenLight(canvas, -815, Hex("834F71"));
            AddGardenLight(canvas, 815, Hex("50508C"));

            var rect = new GameObject("Breathing flower garden", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(canvas, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(frame.GetSiblingIndex());
            var garden = rect.gameObject.AddComponent<BloomGarden>();
            garden.game = game;
            garden.raycastTarget = false;
            InstallFloatingFlowers(game);
            SharpenKeyboard(game);
            EnsureDisplayCamera(game);
        }

        [MenuItem("Air Piano/Add Display Camera to Polished Scene")]
        public static void AddDisplayCamera()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = Object.FindObjectOfType<PianoGame>();
            if (!game) throw new InvalidOperationException("The polished scene has no piano controller.");
            EnsureDisplayCamera(game);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the display camera.");
            Debug.Log("AIR PIANO POLISH: saved the Display 1 camera in " + ScenePath);
        }

        private static void EnsureDisplayCamera(PianoGame game)
        {
            var existing = game.transform.Find("Display Camera");
            var camera = existing ? existing.GetComponent<Camera>() : null;
            if (!camera)
            {
                var display = new GameObject("Display Camera", typeof(Camera));
                display.transform.SetParent(game.transform, false);
                display.transform.localPosition = new Vector3(0, 0, -10);
                camera = display.GetComponent<Camera>();
            }
            camera.enabled = true;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("0A0D16");
            camera.cullingMask = 0;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.targetDisplay = 0;
            camera.targetTexture = null;
        }

        [MenuItem("Air Piano/Sharpen Polished Keyboard")]
        public static void SharpenPolishedKeyboard()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = Object.FindObjectOfType<PianoGame>();
            if (!game) throw new InvalidOperationException("The polished scene has no piano controller.");
            SharpenKeyboard(game);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the sharpened keyboard.");
            Debug.Log("AIR PIANO POLISH: saved pixel-aligned keyboard in " + ScenePath);
        }

        private static void SharpenKeyboard(PianoGame game)
        {
            var frame = game.title.transform.parent;
            float stageWidthScale = Mathf.Abs(frame.localScale.x);
            game.score.canvas.pixelPerfect = true;
            foreach (var lane in game.lanes)
            {
                var key = lane.keySurface;
                // Keep the key aligned with the narrowed track while drawing its labels at native proportions.
                key.localScale = new Vector3(1 / stageWidthScale, 1, 1);
                float width = Mathf.Round(lane.track.rect.width * stageWidthScale) - 3;
                key.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                foreach (var edge in new[] { "Ivory lip", "Top edge" })
                    ((RectTransform)key.Find(edge)).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                var pitch = key.Find("Pitch").GetComponent<Text>();
                Place(pitch, 0, -24, 132, 60, 48, Ink);
                var shortcut = key.Find("Keyboard binding").GetComponent<Text>();
                Place(shortcut, 0, -81, 66, 42, 30, Ivory);
                var chip = (RectTransform)key.Find("Keyboard shortcut chip");
                chip.anchoredPosition = new Vector2(0, -81);
                chip.sizeDelta = new Vector2(66, 45);
                chip.GetComponent<StageSurface>().cornerRadius = 6;
            }
        }

        [MenuItem("Air Piano/Add Floating Flowers to Polished Scene")]
        public static void AddFloatingFlowers()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = Object.FindObjectOfType<PianoGame>();
            if (!game) throw new InvalidOperationException("The polished scene has no piano controller.");
            InstallFloatingFlowers(game);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the floating flowers.");
            Debug.Log("AIR PIANO GARDEN: saved floating flowers in " + ScenePath);
        }

        private static void InstallFloatingFlowers(PianoGame game)
        {
            var roll = game.title.transform.parent.Find("Piano roll");
            if (roll.Find("Distant floating flowers")) return;
            var rect = new GameObject("Distant floating flowers", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(roll, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(roll.Find("Vertical key guide 0").GetSiblingIndex());
            var garden = rect.gameObject.AddComponent<BloomGarden>();
            garden.game = game;
            garden.insideRoll = true;
            garden.raycastTarget = false;
        }

        private static void AddGardenLight(RectTransform canvas, float x, Color tint)
        {
            var rect = new GameObject("Garden ambience", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(820, 1120);
            rect.anchoredPosition = new Vector2(x, -135);
            rect.SetSiblingIndex(canvas.Find("Stage").GetSiblingIndex());
            var light = rect.gameObject.AddComponent<StageLight>();
            tint.a = .34f;
            light.color = tint;
            light.raycastTarget = false;
        }

        private static RectTransform StyleNotePrefab(RectTransform source, int index, Color tint)
        {
            var path = $"{PrefabFolder}/Note_{index + 1}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return (RectTransform)existing.transform;
            var note = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));
            try
            {
                var outline = note.transform.Find("Note outline").GetComponent<StageSurface>();
                outline.color = Color.Lerp(tint, Ink, .56f);
                outline.topColor = Color.Lerp(tint, Color.white, .33f);
                var face = note.transform.Find("Solid note color").GetComponent<StageSurface>();
                face.color = tint;
                face.topColor = Color.Lerp(tint, Ivory, .35f);
                var label = note.GetComponentInChildren<Text>();
                label.color = Ink;
                label.GetComponent<Outline>().enabled = false;
                var saved = PrefabUtility.SaveAsPrefabAsset(note, path);
                if (!saved) throw new IOException("Cannot save " + path);
                return (RectTransform)saved.transform;
            }
            finally { PrefabUtility.UnloadPrefabContents(note); }
        }

        private static void StyleOverlay(GameObject overlay)
        {
            var surface = overlay.GetComponent<Image>();
            surface.color = new Color(.03f, .04f, .08f, .86f);
            overlay.transform.Find("Card rim").GetComponent<Image>().color = Hex("52617E");
            var card = overlay.transform.Find("Card");
            card.GetComponent<Image>().color = Hex("141D30");
            card.Find("Gold accent").GetComponent<Image>().color = Gold;
            foreach (var button in card.GetComponentsInChildren<Button>(true))
            {
                bool primary = button.name != "Switch song";
                button.GetComponent<Image>().color = primary ? Gold : Hex("2C3B58");
                var label = button.GetComponentInChildren<Text>(true);
                label.color = primary ? Ink : Ivory;
            }
        }

        private static void Place(Text text, float x, float y, float width, float height, int size, Color tint)
        {
            text.rectTransform.anchoredPosition = new Vector2(x, y);
            text.rectTransform.sizeDelta = new Vector2(width, height);
            text.fontSize = size;
            text.color = tint;
        }

        private static RectTransform Panel(string name, RectTransform parent, float x, float y,
            float width, float height, Color color, float radius)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            var surface = rect.gameObject.AddComponent<StageSurface>();
            surface.color = surface.topColor = color;
            surface.cornerRadius = radius;
            surface.raycastTarget = false;
            return rect;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }
    }
}
