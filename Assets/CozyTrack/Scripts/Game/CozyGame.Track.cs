using System.Collections.Generic;
using UnityEngine;

namespace CozyTrack
{
    public partial class CozyGame
    {
        class Pickup { public int i; public float u, o, t; public GameObject m; public bool taken; }
        class Obstacle { public ObstDef d; public GameObject m; public float t, o; }
        readonly List<Pickup> boxes = new List<Pickup>(), coins = new List<Pickup>();
        readonly List<Obstacle> obstacles = new List<Obstacle>();
        readonly Dictionary<string, Material> surfMats = new Dictionary<string, Material>();
        Vector3 circC; float circR = 40; float surfT;

        void BuildCircuit(CircuitDef def)
        {
            circ = def; circId = def.id;
            if (!def.id.StartsWith("nivel")) PlayerPrefs.SetString("ctCirc", def.id);
            for (int k = trackRoot.childCount - 1; k >= 0; k--) Destroy(trackRoot.GetChild(k).gameObject);
            lampBulbs.Clear(); surfMats.Clear();
            Track.Build(def);
            BuildRoad();
            var b = Track.GetBounds(); circC = new Vector3(b.center.x, 0, b.center.z); circR = Mathf.Max(b.size.x, b.size.z);
            BuildRoom(def.room, b);
            PlacePickups(def);
            BuildSurfaces();
            BuildLamps();
            BuildObstacles(def);
            ApplyTod(todId);
            if (state == "menu") MenuGrid();
            RefreshMenuSelections();
        }

