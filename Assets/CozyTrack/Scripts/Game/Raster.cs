using UnityEngine;

namespace CozyTrack
{
    // Lienzo de píxeles mínimo para generar texturas por código (calzada, superficies, suelos, pegatinas, UI).
    // Coordenadas con y hacia abajo, como un canvas 2D; se voltea al crear la textura.
    public class Raster
    {
        public readonly int W, H; readonly Color[] px; uint seed;
        public Raster(int w, int h, uint seed = 17) { W = w; H = h; px = new Color[w * h]; this.seed = seed; }
        public float Rnd() { seed = seed * 1664525u + 1013904223u; return (seed >> 8) / 16777216f; }

        void Blend(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int k = (H - 1 - y) * W + x; var d = px[k]; float al = c.a * a;
            float oa = al + d.a * (1 - al);
            if (oa <= 0) { px[k] = new Color(0, 0, 0, 0); return; }
            px[k] = new Color((c.r * al + d.r * d.a * (1 - al)) / oa, (c.g * al + d.g * d.a * (1 - al)) / oa, (c.b * al + d.b * d.a * (1 - al)) / oa, oa);
        }
        public void Fill(Color c) { for (int i = 0; i < px.Length; i++) px[i] = c; }
        public void Rect(float x, float y, float w, float h, Color c)
        {
            for (int j = Mathf.FloorToInt(y); j < Mathf.CeilToInt(y + h); j++) for (int i = Mathf.FloorToInt(x); i < Mathf.CeilToInt(x + w); i++) Blend(Wrap(i, W), Wrap(j, H), c, 1);
        }
        static int Wrap(int v, int m) => ((v % m) + m) % m;
        // forma genérica por función de distancia con borde suavizado de 1 px
        public void Shape(float x0, float y0, float x1, float y1, System.Func<float, float, float> sdf, Color c, bool wrap = false)
        {
            for (int j = Mathf.FloorToInt(y0) - 1; j <= Mathf.CeilToInt(y1) + 1; j++)
                for (int i = Mathf.FloorToInt(x0) - 1; i <= Mathf.CeilToInt(x1) + 1; i++)
                {
                    float d = sdf(i + 0.5f, j + 0.5f);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0) continue;
                    if (wrap) Blend(Wrap(i, W), Wrap(j, H), c, a); else Blend(i, j, c, a);
                }
        }
        public void Circle(float cx, float cy, float r, Color c, bool wrap = false) => Shape(cx - r, cy - r, cx + r, cy + r, (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c, wrap);
        public void Ring(float cx, float cy, float r, float w, Color c, bool wrap = false) => Shape(cx - r - w, cy - r - w, cx + r + w, cy + r + w, (x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - w / 2, c, wrap);
        public void Ellipse(float cx, float cy, float rx, float ry, float rot, Color c, bool wrap = false)
        {
            float cs = Mathf.Cos(rot), sn = Mathf.Sin(rot), m = Mathf.Max(rx, ry);
            Shape(cx - m, cy - m, cx + m, cy + m, (x, y) =>
            {
                float dx = x - cx, dy = y - cy, u = (dx * cs + dy * sn) / rx, v = (-dx * sn + dy * cs) / ry;
                return (Mathf.Sqrt(u * u + v * v) - 1) * Mathf.Min(rx, ry);
            }, c, wrap);
        }
        public void Line(float x0, float y0, float x1, float y1, float w, Color c, bool wrap = false)
        {
            Shape(Mathf.Min(x0, x1) - w, Mathf.Min(y0, y1) - w, Mathf.Max(x0, x1) + w, Mathf.Max(y0, y1) + w, (x, y) =>
            {
                float vx = x1 - x0, vy = y1 - y0, t = Mathf.Clamp01(((x - x0) * vx + (y - y0) * vy) / Mathf.Max(1e-5f, vx * vx + vy * vy));
                float dx = x - (x0 + vx * t), dy = y - (y0 + vy * t); return Mathf.Sqrt(dx * dx + dy * dy) - w / 2;
            }, c, wrap);
        }
        public void Arc(float cx, float cy, float r, float w, float a0, float a1, Color c)
        {
            Shape(cx - r - w, cy - r - w, cx + r + w, cy + r + w, (x, y) =>
            {
                float a = Mathf.Atan2(y - cy, x - cx); if (a < 0) a += Mathf.PI * 2;
                bool inside = a0 <= a1 ? (a >= a0 && a <= a1) : (a >= a0 || a <= a1);
                return inside ? Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - w / 2 : 99;
            }, c);
        }
        public void Poly(Vector2[] p, Color c)
        {
            float x0 = 1e9f, y0 = 1e9f, x1 = -1e9f, y1 = -1e9f;
            foreach (var v in p) { x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y); }
            Shape(x0, y0, x1, y1, (x, y) =>
            {
                bool inside = false; float dmin = 1e9f;
                for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                {
                    if ((p[i].y > y) != (p[j].y > y) && x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x) inside = !inside;
                    float vx = p[i].x - p[j].x, vy = p[i].y - p[j].y, t = Mathf.Clamp01(((x - p[j].x) * vx + (y - p[j].y) * vy) / Mathf.Max(1e-5f, vx * vx + vy * vy));
                    float dx = x - (p[j].x + vx * t), dy = y - (p[j].y + vy * t); dmin = Mathf.Min(dmin, Mathf.Sqrt(dx * dx + dy * dy));
                }
                return inside ? -dmin : dmin;
            }, c);
        }
        public Texture2D ToTexture(bool repeat = true, FilterMode f = FilterMode.Bilinear)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, filterMode = f, anisoLevel = 4 };
            t.SetPixels(px); t.Apply(true, true); return t;
        }
        public Sprite ToSprite(float border)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            t.SetPixels(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
