using System.Collections.Generic;
using UnityEngine;

namespace CozyTrack
{
    // Texturas generadas por código (nada de imágenes externas).
    public static class CozyTex
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        static Color C(string h) => CozyData.Hex(h);
        static Color A(string h, float a) { var c = C(h); c.a = a; return c; }

        public static Texture2D Get(string key)
        {
            if (cache.TryGetValue(key, out var t) && t) return t;
            t = Make(key); cache[key] = t; return t;
        }

        static Texture2D Make(string key)
        {
            Raster g;
            switch (key)
            {
                case "road":
                {
                    g = new Raster(256, 256, 11); g.Fill(C("#6b6880"));
                    for (int k = 0; k < 1400; k++) g.Rect(g.Rnd() * 256, g.Rnd() * 256, 2, 2, g.Rnd() < 0.5f ? new Color(1, 1, 1, 0.06f) : new Color(0, 0, 0, 0.08f));
                    float W = 256 / 5.4f * 0.3f;
                    for (int y = 0; y < 256; y += 32) { var c = (y / 32) % 2 == 1 ? C("#ff7366") : C("#fff7e8"); g.Rect(0, y, W, 32, c); g.Rect(256 - W, y, W, 32, c); }
                    foreach (var o in new[] { -1.2f, 0f, 1.2f }) { float x = (o + 2.7f) / 5.4f * 256; for (int y = 0; y < 256; y += 64) g.Rect(x - 2, y, 4, 36, new Color(1, 0.97f, 0.91f, 0.85f)); }
                    return g.ToTexture();
                }
                case "hielo":
                {
                    g = new Raster(256, 256, 3);
                    for (int y = 0; y < 256; y++) g.Rect(0, y, 256, 1, Color.Lerp(C("#d9f3ff"), C("#a8dcf5"), y / 256f));
                    for (int k = 0; k < 14; k++) { float x = g.Rnd() * 256, y = g.Rnd() * 256; for (int j = 0; j < 4; j++) { float nx = x + (g.Rnd() - 0.5f) * 70, ny = y + (g.Rnd() - 0.5f) * 70; g.Line(x, y, nx, ny, 2, new Color(1, 1, 1, 0.85f), true); x = nx; y = ny; } }
                    for (int k = 0; k < 40; k++) g.Rect(g.Rnd() * 256, g.Rnd() * 256, 2, 2, new Color(1, 1, 1, 0.9f));
                    return g.ToTexture();
                }
                case "arena":
                {
                    g = new Raster(256, 256, 5); g.Fill(C("#e8c98f"));
                    for (int k = 0; k < 900; k++) g.Rect(g.Rnd() * 256, g.Rnd() * 256, 2, 2, g.Rnd() < 0.5f ? A("#b48c50", 0.35f) : A("#fff0c8", 0.5f));
                    for (int y = 20; y < 256; y += 40) for (int x = 0; x < 256; x += 8) g.Line(x, y + Mathf.Sin(x / 20f) * 6, x + 8, y + Mathf.Sin((x + 8) / 20f) * 6, 3, A("#be965a", 0.35f), true);
                    return g.ToTexture();
                }
                case "barro":
                {
                    g = new Raster(256, 256, 7); g.Fill(C("#6e4a2c"));
                    for (int k = 0; k < 45; k++) g.Ellipse(g.Rnd() * 256, g.Rnd() * 256, 8 + g.Rnd() * 22, 6 + g.Rnd() * 14, g.Rnd() * 3, g.Rnd() < 0.5f ? A("#5a3c23", 0.9f) : A("#8c643c", 0.6f), true);
                    for (int k = 0; k < 30; k++) g.Circle(g.Rnd() * 256, g.Rnd() * 256, 2 + g.Rnd() * 3, new Color(1, 1, 1, 0.18f), true);
                    return g.ToTexture();
                }
                case "agua":
                {
                    g = new Raster(256, 256, 9); g.Fill(A("#5aaaf0", 0.55f));
                    for (int k = 0; k < 10; k++) { float x = g.Rnd() * 256, y = g.Rnd() * 256; for (int j = 1; j <= 3; j++) g.Ring(x, y, j * 9, 2, new Color(1, 1, 1, 0.6f), true); }
                    return g.ToTexture();
                }
                case "turbo":
                {
                    g = new Raster(256, 256, 1); g.Fill(C("#ff9e40"));
                    for (int y = 0; y < 256; y += 64) foreach (var x in new[] { 64, 192 })
                        g.Poly(new[] { new Vector2(x - 40, y + 48), new Vector2(x, y + 8), new Vector2(x + 40, y + 48), new Vector2(x + 40, y + 62), new Vector2(x, y + 22), new Vector2(x - 40, y + 62) }, C("#ffd64d"));
                    return g.ToTexture();
                }
                case "jabon":
                {
                    g = new Raster(256, 256, 13); g.Fill(new Color(1f, 0.84f, 0.93f, 0.8f));
                    for (int k = 0; k < 60; k++) { float x = g.Rnd() * 256, y = g.Rnd() * 256, r = 3 + g.Rnd() * 12; g.Circle(x, y, r, new Color(1, 1, 1, 0.55f), true); g.Ring(x, y, r, 1.5f, new Color(0.7f, 0.78f, 1f, 0.8f), true); }
                    return g.ToTexture();
                }
                case "cross":
                {
                    g = new Raster(64, 64); for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) g.Rect(x * 8, y * 8, 8, 8, (x + y) % 2 == 1 ? C("#fff7e8") : C("#ffd64d"));
                    return g.ToTexture(true, FilterMode.Point);
                }
                case "start":
                {
                    g = new Raster(64, 16); for (int y = 0; y < 2; y++) for (int x = 0; x < 8; x++) g.Rect(x * 8, y * 8, 8, 8, (x + y) % 2 == 1 ? C("#2d2a3e") : C("#fff7e8"));
                    return g.ToTexture(true, FilterMode.Point);
                }
                case "floor_salon":
                {
                    g = new Raster(256, 256, 21); var cols = new[] { C("#c98f5a"), C("#d29b66"), C("#bf8550") };
                    for (int y = 0; y < 256; y += 32) { g.Rect(0, y, 256, 32, cols[(y / 32) % 3]); g.Rect(0, y, 256, 2, A("#3c2314", 0.35f)); g.Rect((y * 7) % 256, y, 2, 32, A("#3c2314", 0.35f)); }
                    for (int k = 0; k < 220; k++) g.Rect(g.Rnd() * 256, g.Rnd() * 256, 18 + g.Rnd() * 30, 1, A("#5a371e", 0.12f));
                    return g.ToTexture();
                }
                case "floor_cocina":
                {
                    g = new Raster(256, 256); for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) g.Rect(x * 64, y * 64, 64, 64, (x + y) % 2 == 1 ? C("#fff3dc") : C("#9fd8c0"));
                    for (int k = 0; k <= 256; k += 64) { g.Rect(k - 1, 0, 2, 256, A("#2d2a3e", 0.15f)); g.Rect(0, k - 1, 256, 2, A("#2d2a3e", 0.15f)); }
                    return g.ToTexture();
                }
                case "floor_bano":
                {
                    g = new Raster(256, 256, 4); g.Fill(C("#8fc8f0"));
                    for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) g.Rect(x * 32 + 2, y * 32 + 2, 28, 28, g.Rnd() < 0.15f ? C("#a9d6f5") : C("#93cbf2"));
                    return g.ToTexture();
                }
                case "floor_nieve":
                {
                    g = new Raster(256, 256, 8); g.Fill(C("#eef6ff"));
                    for (int k = 0; k < 400; k++) g.Circle(g.Rnd() * 256, g.Rnd() * 256, 1 + g.Rnd() * 2.5f, A("#cfe3f5", 0.6f), true);
                    return g.ToTexture();
                }
                case "floor_jardin":
                {
                    g = new Raster(256, 256, 6); g.Fill(Color.white);
                    for (int k = 0; k < 900; k++) g.Rect(g.Rnd() * 256, g.Rnd() * 256, 1, 3, new Color(0.85f, 0.95f, 0.85f, 0.5f));
                    return g.ToTexture();
                }
                case "rug":
                {
                    g = new Raster(256, 256, 2); g.Fill(C("#f2d6c4"));
                    g.Rect(9, 9, 238, 10, C("#e0a98f")); g.Rect(9, 237, 238, 10, C("#e0a98f")); g.Rect(9, 9, 10, 238, C("#e0a98f")); g.Rect(237, 9, 10, 238, C("#e0a98f"));
                    for (int y = 40; y < 230; y += 36) for (int x = 40; x < 230; x += 36) g.Circle(x, y, 6, C("#b38cf2"));
                    for (int k = 0; k < 1500; k++) g.Rect(g.Rnd() * 256, g.Rnd() * 256, 1, 3, new Color(1, 1, 1, 0.12f));
                    return g.ToTexture(false);
                }
                case "dot":
                {
                    g = new Raster(32, 32); g.Shape(0, 0, 32, 32, (x, y) => (Mathf.Sqrt((x - 16) * (x - 16) + (y - 16) * (y - 16)) - 15) / 1f, Color.white);
                    // borde suave para partículas
                    var t = new Raster(32, 32); for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) { float d = Mathf.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f)) / 16f; t.Rect(x, y, 1, 1, new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d))); }
                    return t.ToTexture(false);
                }
            }
            if (key.StartsWith("ic_")) { g = new Raster(128, 128, 5); DrawIcon(g, key.Substring(3)); return g.ToTexture(false); }
            if (key.StartsWith("sticker_")) { g = new Raster(128, 128); DrawSticker(g, key.Substring(8), 128); return g.ToTexture(false); }
            g = new Raster(4, 4); g.Fill(Color.white); return g.ToTexture();
        }

        // ---------- pegatinas (dibujadas a mano en código) ----------
        public static void DrawSticker(Raster g, string id, float S)
        {
            float cx = S / 2, cy = S / 2, R = S * 0.4f;
            g.Circle(cx, cy, S * 0.48f, C("#fff8ec"));
            Vector2 P(float x, float y) => new Vector2(cx + x * R, cy + y * R);
            switch (id)
            {
                case "estrella":
                {
                    var p = new Vector2[10]; for (int k = 0; k < 10; k++) { float a = -Mathf.PI / 2 + k * Mathf.PI / 5, r = k % 2 == 1 ? 0.45f : 1f; p[k] = P(Mathf.Cos(a) * r, Mathf.Sin(a) * r); }
                    g.Poly(p, C("#ffd64d")); break;
                }
                case "corazon":
                    g.Shape(cx - R * 1.3f, cy - R * 1.3f, cx + R * 1.3f, cy + R * 1.3f, (x, y) =>
                    {
                        float X = (x - cx) / (R * 1.05f), Y = -(y - cy) / (R * 1.05f) + 0.15f;
                        float f = Mathf.Pow(X * X + Y * Y - 1, 3) - X * X * Y * Y * Y; return f * R * 0.6f;
                    }, C("#ff7366")); break;
                case "rayo":
                    g.Poly(new[] { P(0.15f, -1), P(-0.55f, 0.1f), P(-0.05f, 0.1f), P(-0.25f, 1), P(0.55f, -0.2f), P(0.05f, -0.2f), P(0.3f, -1) }, C("#ffb04d")); break;
                case "flor":
                    for (int k = 0; k < 5; k++) { float a = k * Mathf.PI * 0.4f; g.Circle(cx + Mathf.Cos(a) * R * 0.5f, cy + Mathf.Sin(a) * R * 0.5f, R * 0.42f, C("#ff8ccc")); }
                    g.Circle(cx, cy, R * 0.35f, C("#ffd64d")); break;
                case "sonrisa":
                    g.Circle(cx, cy, R, C("#ffd64d"));
                    g.Circle(cx - 0.35f * R, cy - 0.25f * R, R * 0.12f, C("#2d2a3e")); g.Circle(cx + 0.35f * R, cy - 0.25f * R, R * 0.12f, C("#2d2a3e"));
                    g.Arc(cx, cy + 0.05f * R, R * 0.5f, R * 0.12f, 0.2f, Mathf.PI - 0.2f, C("#2d2a3e")); break;
                case "huella":
                    g.Ellipse(cx, cy + R * 0.3f, R * 0.5f, R * 0.42f, 0, C("#7a5232"));
                    foreach (var t in new[] { new Vector3(-0.6f, -0.25f, -0.3f), new Vector3(-0.22f, -0.65f, -0.1f), new Vector3(0.22f, -0.65f, 0.1f), new Vector3(0.6f, -0.25f, 0.3f) })
                        g.Ellipse(cx + t.x * R, cy + t.y * R, R * 0.18f, R * 0.24f, t.z, C("#7a5232"));
                    break;
                case "arcoiris":
                {
                    var cols = new[] { "#ff7366", "#ffb04d", "#ffd64d", "#73e0a6", "#73bfff", "#b38cf2" };
                    for (int k = 0; k < 6; k++) g.Arc(cx, cy + R * 0.4f, R * (0.95f - k * 0.13f), R * 0.14f, Mathf.PI, Mathf.PI * 2, C(cols[k]));
                    break;
                }
                case "planeta":
                    g.Circle(cx, cy, R * 0.6f, C("#b38cf2"));
                    g.Shape(cx - R * 1.1f, cy - R * 1.1f, cx + R * 1.1f, cy + R * 1.1f, (x, y) =>
                    {
                        float a = -0.4f, dx = x - cx, dy = y - cy, u = dx * Mathf.Cos(a) + dy * Mathf.Sin(a), v = -dx * Mathf.Sin(a) + dy * Mathf.Cos(a);
                        return Mathf.Abs(Mathf.Sqrt(u * u / (R * R) + v * v / (0.09f * R * R)) - 1) * R * 0.3f - R * 0.05f;
                    }, C("#ffd64d")); break;
                case "llama":
                    g.Ellipse(cx, cy + R * 0.25f, R * 0.6f, R * 0.65f, 0, C("#ff7366"));
                    g.Poly(new[] { P(-0.55f, 0.1f), P(0, -1f), P(0.55f, 0.1f) }, C("#ff7366"));
                    g.Ellipse(cx, cy + R * 0.38f, R * 0.33f, R * 0.4f, 0, C("#ffd64d"));
                    g.Poly(new[] { P(-0.3f, 0.3f), P(0.05f, -0.45f), P(0.3f, 0.3f) }, C("#ffd64d")); break;
                case "siete":
                    g.Circle(cx, cy, R, C("#4080f2"));
                    g.Poly(new[] { P(-0.45f, -0.55f), P(0.5f, -0.55f), P(0.5f, -0.35f), P(0.0f, 0.6f), P(-0.25f, 0.6f), P(0.22f, -0.33f), P(-0.45f, -0.33f) }, C("#fff8ec")); break;
            }
        }

        // ---------- iconos de la interfaz (los de Kenney están en Resources/UI/Icons; estos faltaban) ----------
        static void DrawIcon(Raster g, string id)
        {
            Color ink = C("#2d2a3e");
            switch (id)
            {
                case "chest": case "chest_gold":
                {
                    bool gold = id == "chest_gold";
                    Color wood = gold ? C("#ffd64d") : C("#c98f5a"), dark = gold ? C("#e8a83a") : C("#8a5a34"), band = gold ? C("#fff3c4") : C("#ffd64d");
                    g.Shape(14, 50, 114, 116, (x, y) => RoundBox(x - 64, y - 83, 50, 33, 10), ink);
                    g.Shape(18, 54, 110, 112, (x, y) => RoundBox(x - 64, y - 83, 46, 29, 8), wood);
                    g.Shape(14, 14, 114, 60, (x, y) => RoundBox(x - 64, y - 40, 50, 24, 16), ink);
                    g.Shape(18, 18, 110, 56, (x, y) => RoundBox(x - 64, y - 40, 46, 20, 13), dark);
                    g.Rect(18, 54, 92, 8, ink);
                    g.Rect(30, 18, 12, 94, band); g.Rect(86, 18, 12, 94, band);
                    g.Shape(50, 46, 78, 80, (x, y) => RoundBox(x - 64, y - 63, 12, 15, 4), ink);
                    g.Shape(54, 50, 74, 76, (x, y) => RoundBox(x - 64, y - 63, 8, 11, 3), band);
                    break;
                }
                case "gear":
                    for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; g.Shape(0, 0, 128, 128, (x, y) => { float dx = x - 64, dy = y - 64, u = dx * Mathf.Cos(a) + dy * Mathf.Sin(a), v = -dx * Mathf.Sin(a) + dy * Mathf.Cos(a); return RoundBox(u, v - 0, 12, 56, 4); }, ink); }
                    g.Circle(64, 64, 42, ink); g.Circle(64, 64, 17, Color.white);
                    break;
                case "car":
                    g.Shape(8, 40, 120, 96, (x, y) => RoundBox(x - 64, y - 74, 54, 20, 12), ink);
                    g.Shape(30, 22, 98, 64, (x, y) => RoundBox(x - 64, y - 48, 30, 22, 12), ink);
                    g.Circle(34, 96, 16, ink); g.Circle(94, 96, 16, ink);
                    g.Circle(34, 96, 7, Color.white); g.Circle(94, 96, 7, Color.white);
                    g.Shape(38, 30, 90, 52, (x, y) => RoundBox(x - 64, y - 43, 22, 9, 5), Color.white);
                    break;
                case "coin":
                    g.Circle(64, 64, 58, C("#c98f1a")); g.Circle(64, 60, 54, C("#ffd64d")); g.Ring(64, 60, 38, 6, C("#e8a83a"));
                    g.Shape(52, 34, 76, 86, (x, y) => RoundBox(x - 64, y - 60, 8, 22, 4), C("#e8a83a"));
                    break;
                case "wrench":
                    g.Line(32, 96, 84, 44, 22, ink); g.Circle(90, 38, 26, ink); g.Shape(84, 4, 124, 40, (x, y) => RoundBox(x - 104, y - 22, 9, 16, 3), Color.white);
                    g.Circle(28, 100, 15, ink);
                    break;
                case "book":
                    g.Shape(10, 16, 118, 112, (x, y) => RoundBox(x - 64, y - 64, 52, 46, 10), ink);
                    g.Rect(60, 20, 8, 88, Color.white); g.Rect(22, 34, 30, 6, Color.white); g.Rect(22, 50, 30, 6, Color.white); g.Rect(76, 34, 30, 6, Color.white); g.Rect(76, 50, 30, 6, Color.white);
                    break;
                case "flag":
                    g.Rect(22, 10, 10, 110, ink);
                    for (int y = 0; y < 4; y++) for (int x = 0; x < 5; x++) g.Rect(32 + x * 16, 14 + y * 14, 16, 14, (x + y) % 2 == 0 ? ink : Color.white);
                    break;
            }
        }
        static float RoundBox(float x, float y, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(x) - hw + r, qy = Mathf.Abs(y) - hh + r;
            return Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        // ---------- piezas de interfaz ----------
        // Las formas dejan 1,5 px transparentes alrededor: así el borde lo suaviza la textura y no queda dentado
        // cuando la pieza está girada (la geometría de la UI no tiene antialiasing).
        static Sprite round, circle, soft;
        public static Sprite Round()
        {
            if (round) return round;
            const float half = 30.5f, rad = 20.5f;
            var g = new Raster(64, 64);
            g.Shape(0, 0, 64, 64, (x, y) => { float qx = Mathf.Abs(x - 32) - half + rad, qy = Mathf.Abs(y - 32) - half + rad; return Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - rad; }, Color.white);
            return round = g.ToSprite(24);
        }
        // rectángulo de esquinas rectas con el borde suavizado (franjas, cintas, líneas)
        public static Sprite SoftRect()
        {
            if (soft) return soft;
            var g = new Raster(16, 16);
            g.Shape(0, 0, 16, 16, (x, y) => Mathf.Max(Mathf.Abs(x - 8), Mathf.Abs(y - 8)) - 6.5f, Color.white);
            return soft = g.ToSprite(4);
        }
        public static Sprite CircleSprite()
        {
            if (circle) return circle;
            var g = new Raster(64, 64); g.Circle(32, 32, 31, Color.white);
            return circle = g.ToSprite(0);
        }
    }
}
