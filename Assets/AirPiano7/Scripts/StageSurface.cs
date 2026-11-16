using UnityEngine;
using UnityEngine.UI;

namespace AirPiano7
{
    // A crisp trapezoid with vertex colors for the runway and beveled piano keys.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StageSurface : MaskableGraphic
    {
        [Range(.1f, 1f)] public float topWidth = 1;
        public float topOffset;
        public Color topColor = Color.white;
        [Min(0)] public float cornerRadius;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var r = GetPixelAdjustedRect();
            if (cornerRadius > 0)
            {
                Rounded(mesh, r);
                return;
            }
            float halfTop = r.width * topWidth * .5f;
            float center = r.center.x + topOffset;
            mesh.AddVert(new Vector3(r.xMin, r.yMin), color, Vector2.zero);
            mesh.AddVert(new Vector3(center - halfTop, r.yMax), topColor, Vector2.up);
            mesh.AddVert(new Vector3(center + halfTop, r.yMax), topColor, Vector2.one);
            mesh.AddVert(new Vector3(r.xMax, r.yMin), color, Vector2.right);
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(0, 2, 3);
        }

        private void Rounded(VertexHelper mesh, Rect r)
        {
            const int steps = 8;
            float radius = Mathf.Min(cornerRadius, Mathf.Min(r.width, r.height) * .5f);
            mesh.AddVert(r.center + Vector2.right * topOffset * .5f, Color.Lerp(color, topColor, .5f), Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                bool right = corner == 0 || corner == 3;
                bool top = corner < 2;
                var center = new Vector2(right ? r.xMax - radius : r.xMin + radius,
                    top ? r.yMax - radius : r.yMin + radius);
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (corner + (float)step / steps) * Mathf.PI * .5f;
                    var p = center + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float height = Mathf.InverseLerp(r.yMin, r.yMax, p.y);
                    p.x = r.center.x + (p.x - r.center.x) * Mathf.Lerp(1, topWidth, height) + topOffset * height;
                    mesh.AddVert(p, Color.Lerp(color, topColor, height), Vector2.zero);
                    int index = mesh.currentVertCount - 1;
                    if (index > 1) mesh.AddTriangle(0, index, index - 1);
                }
            }
            mesh.AddTriangle(0, 1, mesh.currentVertCount - 1);
        }
    }
}