        // ---------- calzada procedural ----------
        Mesh Strip(int i, float u0, float u1, float o0, float o1, int steps, float vScale, float lift)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var idx = new List<int>();
            for (int k = 0; k <= steps; k++)
            {
                float u = u0 + (u1 - u0) * k / steps;
                foreach (var o in new[] { o0, o1 })
                {
                    var w = Track.World(i, u, o);
                    v.Add(w.pos + Vector3.up * lift * Mathf.Cos(w.roll)); uv.Add(new Vector2(o == o0 ? 0 : 1, u * vScale));
                }
                if (k < steps) { int a = 2 * k; idx.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
            }
            var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(idx, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
        GameObject MeshObj(string name, Mesh mesh, Material mat, bool shadows = false)
        {
            var g = new GameObject(name); g.transform.SetParent(trackRoot, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
        Mesh Tube(List<Vector3> pts, float r, int seg = 8)
        {
            var v = new List<Vector3>(); var idx = new List<int>();
            for (int k = 0; k < pts.Count; k++)
            {
                var t = (pts[Mathf.Min(k + 1, pts.Count - 1)] - pts[Mathf.Max(k - 1, 0)]).normalized;
                var n = Vector3.Cross(t, Vector3.up); if (n.sqrMagnitude < 1e-4f) n = Vector3.Cross(t, Vector3.right); n.Normalize();
                var bN = Vector3.Cross(t, n);
                for (int s = 0; s < seg; s++) { float a = s * Mathf.PI * 2 / seg; v.Add(pts[k] + (n * Mathf.Cos(a) + bN * Mathf.Sin(a)) * r); }
                if (k > 0) for (int s = 0; s < seg; s++) { int a = (k - 1) * seg + s, b = (k - 1) * seg + (s + 1) % seg; idx.AddRange(new[] { a, b, a + seg, b, b + seg, a + seg }); }
            }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(idx, 0); m.RecalculateNormals(); return m;
        }

        void BuildRoad()
        {
            var solid = Mat(Color.white, CozyTex.Get("road"), false, true, null, 0);
            var glass = Mat(new Color(1, 1, 1, 0.42f), CozyTex.Get("road"), true, true);
            var rail = Mat(new Color(0.62f, 0.85f, 1f, 0.75f), null, true);
            var post = Mat(CozyData.Hex("#fff7e8"), null, false, false, null, 1);
            var crossMat = Mat(Color.white, CozyTex.Get("cross"), false, true, null, 0);
            bool crossDone = false;
            for (int i = 0; i < Track.N; i++)
            {
                var p = Track.Pieces[i]; float len = Track.PieceLen(i, 0);
                int steps = p.straight ? (p.twist != 0 ? 28 : 2) : 18;
                if (p.cross)
                {
                    MeshObj("Cruce", Strip(i, 0, 1, -Track.Half, Track.Half, 2, len / 5.4f, crossDone ? 0.006f : 0.004f), crossMat);
                    crossDone = true; continue;
                }
                MeshObj("Pieza " + i + " " + p.k, Strip(i, 0, 1, -Track.Half, Track.Half, steps, len / 5.4f, 0), p.twist != 0 ? glass : solid);
                if (p.twist != 0)
                {
                    foreach (var o in new[] { -2.85f, 2.85f })
                    {
                        var pts = new List<Vector3>(); for (int k = 0; k <= 24; k++) pts.Add(Track.World(i, k / 24f, o).pos);
                        MeshObj("Barandilla", Tube(pts, 0.09f), rail);
                    }
                    if (p.twist == 'r')
                        foreach (var u in new[] { 0.1f, 0.5f, 0.9f }) foreach (var o in new[] { -3.0f, 3.0f })
                        {
                            var w = Track.World(i, u, o);
                            Prim(PrimitiveType.Cylinder, Color.white, new Vector3(w.pos.x, (w.pos.y - 0.2f) / 2, w.pos.z), new Vector3(0.22f, (w.pos.y + 0.2f) / 2, 0.22f), trackRoot, 0, post);
                        }
                }
            }
            var start = Mat(Color.white, CozyTex.Get("start"), false, true, null, 0);
            MeshObj("Salida", Strip(0, 0.03f, 0.03f + 0.7f / Track.PieceLen(0, 0), -2.4f, 2.4f, 1, 1, 0.003f), start);
        }

        // ---------- superficies: capa visual sobre la calzada ----------
        Material SurfMat(string kind)
        {
            if (surfMats.TryGetValue(kind, out var m)) return m;
            var tex = CozyTex.Get(kind);
            m = Mat(Color.white, tex, true, true, kind == "turbo" ? CozyData.Hex("#ff9e40") * 0.35f : (Color?)null);
            return surfMats[kind] = m;
        }
        void BuildSurfaces()
        {
            for (int i = 0; i < Track.N; i++)
            {
                string kind = Track.SurfId(i); if (kind == "asfalto") continue;
                var p = Track.Pieces[i]; float len = Track.PieceLen(i, 0), half = Track.NL * Track.LaneW / 2;
                int steps = p.straight ? (p.twist != 0 ? 28 : 4) : 16;
                var mesh = Strip(i, 0, 1, -half, half, steps, len / 2.4f, 0.012f);
                var uv = new List<Vector2>(); mesh.GetUVs(0, uv); for (int k = 0; k < uv.Count; k++) uv[k] = new Vector2(uv[k].x * (Track.NL * Track.LaneW / 2.4f), uv[k].y); mesh.SetUVs(0, uv);
                MeshObj("Superficie " + kind, mesh, SurfMat(kind));
            }
        }
        void AnimateSurfaces(float dt)
        {
            surfT += dt;
            if (surfMats.TryGetValue("turbo", out var t)) t.SetTextureOffset("_BaseMap", new Vector2(0, -surfT * 1.5f));
            if (surfMats.TryGetValue("agua", out var a)) a.SetTextureOffset("_BaseMap", new Vector2(Mathf.Sin(surfT * 0.7f) * 0.05f, surfT * 0.05f));
        }

        // ---------- farolas (se encienden de noche) ----------
        void BuildLamps()
        {
            var postMat = Mat(CozyData.Hex("#3b3a4f"));
            var spots = new List<(int, float)>();
            if (circ.id == "jardin") spots.AddRange(new[] { (0, 0.3f), (2, 0.5f), (5, 0.5f), (8, 0.5f), (10, 0.5f), (13, 0.5f) });
            else for (int i = 1; i < Track.N; i += 3) { var p = Track.Pieces[i]; if (p.twist == 0 && !p.cross && !Track.Pieces[(i + 1) % Track.N].cross && !Track.Pieces[(i + Track.N - 1) % Track.N].cross) spots.Add((i, 0.5f)); }
            foreach (var (i, u) in spots)
            {
                var w = Track.World(i, u, Track.LaneO(-0.4f) - 0.9f);
                Prim(PrimitiveType.Cylinder, Color.white, new Vector3(w.pos.x, 1.1f, w.pos.z), new Vector3(0.14f, 1.3f, 0.14f), trackRoot, 0, postMat);
                var bulb = Prim(PrimitiveType.Sphere, Color.white, new Vector3(w.pos.x, 2.45f, w.pos.z), Vector3.one * 0.44f, trackRoot, 0, Mat(CozyData.Hex("#fff3c4")));
                lampBulbs.Add(bulb);
            }
        }

        // ---------- cajas de objetos y monedas ----------
        void PlacePickups(CircuitDef def)
        {
            boxes.Clear(); coins.Clear();
            var bs = def.boxes; var cs = def.coins;
            bool Plain(int i) { var p = Track.Pieces[i]; return p.straight && p.twist == 0 && !p.cross; }
            if (bs == null)
            {
                var l = new List<int[]>(); var st = new List<int>(); for (int i = 1; i < Track.N; i++) if (Plain(i)) st.Add(i);
                var pick = new List<int>(); if (st.Count > 0) pick.Add(st[0]); if (st.Count > 1) pick.Add(st[st.Count / 2]);
                for (int k = 0; k < pick.Count; k++) foreach (var ln in k % 2 == 1 ? new[] { 1, 2 } : new[] { 0, 3 }) l.Add(new[] { pick[k], ln });
                bs = l.ToArray();
            }
            if (cs == null) { var l = new List<int[]>(); for (int i = 0; i < Track.N; i += 2) if (!Track.Pieces[i].straight) l.Add(new[] { i, i % 4 }); cs = l.ToArray(); }
            foreach (var b in bs) { var m = Spawn("Item_Box", trackRoot, true); m.transform.localScale = Vector3.one * 0.75f; boxes.Add(new Pickup { i = b[0], u = 0.5f, o = Track.LaneO(b[1]), m = m }); }
            foreach (var c in cs) for (int k = 0; k < 5; k++) { var m = Spawn("Item_Coin", trackRoot); m.transform.localScale = Vector3.one * 0.9f; coins.Add(new Pickup { i = c[0], u = 0.15f + k * 0.17f, o = Track.LaneO(c[1]), m = m }); }
            foreach (var p in boxes) { var w = Track.World(p.i, p.u, p.o); p.m.transform.position = w.pos + Vector3.up * 0.55f; }
            foreach (var p in coins) { var w = Track.World(p.i, p.u, p.o); p.m.transform.position = w.pos + Vector3.up * 0.55f; }
        }

        // ---------- habitaciones de una casa (muebles a escala de coche de juguete) y jardín ----------
        void BuildRoom(string room, Bounds b)
        {
            groundMat.SetTexture("_BaseMap", CozyTex.Get("floor_" + (room ?? "jardin")));
            groundMat.SetTextureScale("_BaseMap", new Vector2(room == "jardin" ? 80 : 40, room == "jardin" ? 80 : 40));
            float x0 = b.min.x, x1 = b.max.x, z0 = b.min.z, z1 = b.max.z, cx = b.center.x, cz = b.center.z, W = b.size.x, D = b.size.z;
            GameObject Box(float w, float h, float d, string col, float x, float y, float z, float ry = 0) => Prim(PrimitiveType.Cube, CozyData.Hex(col), new Vector3(x, y, z), new Vector3(w, h, d), trackRoot, ry, Mat(CozyData.Hex(col), null, false, false, null, 1));
            GameObject Cyl(float r, float h, string col, float x, float y, float z) => Prim(PrimitiveType.Cylinder, CozyData.Hex(col), new Vector3(x, y, z), new Vector3(r * 2, h / 2, r * 2), trackRoot, 0, Mat(CozyData.Hex(col), null, false, false, null, 1));
            GameObject Sph(float r, string col, float x, float y, float z) => Prim(PrimitiveType.Sphere, CozyData.Hex(col), new Vector3(x, y, z), Vector3.one * r * 2, trackRoot, 0, Mat(CozyData.Hex(col), null, false, false, null, 1));
            switch (room)
            {
                case "salon":
                {
                    var rug = Prim(PrimitiveType.Quad, Color.white, new Vector3(cx, -0.195f, cz), new Vector3(W + 8, D + 8, 1), trackRoot, 0, Mat(Color.white, CozyTex.Get("rug"), false, false, null, 0));
                    rug.transform.rotation = Quaternion.Euler(90, 0, 0);
                    float sx = x1 + 8, L = D + 6;
                    Box(6, 2.4f, L, "#b38cf2", sx, 1.0f, cz); Box(2.2f, 5, L, "#a07ce0", sx + 3.2f, 2.3f, cz);
                    Box(6.6f, 3.6f, 2.2f, "#a07ce0", sx, 1.6f, cz - L / 2); Box(6.6f, 3.6f, 2.2f, "#a07ce0", sx, 1.6f, cz + L / 2);
                    string[] cush = { "#ffd64d", "#73e0a6", "#ff8ccc" };
                    for (int k = -1; k <= 1; k++) Box(1.2f, 2.6f, 3.2f, cush[k + 1], sx + 1.9f, 3.4f, cz + k * (L / 3.2f), k * 6);
                    float lx = x0 - 6, lz = z1 + 1;
                    Cyl(1.9f, 0.4f, "#3b3a4f", lx, 0, lz); Cyl(0.14f, 10, "#3b3a4f", lx, 5, lz);
                    var shade = Cyl(2.0f, 2.4f, "#fff3c4", lx, 10, lz); shade.GetComponent<Renderer>().sharedMaterial.SetColor("_EmissionColor", CozyData.Hex("#ffd88a") * 0.4f);
                    string[] books = { "#ff7366", "#73bfff", "#ffd64d", "#73e0a6", "#b38cf2", "#ff9e40" };
                    for (int k = 0; k < books.Length; k++) Box(3.4f, 0.62f, 2.4f, books[k], x0 - 5, 0.11f + k * 0.64f, z0 + 4, k * 10 - 23);
                    var tb = Spawn("Prop_Playroom_ToyBlock", trackRoot, true); tb.transform.position = new Vector3(x0 - 3, -0.2f, cz); tb.transform.localScale = Vector3.one * 1.6f;
                    break;
                }
                case "cocina":
                {
                    foreach (var (x, z) in new[] { (x0 - 2.5f, z0 - 2.5f), (x1 + 2.5f, z0 - 2.5f), (x0 - 2.5f, z1 + 2.5f), (x1 + 2.5f, z1 + 2.5f) }) Cyl(0.65f, 16, "#d29b66", x, 7.8f, z);
                    float mx = x1 + 5, mz = cz - 4;
                    Cyl(1.7f, 3.6f, "#73bfff", mx, 1.6f, mz); Cyl(1.5f, 0.06f, "#7a5232", mx, 3.4f, mz);
                    var h = Prim(PrimitiveType.Cylinder, Color.white, new Vector3(mx + 1.9f, 1.7f, mz), new Vector3(1.2f, 0.25f, 1.2f), trackRoot, 0, Mat(CozyData.Hex("#73bfff")));
                    h.transform.rotation = Quaternion.Euler(0, 0, 90);
                    Sph(1.5f, "#ff5a4f", x0 - 4.5f, 1.25f, cz + 3); Cyl(0.09f, 0.8f, "#7a5232", x0 - 4.5f, 2.9f, cz + 3);
                    var leaf = Sph(0.5f, "#66bf59", x0 - 4.1f, 2.8f, cz + 3); leaf.transform.localScale = new Vector3(1, 0.3f, 0.6f);
                    Cyl(1.8f, 0.35f, "#e8b56a", cx, -0.05f, z1 + 5.5f);
                    for (int k = 0; k < 7; k++) Sph(0.18f, "#5a3a22", cx + Mathf.Cos(k * 1.7f) * 1.1f, 0.13f, z1 + 5.5f + Mathf.Sin(k * 1.7f) * 1.1f);
                    break;
                }
                case "bano":
                {
                    float bx = x1 + 7;
                    Box(7, 4, D + 6, "#fff7e8", bx, 1.8f, cz);
                    Prim(PrimitiveType.Cube, Color.white, new Vector3(bx, 3.82f, cz), new Vector3(5.6f, 0.1f, D + 4.6f), trackRoot, 0, Mat(new Color(0.45f, 0.75f, 1f, 0.8f), null, true));
                    Box(1.4f, 0.8f, 2.2f, "#ff8ccc", x0 - 4, 0.2f, cz - 3, 23);
                    var bub = Mat(new Color(1, 1, 1, 0.6f), null, true);
                    for (int k = 0; k < 6; k++) Prim(PrimitiveType.Sphere, Color.white, new Vector3(x0 - 4 + Random.value * 2 - 1, 0.8f + Random.value * 1.2f, cz - 3 + Random.value * 2 - 1), Vector3.one * (0.4f + Random.value * 0.5f), trackRoot, 0, bub);
                    Cyl(1.0f, 3.4f, "#ffd64d", x0 - 5, 1.5f, z1 + 2);
                    var b1 = Cyl(0.12f, 4.2f, "#ff7366", x0 - 5.3f, 4.2f, z1 + 2); b1.transform.rotation = Quaternion.Euler(0, 0, -17);
                    var b2 = Cyl(0.12f, 4.2f, "#73e0a6", x0 - 4.65f, 4.2f, z1 + 2); b2.transform.rotation = Quaternion.Euler(0, 0, 20);
                    Box(2.4f, 0.5f, 5, "#ff9e40", x0 - 4, 0.05f, z0 + 3, 17);
                    break;
                }
                case "nieve": Scatter(new[] { "Prop_Snow_Pine", "Prop_Snow_Pine", "Prop_Garden_Rock", "Prop_Common_TrafficCone" }, b, 50); break;
                default:
                    Scatter(new[] { "Prop_Garden_Tree", "Prop_Garden_Bush", "Prop_Garden_Flower", "Prop_Garden_Rock", "Prop_Common_TrafficCone", "Prop_Snow_Pine", "Prop_Playroom_ToyBlock", "Prop_Garden_Flower" }, b, 70);
                    var flag = Spawn("Prop_Common_Flag", trackRoot, true); var f = Track.World(0, 0.5f, -Track.LaneO(-1.2f)); flag.transform.position = new Vector3(f.pos.x, -0.2f, f.pos.z);
                    break;
            }
        }
        void Scatter(string[] names, Bounds b, int count)
        {
            var center = new List<Vector3>(); for (int i = 0; i < Track.N; i++) for (float u = 0; u < 1; u += 0.25f) center.Add(Track.World(i, u, 0).pos);
            var r = new Raster(1, 1, 7); int placed = 0, tries = 0;
            while (placed < count && tries++ < 4000)
            {
                float x = b.min.x - 24 + r.Rnd() * (b.size.x + 48), z = b.min.z - 24 + r.Rnd() * (b.size.z + 48);
                float d = 1e9f; foreach (var c in center) d = Mathf.Min(d, Vector2.Distance(new Vector2(c.x, c.z), new Vector2(x, z)));
                if (d < 5 || d > 26) continue;
                var m = Spawn(names[Mathf.FloorToInt(r.Rnd() * names.Length) % names.Length], trackRoot, true);
                m.transform.position = new Vector3(x, -0.2f, z); m.transform.rotation = Quaternion.Euler(0, r.Rnd() * 360, 0); m.transform.localScale = Vector3.one * (0.9f + r.Rnd() * 0.6f);
                placed++;
            }
        }

        // ---------- obstáculos vivos: cruzan la pista de vez en cuando ----------
        GameObject ObstMesh(string type)
        {
            var g = new GameObject("Obstaculo " + type); g.transform.SetParent(trackRoot, false);
            GameObject P(PrimitiveType t, string c, Vector3 pos, Vector3 sc, Vector3 rot = default)
            {
                var o = Prim(t, CozyData.Hex(c), pos, sc, g.transform, 0, Mat(CozyData.Hex(c))); o.transform.localRotation = Quaternion.Euler(rot); return o;
            }
            if (type == "roomba")
            {
                P(PrimitiveType.Cylinder, "#3b3a4f", new Vector3(0, 0.12f, 0), new Vector3(1.24f, 0.1f, 1.24f));
                P(PrimitiveType.Cylinder, "#4a4860", new Vector3(0, 0.24f, 0), new Vector3(1.0f, 0.02f, 1.0f));
                P(PrimitiveType.Cylinder, "#73e0a6", new Vector3(0, 0.27f, 0.25f), new Vector3(0.24f, 0.03f, 0.24f));
            }
            else if (type == "cat")
            {
                P(PrimitiveType.Cube, "#ff9e40", new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.45f, 1.1f));
                P(PrimitiveType.Sphere, "#ff9e40", new Vector3(0, 0.78f, 0.6f), Vector3.one * 0.6f);
                foreach (var x in new[] { -0.14f, 0.14f }) P(PrimitiveType.Cube, "#ff9e40", new Vector3(x, 1.05f, 0.58f), new Vector3(0.12f, 0.2f, 0.08f), new Vector3(0, 0, x > 0 ? -12 : 12));
                foreach (var x in new[] { -0.1f, 0.1f }) P(PrimitiveType.Sphere, "#2d2a3e", new Vector3(x, 0.82f, 0.86f), Vector3.one * 0.09f);
                foreach (var v in new[] { new Vector2(-0.17f, 0.4f), new Vector2(0.17f, 0.4f), new Vector2(-0.17f, -0.4f), new Vector2(0.17f, -0.4f) }) P(PrimitiveType.Cylinder, "#fff7e8", new Vector3(v.x, 0.15f, v.y), new Vector3(0.14f, 0.15f, 0.14f));
                P(PrimitiveType.Cylinder, "#ff9e40", new Vector3(0, 0.85f, -0.7f), new Vector3(0.1f, 0.4f, 0.1f), new Vector3(-35, 0, 0));
            }
            else
            {
                P(PrimitiveType.Sphere, "#ffd64d", new Vector3(0, 0.35f, 0), new Vector3(0.96f, 0.72f, 1.2f));
                P(PrimitiveType.Sphere, "#ffd64d", new Vector3(0, 0.85f, 0.35f), Vector3.one * 0.6f);
                P(PrimitiveType.Cube, "#ff9e40", new Vector3(0, 0.82f, 0.68f), new Vector3(0.2f, 0.08f, 0.26f));
                foreach (var x in new[] { -0.12f, 0.12f }) P(PrimitiveType.Sphere, "#2d2a3e", new Vector3(x, 0.93f, 0.6f), Vector3.one * 0.08f);
            }
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial.SetFloat("_Outline", 1);
            return g;
        }
        void BuildObstacles(CircuitDef def)
        {
            obstacles.Clear();
            foreach (var d in def.obst ?? new ObstDef[0]) obstacles.Add(new Obstacle { d = d, m = ObstMesh(d.type), t = d.period * 0.6f });
        }
        static readonly Dictionary<string, string> ObstName = new Dictionary<string, string> { { "roomba", "robot aspirador" }, { "cat", "gato" }, { "duck", "patito" } };
        void UpdateObstacles(float dt)
        {
            foreach (var ob in obstacles)
            {
                if (!ob.m) continue;
                ob.t += dt;
                float a = (ob.t % ob.d.period) / ob.d.period * Mathf.PI * 2, s = Mathf.Sin(a) * 1.7f;
                float o = Mathf.Clamp(s, -1, 1) * 3.9f; int dir = Mathf.Cos(a) >= 0 ? 1 : -1; bool moving = Mathf.Abs(s) < 1;
                var w = Track.World(ob.d.i, ob.d.u, o); ob.o = o;
                ob.m.transform.position = w.pos + Vector3.up * (moving && ob.d.type != "roomba" ? Mathf.Abs(Mathf.Sin(ob.t * 12)) * 0.07f : 0);
                ob.m.transform.rotation = Quaternion.Euler(0, (w.h + dir * Mathf.PI / 2) * Mathf.Rad2Deg + (ob.d.type == "roomba" ? ob.t * 115 : 0), 0);
                if (state != "race") continue;
                foreach (var c in cars)
                {
                    if (c.crash > 0 || c.isOut || c.obCool > raceT) continue;
                    var p = Track.World(c.i, c.u, c.o);
                    if (Vector2.Distance(new Vector2(p.pos.x, p.pos.z), new Vector2(ob.m.transform.position.x, ob.m.transform.position.z)) < 0.85f && Mathf.Abs(p.pos.y - w.pos.y) < 1)
                    {
                        c.obCool = raceT + 1.5f; HitCar(c, null, "spin");
                        if (c.isPlayer) Toast("¡Cuidado con el " + ObstName[ob.d.type] + "!");
                    }
                }
            }
        }

        // ---------- línea de frenada (como en Need for Speed) ----------
        // 30 m por delante en tu carril; cada tramo compara tu velocidad con la máxima admisible en ese punto
        // (contando lo que te da tiempo a frenar): verde = bien, amarillo = cerca del límite, rojo = suelta.
        const int LineN = 60; const float LineStep = 0.5f, LineW = 0.28f;
        Mesh lineMesh; GameObject lineGo; Vector3[] lineV; Color[] lineC;
        void BuildBrakeLine()
        {
            lineMesh = new Mesh(); lineMesh.MarkDynamic();
            lineV = new Vector3[LineN * 2]; lineC = new Color[LineN * 2]; var idx = new List<int>();
            for (int k = 0; k < LineN - 1; k++) { int a = 2 * k; idx.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 }); }
            lineMesh.vertices = lineV; lineMesh.colors = lineC; lineMesh.SetTriangles(idx, 0); lineMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000);
            lineGo = new GameObject("Linea de frenada"); lineGo.AddComponent<MeshFilter>().sharedMesh = lineMesh;
            var m = new Material(lib.unlit); m.SetColor("_BaseColor", new Color(1, 1, 1, 0.85f));
            var r = lineGo.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineGo.SetActive(false);
        }
        static readonly Color COk = CozyData.Hex("#5be38a"), CWarn = CozyData.Hex("#ffd64d"), CBad = CozyData.Hex("#ff5a4f");
        void UpdateBrakeLine()
        {
            if (state != "race" || !assistOn || player.crash > 0) { lineGo.SetActive(false); return; }
            int total = Mathf.CeilToInt(45 / LineStep);
            var si = new int[total]; var su = new float[total]; var vs = new float[total];
            int i = player.i; float u = player.u;
            for (int k = 0; k < total; k++)
            {
                si[k] = i; su[k] = u; vs[k] = Track.Pieces[i].straight ? float.PositiveInfinity : VSafe(player, i, player.o) * RaceCar.CornerQEq;
                float left = LineStep;
                while (left > 0) { float L = Track.PieceLen(i, player.o), rem = (1 - u) * L; if (left < rem) { u += left / L; left = 0; } else { left -= rem; u = 0; i = (i + 1) % Track.N; } }
            }
            var allow = new float[total]; allow[total - 1] = vs[total - 1];
            for (int k = total - 2; k >= 0; k--) allow[k] = Mathf.Min(vs[k], Mathf.Sqrt(allow[k + 1] * allow[k + 1] + 2 * player.brake * LineStep));
            float v = player.v;
            for (int k = 0; k < LineN; k++)
            {
                var w = Track.World(si[k], su[k], player.o);
                float cr = Mathf.Cos(w.roll);
                var lat = new Vector3(Mathf.Cos(w.h) * cr, Mathf.Sin(w.roll), -Mathf.Sin(w.h) * cr) * (LineW / 2);
                var c = w.pos + Vector3.up * 0.02f * cr;
                lineV[k * 2] = c - lat; lineV[k * 2 + 1] = c + lat;
                float ratio = v / allow[k];
                Color col = ratio <= 0.9f ? COk : ratio <= 1 ? Color.Lerp(COk, CWarn, (ratio - 0.9f) / 0.1f) : Color.Lerp(CWarn, CBad, Mathf.Min(1, (ratio - 1) / 0.08f));
                lineC[k * 2] = lineC[k * 2 + 1] = col;
            }
            lineMesh.vertices = lineV; lineMesh.colors = lineC;
            lineGo.SetActive(true);
            player.mustBrake = v > allow[0];
        }
    }
}
