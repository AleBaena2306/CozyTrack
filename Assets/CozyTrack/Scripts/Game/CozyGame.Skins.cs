using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CozyTrack
{
    // Skins: pintura (de fábrica, colores lisos o estampados), color de detalles, diseño de ruedas y color de llanta.
    // Los colores son gratis; los estampados y los diseños de rueda se compran con monedas o salen en cofres de oro.
    // Se aplican cambiando los materiales de las ranuras CT_Paint_* / CT_Trim_* / CT_WheelRim de cada vehículo.
    public class PaintDef { public string id, name, pattern; public Color color; public int price; }
    public class WheelDef { public string id, name; public int price; }

    public static class Skins
    {
        public const int PatternPrice = 150, WheelPrice = 120;
        static Color H(string h) => CozyData.Hex(h);
        public static readonly PaintDef[] Paints = BuildPaints();
        static PaintDef[] BuildPaints()
        {
            var l = new List<PaintDef> { new PaintDef { id = "", name = "Factory", color = Color.white } };
            (string, string)[] solid = { ("cream", "#FFF4E0"), ("sunny", "#FFCF3D"), ("coral", "#FF6B4A"), ("ocean", "#2F7DD1"), ("grape", "#7B63D1"),
                                         ("mint", "#73E0A6"), ("sky", "#8FC8F0"), ("bubblegum", "#FF9EC4"), ("ink", "#3A3350"), ("snow", "#F4F3EF") };
            foreach (var (id, hex) in solid) l.Add(new PaintDef { id = id, name = char.ToUpper(id[0]) + id.Substring(1), color = H(hex) });
            (string, string)[] pats = { ("Dots_Sky", "Sky dots"), ("Stripes_Coral", "Coral stripes"), ("Hearts_Bubblegum", "Hearts"), ("Stars_Night", "Night stars"),
                                        ("Checker_Race", "Race checker"), ("Bolt_Electric", "Electric bolts"), ("Confetti_Cream", "Confetti"), ("Hex_Mint", "Mint hex"),
                                        ("Plaid_Cherry", "Cherry plaid"), ("Triangles_Sunset", "Sunset triangles"), ("Waves_Ocean", "Ocean waves"), ("Zigzag_Grape", "Grape zigzag") };
            foreach (var (id, n) in pats) l.Add(new PaintDef { id = id, name = n, pattern = "Skin_" + id, color = Color.white, price = PatternPrice });
            return l.ToArray();
        }
        // Detalles y llantas: colores gratis («» = de fábrica)
        public static readonly (string id, Color c)[] Colors =
        {
            ("", Color.white), ("cream", H("#FFF4E0")), ("sunny", H("#FFCF3D")), ("coral", H("#FF6B4A")), ("ocean", H("#2F7DD1")),
            ("grape", H("#7B63D1")), ("mint", H("#73E0A6")), ("ink", H("#3A3350")), ("silver", H("#C9CED8")),
        };
        public static readonly WheelDef[] Wheels =
        {
            new WheelDef { id = "", name = "Factory" }, new WheelDef { id = "Classic", name = "Classic", price = WheelPrice }, new WheelDef { id = "Racing", name = "Racing", price = WheelPrice },
            new WheelDef { id = "Spokes", name = "Spokes", price = WheelPrice }, new WheelDef { id = "Star", name = "Star", price = WheelPrice }, new WheelDef { id = "Knobby", name = "Knobby", price = WheelPrice },
            new WheelDef { id = "Turbine", name = "Turbine", price = WheelPrice }, new WheelDef { id = "Whitewall", name = "Whitewall", price = WheelPrice },
        };
        public static PaintDef Paint(string id) => Paints.FirstOrDefault(p => p.id == id) ?? Paints[0];
        public static Color? ColorOf(string id) { if (string.IsNullOrEmpty(id)) return null; foreach (var c in Colors) if (c.id == id) return c.c; return null; }
        // claves de la colección: «p:Dots_Sky», «w:Star»
        public static IEnumerable<string> Lockables() => Paints.Where(p => p.price > 0).Select(p => "p:" + p.id).Concat(Wheels.Where(w => w.price > 0).Select(w => "w:" + w.id));
        public static string NameOf(string key) => key.StartsWith("p:") ? Paint(key.Substring(2)).name + " paint" : (Wheels.FirstOrDefault(w => "w:" + w.id == key)?.name ?? key) + " wheels";
    }

    public partial class CozyGame
    {
        // skin de cada vehículo: pintura | detalles | ruedas | llantas
        string[] SkinOf(string car)
        {
            int i = prof.skinCar.IndexOf(car);
            var v = i >= 0 ? prof.skinVal[i].Split('|') : null;
            return v != null && v.Length == 4 ? v : new[] { "", "", "", "" };
        }
        void SetSkin(string car, int part, string id)
        {
            var v = SkinOf(car); v[part] = id ?? "";
            int i = prof.skinCar.IndexOf(car); string s = string.Join("|", v);
            if (i >= 0) prof.skinVal[i] = s; else { prof.skinCar.Add(car); prof.skinVal.Add(s); }
            SaveProfile();
        }
        bool OwnsSkin(string key) => prof.ownedSkins.Contains(key) || allUnlocked;
        bool PaintOwned(PaintDef p) => p.price == 0 || OwnsSkin("p:" + p.id);
        bool WheelOwned(WheelDef w) => w.price == 0 || OwnsSkin("w:" + w.id);

        Mesh WheelMesh(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var m = lib.Get("Wheel_" + id); var mf = m ? m.GetComponentInChildren<MeshFilter>(true) : null;
            return mf ? mf.sharedMesh : null;
        }
        Texture2D PatternTex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var t in lib.skinPatterns) if (t && t.name == name) return t;
            return null;
        }

        // Vehículo con la skin que el jugador le ha puesto (los rivales salen de fábrica)
        GameObject SpawnCar(string car, Transform parent, bool skinned)
        {
            var s = skinned ? SkinOf(car) : null;
            var g = Spawn("Car_" + car, parent, true, true, skinned ? WheelMesh(s[2]) : null);
            if (skinned) PaintCar(g, s);
            ApplySticker(g, car);
            return g;
        }

        readonly Dictionary<string, Material> skinMats = new Dictionary<string, Material>();
        Material SkinMat(Material baseM, Color c, Texture tex)
        {
            string key = baseM.GetInstanceID() + "|" + ColorUtility.ToHtmlStringRGB(c) + "|" + (tex ? tex.name : "-");
            if (skinMats.TryGetValue(key, out var m) && m) return m;
            m = new Material(baseM) { name = baseM.name };
            m.SetColor("_BaseColor", c);
            if (tex) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero); }
            else m.SetTexture("_BaseMap", Texture2D.whiteTexture);
            return skinMats[key] = m;
        }

        void PaintCar(GameObject g, string[] s)
        {
            var paint = Skins.Paint(s[0]); var trim = Skins.ColorOf(s[1]); var rim = Skins.ColorOf(s[3]);
            var tex = PatternTex(paint.pattern);
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials; bool changed = false;
                for (int k = 0; k < ms.Length; k++)
                {
                    if (!ms[k]) continue; string n = ms[k].name;
                    if (paint.id != "" && n.StartsWith("CT_Paint")) { ms[k] = SkinMat(ms[k], paint.color, tex); changed = true; }
                    else if (trim.HasValue && n.StartsWith("CT_Trim")) { ms[k] = SkinMat(ms[k], trim.Value, null); changed = true; }
                    else if (rim.HasValue && n.StartsWith("CT_WheelRim")) { ms[k] = SkinMat(ms[k], rim.Value, null); changed = true; }
                }
                if (changed) r.sharedMaterials = ms;
            }
        }

        // Comprar o elegir una pieza de skin desde la pestaña Paint
        void PickSkin(int part, string id, int price, string key)
        {
            if (price > 0 && !OwnsSkin(key))
            {
                if (prof.coins < price) { Toast($"You need {price} coins"); return; }
                prof.coins -= price; prof.ownedSkins.Add(key); Toast("Unlocked: " + Skins.NameOf(key));
            }
            SetSkin(choice, part, id); UpdateShowroom(); RefreshCarPick();
        }

        // Pestaña Paint de la selección de vehículo
        void BuildPaintTab(Transform body)
        {
            var cur = SkinOf(choice);
            LayoutText(body, "Paint", 32, Ink, 44);
            var g1 = Grid(body, new Vector2(84, 84));
            foreach (var p in Skins.Paints)
            {
                var pp = p; bool own = PaintOwned(p), sel = cur[0] == p.id;
                var cell = Img(g1, sel ? Ink : Color.white, null, p.name);
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => PickSkin(0, pp.id, pp.price, "p:" + pp.id));
                var inner = R("Muestra", cell.transform); Fill(inner, 7, 7, 7, 7);
                if (p.pattern != null) { var ri = inner.gameObject.AddComponent<RawImage>(); ri.texture = PatternTex(p.pattern); ri.uvRect = new Rect(0, 0, 0.5f, 0.5f); ri.raycastTarget = false; }
                else { var im = inner.gameObject.AddComponent<Image>(); im.color = p.id == "" ? new Color(0.9f, 0.88f, 0.83f) : p.color; im.raycastTarget = false; if (p.id == "") { var t = T(inner, "—", 30, Muted); Fill(t.rectTransform); } }
                if (!own) PriceTag(cell.transform, p.price);
            }
            LayoutText(body, "Details", 32, Ink, 44);
            ColorRow(body, cur[1], id => PickSkin(1, id, 0, ""));
            LayoutText(body, "Wheels", 32, Ink, 44);
            var g2 = Grid(body, new Vector2(190, 84));
            foreach (var w in Skins.Wheels)
            {
                var ww = w; bool own = WheelOwned(w), sel = cur[2] == w.id;
                var cell = Img(g2, sel ? Ink : Color.white, null, w.name);
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => PickSkin(2, ww.id, ww.price, "w:" + ww.id));
                var t = T(cell.transform, w.name, 28, sel ? Cream : Ink); Fill(t.rectTransform, 6, own ? 4 : 30, 6, 4);
                if (!own) PriceTag(cell.transform, w.price);
            }
            LayoutText(body, "Rims", 32, Ink, 44);
            ColorRow(body, cur[3], id => PickSkin(3, id, 0, ""));
        }
        RectTransform Grid(Transform p, Vector2 cell)
        {
            var r = R("Rejilla", p); var g = r.gameObject.AddComponent<GridLayoutGroup>(); g.cellSize = cell; g.spacing = new Vector2(8, 8); g.childAlignment = TextAnchor.UpperCenter;
            r.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return r;
        }
        void ColorRow(Transform p, string cur, System.Action<string> pick)
        {
            var g = Grid(p, new Vector2(84, 84));
            foreach (var (id, c) in Skins.Colors)
            {
                var cid = id; var cell = Img(g, cur == id ? Ink : Color.white, null, id == "" ? "Fabrica" : id);
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => pick(cid));
                var inner = Img(cell.transform, id == "" ? new Color(0.9f, 0.88f, 0.83f) : c, null, "Color"); Fill(inner.rectTransform, 9, 9, 9, 9); inner.raycastTarget = false;
                if (id == "") { var t = T(inner.transform, "—", 30, Muted); Fill(t.rectTransform); }
            }
        }
        void PriceTag(Transform cell, int price)
        {
            var tag = Img(cell, SunC, null, "Precio"); var r = tag.rectTransform; r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0);
            r.offsetMin = new Vector2(4, 4); r.offsetMax = new Vector2(-4, 34); tag.raycastTarget = false;
            var t = T(tag.transform, price.ToString(), 24, Ink); Fill(t.rectTransform); t.font = fontBlack;
        }

        // Cofres de oro: a veces traen un estampado o unas ruedas que aún no tienes
        string ChestSkin()
        {
            var pool = Skins.Lockables().Where(k => !prof.ownedSkins.Contains(k)).ToList();
            if (pool.Count == 0) return null;
            var key = pool[Random.Range(0, pool.Count)]; prof.ownedSkins.Add(key);
            return Skins.NameOf(key);
        }
    }
}
