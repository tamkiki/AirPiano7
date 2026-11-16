using UnityEngine;
using UnityEngine.UI;

namespace AirPiano7
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HitRing : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            const int segments = 48;
            Vector2 radius = rectTransform.rect.size * .5f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * (2 * Mathf.PI / segments);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(Vector2.Scale(direction, radius), color, Vector2.zero);
                mesh.AddVert(Vector2.Scale(direction, radius - new Vector2(3, 2)), color, Vector2.zero);
                if (i == 0) continue;
                int index = i * 2;
                mesh.AddTriangle(index - 2, index, index + 1);
                mesh.AddTriangle(index - 2, index + 1, index - 1);
            }
        }
    }
}
