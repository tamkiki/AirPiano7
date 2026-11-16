using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace AirPiano7.Editor
{
    public static class StageCapture
    {
        public static void Save(PianoGame game, string filePrefix = "AirPiano7", bool highResolution = false)
        {
            var canvas = game.score.canvas.rootCanvas;
            var rect = (RectTransform)canvas.transform;
            var oldMode = canvas.renderMode;
            var oldPosition = rect.position;
            var oldScale = rect.localScale;
            var oldSize = rect.sizeDelta;
            float oldScaleFactor = canvas.scaleFactor;
            var cameraObject = new GameObject("Verification camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 540;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = Vector3.back * 100;
            try
            {
                canvas.renderMode = RenderMode.WorldSpace;
                rect.position = Vector3.zero;
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(1920, 1080);
                if (highResolution)
                {
                    Capture(camera, canvas, 2560, 1440, filePrefix);
                    Capture(camera, canvas, 3840, 2160, filePrefix);
                    Debug.Log("AIR PIANO VERIFY: saved native 1440p and 4K gameplay captures");
                }
                else
                {
                    Capture(camera, canvas, 1920, 1080, filePrefix);
                    Capture(camera, canvas, 1280, 720, filePrefix);
                    Debug.Log("AIR PIANO VERIFY: saved gameplay captures at 1920x1080 and 1280x720");
                }
            }
            finally
            {
                canvas.renderMode = oldMode;
                rect.sizeDelta = oldSize;
                rect.position = oldPosition;
                rect.localScale = oldScale;
                canvas.scaleFactor = oldScaleFactor;
                Object.Destroy(cameraObject);
            }
        }

        private static void Capture(Camera camera, Canvas canvas, int width, int height, string filePrefix)
        {
            // Dynamic UI fonts rasterize for this render target instead of upscaling a 1080p glyph atlas.
            canvas.scaleFactor = height / 1080f;
            foreach (var label in canvas.GetComponentsInChildren<Text>())
                label.SetVerticesDirty();
            Canvas.ForceUpdateCanvases();
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes($"Logs/{filePrefix}-{width}x{height}.png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
            }
        }
    }
}
