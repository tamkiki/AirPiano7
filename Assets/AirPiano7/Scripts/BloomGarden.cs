using UnityEngine;
using UnityEngine.UI;

namespace AirPiano7
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BloomGarden : MaskableGraphic
    {
        public PianoGame game;
        public bool insideRoll;
        public int HitBlooms { get; private set; }
        public float BeatPulse { get; private set; }
        private static readonly Color[] Petals =
        {
            new Color(1f,.59f,.72f), new Color(.76f,.65f,.98f),
            new Color(1f,.81f,.57f), new Color(.57f,.82f,1f),
            new Color(1f,.68f,.67f)
        };
        private static readonly Color Pollen = new Color(1, .83f, .55f, .95f);

        private struct Flower
        {
            public Vector2 position;
            public float size;
            public float phase;
            public int form;
            public int palette;
            public int side;
            public bool near;
        }

        private Flower[] flowers;
        private double[] hitTimes;
        private double lastHit = -100;
        private int lastSide;
        private bool playing;
        private float phase;
        private float closing;
        private int nextCue;
        private float lastCue = -100;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.isPlaying && game)
            {
                game.SongStarted += Started;
                game.NoteJudged += Judged;
            }
        }

        protected override void OnDisable()
        {
            if (Application.isPlaying && game)
            {
                game.SongStarted -= Started;
                game.NoteJudged -= Judged;
            }
            base.OnDisable();
        }

        private void Started()
        {
            EnsureFlowers();
            for (int i = 0; i < hitTimes.Length; i++) hitTimes[i] = -100;
            lastHit = -100;
            HitBlooms = 0;
            BeatPulse = 0;
            nextCue = 0;
            lastCue = -100;
            closing = 0;
            playing = true;
        }

        private void Judged(int lane, Judgement judgement)
        {
            if (judgement == Judgement.Miss) return;
            EnsureFlowers();
            lastHit = game.SongTime;
            lastSide = HitBlooms % 2;
            int first = (HitBlooms / 2 + lane) % 6;
            int bloomCount = judgement == Judgement.Perfect ? 5 : 3;
            for (int i = 0; i < bloomCount; i++)
                hitTimes[lastSide * 6 + (first + i) % 6] = lastHit + i * .038;
            HitBlooms++;
        }

        private void Update()
        {
            phase = playing ? (float)game.SongTime : Time.unscaledTime;
            if (playing && game.song.backing)
            {
                while (nextCue < game.song.notes.Length)
                {
                    float beat = game.fallDuration + game.song.notes[nextCue].spawnBeat * game.noteInterval - .34f;
                    if (phase < beat) break;
                    lastCue = beat;
                    nextCue++;
                }
                BeatPulse = Pulse(phase - lastCue, .09f, .95f);
                float end = game.fallDuration + game.song.backing.length;
                closing = Mathf.SmoothStep(0, .86f, Mathf.InverseLerp(end - 1.2f, end + .5f, phase));
            }
            SetVerticesDirty();
        }

        private void EnsureFlowers()
        {
            if (flowers != null) return;
            flowers = new Flower[insideRoll ? 20 : 39];
            hitTimes = new double[flowers.Length];
            var random = new System.Random(137);
            var positions = new[]
            {
                new Vector2(.045f,.20f), new Vector2(.094f,.11f), new Vector2(.056f,.41f),
                new Vector2(.094f,.55f), new Vector2(.054f,.74f), new Vector2(.100f,.89f)
            };

            for (int i = 0; i < flowers.Length; i++)
            {
                int side = i < 12 ? i / 6 : i % 2;
                bool near = i < 12;
                var position = near ? positions[i % 6] :
                    new Vector2(.018f + (float)random.NextDouble() * .102f,
                        .045f + (float)random.NextDouble() * .87f);
                if (side == 1)
                {
                    position.x = 1 - position.x;
                    if (near) position.y = Mathf.Clamp01(position.y + ((i % 2) == 0 ? .07f : -.03f));
                }
                if (insideRoll)
                    position = new Vector2(.05f + .90f * Mathf.Repeat(i * .38197f + .13f, 1),
                        .08f + .79f * Mathf.Repeat(i * .75488f + .22f, 1));
                flowers[i] = new Flower
                {
                    position = position,
                    size = insideRoll ? 12 + i % 6 * 4 : near ? (i % 6 == 0 ? 71 : 41 + 23 * (float)random.NextDouble())
                        : 5 + 17 * (float)random.NextDouble(),
                    phase = (float)random.NextDouble() * 6.28f,
                    form = (i + i / 6) % 3,
                    palette = (i + i / 6) % Petals.Length,
                    side = side,
                    near = near
                };
                hitTimes[i] = -100;
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            EnsureFlowers();
            mesh.Clear();
            var r = rectTransform.rect;
            float scale = Mathf.Min(r.width / 1920, r.height / 1080);
            if (scale <= 0) return;

            for (int i = 0; !insideRoll && i < 32; i++)
            {
                float u = Mathf.Repeat(i * .14193f, .126f);
                if (i % 2 == 0) u = 1 - u;
                var point = Point(r, new Vector2(u, Mathf.Repeat(i * .31391f, .94f)));
                float alpha = .14f + .035f * Mathf.Sin(phase * .52f + i);
                Disc(mesh, point, scale * (1.1f + i % 4), new Color(.83f, .72f, .95f, alpha));
            }

            for (int i = flowers.Length - 1; i >= 0; i--)
            {
                var flower = flowers[i];
                var anchor = Point(r, flower.position);
                float sway = Mathf.Sin(phase * .84f + flower.phase);
                var head = anchor + new Vector2((flower.near ? 22 : 13) * sway,
                    (flower.near ? 30 : 17) * Mathf.Sin(phase * .43f + flower.phase) +
                    (flower.near ? 24 : 6) * BeatPulse) * scale;
                float breath = .19f + .07f * Mathf.Sin(phase * 1.87f + flower.phase * .55f);
                float age = phase - (float)hitTimes[i];
                const float flourish = 1.95f;
                float flash = Pulse(age, .22f, flourish);
                float response = Mathf.Max(BeatPulse * (flower.near ? .90f : .45f), flash);
                float opening = Mathf.Lerp(Mathf.Lerp(breath, 1, response), .08f, closing);
                float direction = i % 2 == 0 ? 1 : -1;
                float spin = direction * (phase * .18f + BeatPulse * .85f);
                if (flower.near && !insideRoll)
                {
                    float turn = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / flourish));
                    spin += direction * turn * 2 * Mathf.PI;
                }
                else
                {
                    spin *= .45f;
                }
                float alpha = insideRoll ? .13f + response * .10f : flower.near ? .96f : .50f;
                var glow = Petals[flower.palette];
                glow.a = (flower.near ? .15f : .07f) + response * (flower.near ? .34f : .12f);
                if (insideRoll) glow.a *= .32f;
                Halo(mesh, head, flower.size * scale * (1.46f + .65f * response), glow);
                Blossom(mesh, head, flower, flower.size * scale, opening, alpha, spin);
                if (!insideRoll && flower.near && flash > .34f)
                {
                    var glint = new Color(1, .90f, .68f, flash * .65f);
                    var corner = head + new Vector2(-.60f, .72f) * flower.size * scale;
                    Gleam(mesh, corner, 4.2f * scale, glint);
                }
            }

            float driftAge = phase - (float)lastHit;
            DrawFlyingFlowers(mesh, r, scale, driftAge);
            if (!insideRoll && driftAge < 3 && driftAge >= 0)
            {
                float side = lastSide == 0 ? 1 : -1;
                for (int i = 0; i < 9; i++)
                {
                    float x = (lastSide == 0 ? .067f : .933f) + side * .009f * i * driftAge;
                    if (x > .127f && x < .873f) continue;
                    float y = .26f + .024f * i + .085f * driftAge - .012f * driftAge * driftAge;
                    var tint = Petals[i % Petals.Length];
                    tint.a = .40f * (1 - driftAge / 3);
                    Petal(mesh, Point(r, new Vector2(x, y)), (5 + i % 3 * 2) * scale, 3 * scale,
                        i * .9f + driftAge * 1.8f, tint, tint);
                }
            }
        }

        private void DrawFlyingFlowers(VertexHelper mesh, Rect rect, float scale, float age)
        {
            if (age < 0 || age > 2.3f) return;
            float direction = lastSide == 0 ? 1 : -1;
            int count = insideRoll ? 4 : 3;
            for (int i = count - 1; i >= 0; i--)
            {
                float t = (age - i * .15f) / (insideRoll ? 1.75f : 1.9f);
                if (t <= 0 || t >= 1) continue;
                float eased = t * (2 - t);
                float visibility = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * 7)) *
                    (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.64f, 1, t)));
                float x = insideRoll ? -.055f + 1.11f * eased : .040f + .045f * Mathf.Sin(2.8f * t + i * 1.4f);
                if (direction < 0) x = 1 - x;
                float y = insideRoll ? .21f + .11f * i + .30f * Mathf.Sin(t * Mathf.PI)
                    : .16f + .105f * i + .58f * eased;
                var center = Point(rect, new Vector2(x, y));
                float size = (insideRoll ? 33 - 3 * i : 39 - 6 * i) * scale;
                int palette = (HitBlooms + i) % Petals.Length;
                float opening = Mathf.Lerp(.24f, 1, Mathf.Sin(Mathf.PI * Mathf.Clamp01(t + .13f)));
                var glow = Petals[palette];
                glow.a = visibility * (insideRoll ? .16f : .42f);
                Halo(mesh, center, size * 2.35f, glow);
                var flower = new Flower { palette = palette, form = i % 3, phase = i * 1.2f };
                Blossom(mesh, center, flower, size, opening,
                    visibility * (insideRoll ? .38f : .94f), direction * eased * Mathf.PI * 2);
                var flash = new Color(1, .91f, .73f, visibility * (1 - t) * (insideRoll ? .33f : .86f));
                Gleam(mesh, center + new Vector2(-direction * size, size * .72f), 7 * scale, flash);
            }
        }

        private static float Pulse(float age, float attack, float release)
        {
            if (age < 0 || age >= release) return 0;
            if (age < attack) return Mathf.Lerp(.48f, 1, Mathf.SmoothStep(0, 1, age / attack));
            return 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(attack, release, age));
        }

        private static void Blossom(VertexHelper mesh, Vector2 center, Flower flower, float size,
            float open, float opacity, float rotation)
        {
            var tint = Petals[flower.palette];
            tint.a = opacity;
            var pearl = new Color(1, .95f, .90f, opacity);
            var deep = Color.Lerp(tint, new Color(.34f, .15f, .34f, opacity), .40f);
            int tiers = flower.near ? (flower.form == 2 ? 3 : 2) : 1;
            for (int tier = 0; tier < tiers; tier++)
            {
                int count = flower.form == 2 ? 8 - tier : flower.form == 1 ? 6 : 5;
                float layer = 1 - .26f * tier;
                var shadow = Color.Lerp(deep, tint, tier * .11f);
                var light = Color.Lerp(tint, pearl, .34f + tier * .20f);
                for (int i = 0; i < count; i++)
                {
                    float curl = tier * (1 - open) * .40f;
                    float theta = flower.phase + rotation + curl + (i + tier * .47f) * Mathf.PI * 2 / count;
                    float length = size * Mathf.Lerp(.21f, 1.14f, open) * layer
                        * (1 + .075f * Mathf.Sin(i * 4.81f + flower.phase));
                    float width = size * (.062f + open * .24f) * layer *
                        (flower.form == 1 ? .63f : flower.form == 2 ? .86f : 1.12f);
                    if (flower.near)
                        SilkPetal(mesh, center, length, width, theta, flower.form,
                            i % 2 == 0 ? 1 : -1, shadow, tint, light);
                    else
                        Petal(mesh, center, length, width, theta, shadow, light);
                }
            }
            var pollen = Pollen;
            pollen.a *= opacity;
            Disc(mesh, center, size * (.054f + .035f * open), Color.Lerp(deep, pollen, .50f));
            Disc(mesh, center, size * .042f, pollen);
            if (!flower.near || size < 20) return;
            for (int i = 0; i < 9; i++)
            {
                float angle = i * 2 * Mathf.PI / 9 + flower.phase + rotation;
                var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var end = center + axis * (size * (.08f + .10f * open) * (1 + .16f * Mathf.Sin(i * 8)));
                Segment(mesh, center + axis * size * .045f, end, Mathf.Max(.43f, size * .010f), pollen);
                Disc(mesh, end, size * .023f, new Color(1, .90f, .69f, opacity));
            }
        }

        private static void SilkPetal(VertexHelper mesh, Vector2 center, float length, float width,
            float angle, int form, float bend, Color shadow, Color tint, Color light)
        {
            var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var normal = new Vector2(-axis.y, axis.x);
            const int rows = 8;
            const int columns = 4;
            int start = mesh.currentVertCount;
            for (int row = 0; row <= rows; row++)
            {
                float u = (float)row / rows;
                float shape = Mathf.Pow(Mathf.Max(0, Mathf.Sin(Mathf.PI * u)), form == 1 ? .84f : .47f);
                shape *= .48f + .70f * u;
                for (int col = 0; col <= columns; col++)
                {
                    float v = 2f * col / columns - 1;
                    float curl = bend * u * u * width * (form == 1 ? .43f : .16f);
                    float notch = form == 0 ? .05f * Mathf.Pow(u, 6) * (1 - Mathf.Abs(v)) : 0;
                    var point = center + axis * ((u - notch) * length) + normal * (v * shape * width + curl);
                    float ridge = Mathf.Max(0, 1 - Mathf.Abs(v + bend * .24f));
                    var body = Color.Lerp(shadow, tint, Mathf.SmoothStep(0, 1, u));
                    var color = Color.Lerp(body, light, ridge * .43f + u * .25f);
                    if (col == 0 || col == columns) color = Color.Lerp(color, light, .24f + .22f * u);
                    mesh.AddVert(point, color, Vector2.zero);
                    if (row == 0 || col == 0) continue;
                    int i = start + row * (columns + 1) + col;
                    int previous = i - columns - 1;
                    mesh.AddTriangle(previous - 1, i - 1, i);
                    mesh.AddTriangle(previous - 1, i, previous);
                }
            }
        }

        private static void Segment(VertexHelper mesh, Vector2 from, Vector2 to, float width, Color color)
        {
            var edge = (to - from).normalized;
            var normal = new Vector2(-edge.y, edge.x) * width;
            int i = mesh.currentVertCount;
            mesh.AddVert(from - normal, color, Vector2.zero);
            mesh.AddVert(to - normal, color, Vector2.zero);
            mesh.AddVert(to + normal, color, Vector2.zero);
            mesh.AddVert(from + normal, color, Vector2.zero);
            mesh.AddTriangle(i, i + 1, i + 2);
            mesh.AddTriangle(i, i + 2, i + 3);
        }

        private static void Gleam(VertexHelper mesh, Vector2 at, float size, Color color)
        {
            Segment(mesh, at - Vector2.up * size, at + Vector2.up * size, size * .12f, color);
            Segment(mesh, at - Vector2.right * size * .7f, at + Vector2.right * size * .7f, size * .12f, color);
        }

        private static void Petal(VertexHelper mesh, Vector2 root, float length, float width, float angle,
            Color inside, Color edge, float outline = 0)
        {
            var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var normal = new Vector2(-axis.y, axis.x);
            if (outline > 0)
            {
                var ink = Color.Lerp(inside, new Color(.35f, .18f, .32f, inside.a), .42f);
                PetalFace(mesh, root, length, width, axis, normal, ink, ink);
                root += axis * outline;
                length = Mathf.Max(1, length - 2 * outline);
                width = Mathf.Max(1, width - outline);
            }
            PetalFace(mesh, root, length, width, axis, normal, inside, edge);
        }

        private static void PetalFace(VertexHelper mesh, Vector2 root, float length, float width,
            Vector2 axis, Vector2 normal, Color inside, Color edge)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(root + axis * (length * .54f), Color.Lerp(inside, edge, .65f), Vector2.zero);
            const int steps = 20;
            for (int i = 0; i <= steps; i++)
            {
                float a = i * 2 * Mathf.PI / steps;
                float u = .5f - .5f * Mathf.Cos(a);
                float v = Mathf.Sin(a) * (.70f + .30f * u);
                mesh.AddVert(root + axis * (u * length) + normal * (v * width),
                    Color.Lerp(inside, edge, Mathf.SmoothStep(0, 1, u)), Vector2.zero);
                if (i > 0) mesh.AddTriangle(first, first + i, first + i + 1);
            }
        }

        private static void Halo(VertexHelper mesh, Vector2 center, float radius, Color tint)
        {
            const int sectors = 18;
            const int bands = 6;
            int start = mesh.currentVertCount;
            for (int band = 0; band <= bands; band++)
            {
                float t = (float)band / bands;
                var current = tint;
                current.a *= (1 - t) * (1 - t) * (1 - t);
                for (int i = 0; i < sectors; i++)
                {
                    float angle = i * 2 * Mathf.PI / sectors;
                    mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (t * radius),
                        current, Vector2.zero);
                    if (band == 0) continue;
                    int a = start + band * sectors + i;
                    int b = start + band * sectors + (i + 1) % sectors;
                    mesh.AddTriangle(a - sectors, b - sectors, b);
                    mesh.AddTriangle(a - sectors, b, a);
                }
            }
        }

        private static Vector2 Point(Rect rect, Vector2 p) =>
            new Vector2(rect.xMin + rect.width * p.x, rect.yMin + rect.height * p.y);

        private static void Disc(VertexHelper mesh, Vector2 center, float radius, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            const int count = 10;
            for (int i = 0; i <= count; i++)
            {
                float a = i * 2 * Mathf.PI / count;
                mesh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, tint, Vector2.zero);
                if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
            }
        }
    }
}
