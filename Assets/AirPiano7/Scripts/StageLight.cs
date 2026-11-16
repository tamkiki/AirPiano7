using UnityEngine;
using UnityEngine.UI;

namespace AirPiano7
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StageLight : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            const int segments = 48;
            const int bands = 7;
            mesh.Clear();
            var bounds = GetPixelAdjustedRect();
            var radii = bounds.size * .5f;
            for (int band = 0; band <= bands; band++)
            {
                float distance = (float)band / bands;
                var tint = color;
                tint.a *= Mathf.Pow(1 - distance, 2.4f);
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    var point = new Vector2(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y);
                    mesh.AddVert(bounds.center + point * distance, tint, Vector2.zero);
                    if (band == 0) continue;
                    int current = band * segments + i;
                    int next = band * segments + (i + 1) % segments;
                    mesh.AddTriangle(current - segments, next - segments, next);
                    mesh.AddTriangle(current - segments, next, current);
                }
            }
        }
    }
}
