using System.Collections.Generic;
using UnityEngine;

namespace CozyTrack
{
    public class CarDef
    {
        public string name, role, cat; public Color color; public int[] st;
        public CarDef(string n, string r, string c, string hex, int v, int a, int g, int w)
        { name = n; role = r; cat = c; ColorUtility.TryParseHtmlString(hex, out color); st = new[] { v, a, g, w }; }
    }

    public class SurfDef
    {
        public string id, n; public float grip = 1, roll = 1, boost = 1, scrub = 1; public Color fx; public bool hasFx;
        public SurfDef(string id, string n, float grip, float roll, float boost, float scrub, string fx = null)
        { this.id = id; this.n = n; this.grip = grip; this.roll = roll; this.boost = boost; this.scrub = scrub; hasFx = fx != null; if (hasFx) ColorUtility.TryParseHtmlString(fx, out this.fx); }
    }

    public class ObstDef { public string type; public int i; public float u, period; public ObstDef(string t, int i, float u, float p) { type = t; this.i = i; this.u = u; period = p; } }

    public class CircuitDef
    {
        public string id, n, desc, room;
        public string[] layout, surf;
        public int[][] boxes;   // {pieza, carril} con u = 0,5
        public int[][] coins;   // {pieza, carril}
        public ObstDef[] obst = new ObstDef[0];
        public float grip = 1f;
    }

    public class LevelKey
    {
        public int level; public string world, name, rival; public float skill, grip; public int target; public string[] lap;
        public LevelKey(int l, string w, string n, string r, float s, float g, int t, string[] lap) { level = l; world = w; name = n; rival = r; skill = s; grip = g; target = t; this.lap = lap; }
    }

    public class TodDef
    {
        public string id, n, air; public Color bg, fog, sky, gnd, sun, ground; public float sunI; public Vector3 sunDir; public bool lamps;
    }

    public static class CozyData
    {
        public static readonly CarDef[] Cars =
        {
            new CarDef("Dart", "Formula car", "Racing", "#ff7366", 5, 2, 3, 2),
            new CarDef("Hopper", "Little hatchback", "Street", "#ffd64d", 3, 5, 2, 1),
            new CarDef("Gecko", "Buggy", "Racing", "#73e0a6", 2, 4, 5, 2),
            new CarDef("Rhino", "Off-roader", "Work", "#4080f2", 3, 1, 4, 5),
            new CarDef("Cart", "Shopping cart", "Everyday", "#73bfff", 2, 4, 3, 2),
            new CarDef("EScooter", "E-scooter", "Everyday", "#73e0a6", 3, 5, 2, 1),
            new CarDef("Skate", "Giant roller skate", "Everyday", "#ff8ccc", 3, 4, 4, 1),
            new CarDef("Skateboard", "Skateboard", "Everyday", "#ff9e40", 4, 4, 3, 1),
            new CarDef("Bike", "Bike", "Everyday", "#ff7366", 4, 3, 2, 1),
            new CarDef("Trike", "Trike", "Everyday", "#ffd64d", 2, 4, 4, 1),
            new CarDef("Stroller", "Stroller", "Everyday", "#b38cf2", 2, 3, 4, 2),
            new CarDef("Wheelbarrow", "Wheelbarrow", "Everyday", "#66bf59", 1, 3, 4, 3),
            new CarDef("Bathtub", "Bathtub on wheels", "Everyday", "#aab6cc", 3, 2, 3, 4),
            new CarDef("Taxi", "Taxi", "Street", "#ffd64d", 3, 3, 3, 3),
            new CarDef("Bus", "Bus", "Street", "#ff7366", 2, 1, 3, 5),
            new CarDef("IceCream", "Ice cream van", "Street", "#ff8ccc", 2, 2, 3, 4),
            new CarDef("FireTruck", "Fire truck", "Street", "#ff7366", 3, 2, 3, 5),
            new CarDef("Ambulance", "Ambulance", "Street", "#aab6cc", 3, 3, 3, 4),
            new CarDef("Camper", "Camper van", "Street", "#73e0a6", 2, 2, 3, 4),
            new CarDef("GolfCart", "Golf cart", "Street", "#aab6cc", 2, 4, 4, 2),
            new CarDef("Tractor", "Tractor", "Work", "#66bf59", 1, 2, 5, 5),
            new CarDef("Digger", "Digger", "Work", "#ffd64d", 1, 2, 4, 5),
            new CarDef("Forklift", "Forklift", "Work", "#ff9e40", 1, 4, 4, 4),
            new CarDef("Mower", "Ride-on mower", "Work", "#ff7366", 2, 3, 4, 3),
            new CarDef("Moto", "Racing bike", "Racing", "#734dbf", 5, 4, 2, 2),
            new CarDef("Vespino", "Classic scooter", "Racing", "#73bfff", 3, 4, 3, 2),
            new CarDef("Quad", "Quad", "Racing", "#ff9e40", 3, 4, 4, 2),
            new CarDef("Monster", "Monster truck", "Racing", "#b38cf2", 3, 3, 3, 5),
            new CarDef("Rocket", "Rocket car", "Racing", "#1f1f40", 5, 3, 2, 3),
            new CarDef("Bumper", "Bumper car", "Racing", "#73bfff", 2, 4, 3, 4),
            new CarDef("Sled", "Sled", "Snow", "#ff7366", 4, 3, 1, 2),
            new CarDef("Snowmobile", "Snowmobile", "Snow", "#73bfff", 4, 4, 3, 3),
            new CarDef("Hover", "Hovercraft", "Water", "#ff9e40", 4, 3, 1, 2),
            new CarDef("Train", "Toy train", "Rails", "#4080f2", 3, 2, 4, 5),
            new CarDef("Tram", "Tram", "Rails", "#ffd64d", 3, 2, 4, 5),
            new CarDef("HotDog", "Hot dog car", "Everyday", "#ffd64d", 3, 3, 3, 3),
        };
        static Dictionary<string, CarDef> carMap;
        public static CarDef Car(string n)
        {
            if (carMap == null) { carMap = new Dictionary<string, CarDef>(); foreach (var c in Cars) carMap[c.name] = c; }
            return carMap.TryGetValue(n, out var d) ? d : Cars[0];
        }
        public static readonly string[] StatNames = { "Speed", "Acceleration", "Grip", "Weight" };
        public static readonly string[] StartCars = { "Dart", "Hopper", "Gecko", "Rhino" };

        public static readonly Dictionary<string, SurfDef> Surfs = new Dictionary<string, SurfDef>
        {
            { "asfalto", new SurfDef("asfalto", "Asphalt", 1f, 1f, 1f, 1f) },
            { "turbo", new SurfDef("turbo", "Turbo carpet", 1f, 1f, 1.35f, 1f, "#ffb04d") },
            { "hielo", new SurfDef("hielo", "Ice", 0.72f, 0.6f, 1f, 0.35f, "#e6f7ff") },
            { "arena", new SurfDef("arena", "Sand", 0.95f, 3.5f, 1f, 1.5f, "#e8c98f") },
            { "barro", new SurfDef("barro", "Mud", 0.85f, 2.6f, 1f, 1.3f, "#7a5232") },
            { "agua", new SurfDef("agua", "Puddle", 0.8f, 2.0f, 1f, 0.8f, "#9fd8ff") },
            { "jabon", new SurfDef("jabon", "Soap", 0.68f, 0.5f, 1f, 0.3f, "#ffffff") },
        };

        static readonly string[] CH = { "C45R", "C45L", "C45L", "C45R" }, CHm = { "C45L", "C45R", "C45R", "C45L" };
        static string[] Cat(params object[] parts)
        {
            var l = new List<string>();
            foreach (var p in parts) { if (p is string s) l.Add(s); else l.AddRange((string[])p); }
            return l.ToArray();
        }

        public static readonly CircuitDef[] Circuits =
        {
            new CircuitDef { id = "jardin", n = "Garden", room = "jardin", desc = "The classic: chicanes, turbo carpet, ice, sand, mud and a puddle.",
                layout = Cat("SF", "S8", "C90L", CH, "C90L", "S8", "S8", "C90L", CH, "C90L"),
                surf = new[] { "asfalto","turbo","asfalto","hielo","hielo","hielo","hielo","asfalto","arena","barro","asfalto","agua","agua","agua","agua","asfalto" },
                boxes = new[] { new[]{1,0}, new[]{1,3}, new[]{8,1}, new[]{8,2}, new[]{9,0}, new[]{9,3} },
                coins = new[] { new[]{2,1}, new[]{2,2}, new[]{10,3}, new[]{10,0}, new[]{15,1}, new[]{7,2} } },
            new CircuitDef { id = "salon", n = "Living room", room = "salon", desc = "On the rug, by the sofa. The robot vacuum crosses the track.",
                layout = Cat("SF", "C90L", CH, "C90L", "S8", "C90L", CHm, "C90L"),
                surf = new[] { "asfalto","asfalto","asfalto","turbo","turbo","asfalto","asfalto","arena","asfalto","asfalto","barro","barro","asfalto","asfalto" },
                obst = new[] { new ObstDef("roomba", 7, 0.5f, 7f) } },
            new CircuitDef { id = "cocina", n = "Kitchen", room = "cocina", desc = "A figure 8 with a level crossing: look both ways in the middle. The cat crosses too.",
                layout = new[] { "S5.3","X5.4","S5.3","C90L","C90L","C90L","S5.3","X5.4","S5.3","C90R","C90R","C90R" },
                surf = new[] { "asfalto","asfalto","asfalto","asfalto","agua","asfalto","turbo","asfalto","asfalto","hielo","hielo","asfalto" },
                obst = new[] { new ObstDef("cat", 4, 0.5f, 9f) } },
            new CircuitDef { id = "bano", n = "Bathroom", room = "bano", desc = "A loop over the ceiling: the track twists and you race upside down. Mind the soap and the duck.",
                layout = new[] { "SF","S8","S8","C90L","C90L","Ti8","Tr8","To8","C90L","C90L" },
                surf = new[] { "asfalto","asfalto","jabon","asfalto","agua","asfalto","turbo","asfalto","jabon","asfalto" },
                obst = new[] { new ObstDef("duck", 2, 0.5f, 8f) } },
        };
        public static CircuitDef Circuit(string id) { foreach (var c in Circuits) if (c.id == id) return c; return Circuits[0]; }

        // ---------- niveles (tools/progression.py): 7 niveles clave; los intermedios usan el trazado del clave anterior ----------
        public static readonly LevelKey[] LevelKeys =
        {
            new LevelKey(1, "Playroom", "First lap", "Rhino", 0.85f, 1.0f, 0, new[] { "S8", "S8", "C90L8", "S4", "C90L8", "S8", "S8", "C90L8", "S4", "C90L8" }),
            new LevelKey(5, "Playroom", "Zigzag", "Gecko", 0.865f, 1.0f, 1, new[] { "S8", "S8", "S8", "C90L8", "C45R8", "C45L8", "C90L8", "S8", "S8", "S8", "C90L8", "C45R8", "C45L8", "C90L8" }),
            new LevelKey(10, "Playroom", "The rug", "Dart", 0.885f, 1.0f, 2, new[] { "S8", "S8", "C90L8", "C45R8", "C45L8", "C45L8", "C45R8", "C90L8", "S8", "S8", "S8", "C90L8", "C45R8", "C45L8", "C45L8", "C45R8", "C90L8", "S8" }),
            new LevelKey(15, "Garden", "Flower pots", "Rhino", 0.925f, 1.0f, 2, new[] { "S8", "S8", "S8", "C90L5", "S4", "C90R8", "C90L5", "C90L5", "S8", "S8", "S8", "S8", "C90L5", "S4", "C90R8", "C90L5", "C90L5", "S8" }),
            new LevelKey(20, "Garden", "The hedge", "Hopper", 0.915f, 1.0f, 3, new[] { "S8", "S8", "S8", "C90L5", "C45R5", "C45L5", "C90L5", "C90R8", "C90L5", "S8", "S8", "S8", "S8", "C90L5", "C45R5", "C45L5", "C90L5", "C90R8", "C90L5", "S8" }),
            new LevelKey(25, "Snow", "Hairpins", "Rhino", 0.887f, 0.85f, 4, new[] { "S8", "S8", "S8", "C90L3", "C90L3", "S8", "S8", "C45R5", "C45L5", "S8", "S8", "S8", "C90L3", "C90L3", "S8", "S8", "C45R5", "C45L5" }),
            new LevelKey(30, "Snow", "The big freeze", "Gecko", 0.89f, 0.85f, 5, new[] { "S8", "S8", "S8", "S4", "C90L3", "C90L3", "C90R5", "C90L5", "C45L5", "C45R5", "S8", "S8", "S8", "S8", "S8", "S4", "C90L3", "C90L3", "C90R5", "C90L5", "C45L5", "C45R5", "S8", "S8" }),
        };
        public const int LevelCount = 30;
        public static CircuitDef Level(int n, out LevelKey key, out float skill)
        {
            key = LevelKeys[0]; LevelKey next = null;
            foreach (var k in LevelKeys) { if (k.level <= n) key = k; else if (next == null) next = k; }
            skill = key.skill;
            if (next != null && n > key.level) skill = Mathf.Lerp(key.skill, next.skill, (n - key.level) / (float)(next.level - key.level));
            var lap = new List<string>(key.lap);
            lap[0] = "SF";
            string room = key.world == "Garden" ? "jardin" : key.world == "Snow" ? "nieve" : "salon";
            var surf = new string[lap.Count];
            for (int i = 0; i < surf.Length; i++) surf[i] = key.grip < 1f ? "hielo" : "asfalto";
            if (surf.Length > 2) surf[1] = "turbo";
            return new CircuitDef { id = "nivel" + n, n = $"Level {n} · {key.name}", room = room, desc = key.world, layout = lap.ToArray(), surf = surf, grip = 1f,
                obst = room == "salon" && n >= 8 ? new[] { new ObstDef("roomba", Mathf.Min(3, lap.Count - 1), 0.5f, 7f) } : new ObstDef[0] };
        }

        // ---------- mejoras (herramienta de balanceo): % totales con las 5 mejoras, reparto decreciente ----------
        public static readonly string[] UpgradeNames = { "Speed", "Accel", "Brakes" };
        public static readonly float[] UpgradeTotal = { 20f, 33f, 40f };
        public static readonly float[] UpgradeShape = { 0.30f, 0.25f, 0.20f, 0.15f, 0.10f };
        public static readonly int[] UpgradeCost = { 60, 110, 180, 280, 420 };
        public static float UpSum(int stat, int lvl) { float s = 0; for (int k = 0; k < lvl && k < 5; k++) s += UpgradeShape[k]; return s * UpgradeTotal[stat] / 100f; }

        // ---------- objetos (mecánicas de Mario Kart con nombre y aspecto propios) ----------
        public static readonly Dictionary<string, string[]> Items = new Dictionary<string, string[]>
        {
            { "turbo", new[]{"»","Turbo"} }, { "turbo3", new[]{"»»»","Triple turbo"} }, { "shield", new[]{"◯","Shield"} },
            { "marble", new[]{"●","Marble"} }, { "homing", new[]{"✸","Spinning top"} }, { "comet", new[]{"★","Comet"} },
            { "oil", new[]{"◐","Oil"} }, { "fakebox", new[]{"?!","Trick box"} }, { "blackout", new[]{"ϟ","Blackout"} },
            { "autopilot", new[]{"➤","Autopilot"} }, { "super", new[]{"✦","Supercharge"} }, { "bubbles", new[]{"∘∘","Bubbles"} }, { "magnet", new[]{"∪","Magnet"} },
        };
        public static readonly (string, int)[][] ItemOdds =
        {
            new[] { ("oil",25), ("fakebox",20), ("shield",20), ("marble",15), ("magnet",10), ("turbo",10) },
            new[] { ("turbo",20), ("marble",20), ("homing",20), ("oil",10), ("bubbles",15), ("turbo3",10), ("shield",5) },
            new[] { ("homing",20), ("turbo3",20), ("bubbles",15), ("comet",10), ("blackout",10), ("super",10), ("turbo",15) },
            new[] { ("autopilot",20), ("super",20), ("comet",15), ("blackout",15), ("turbo3",20), ("homing",10) },
        };

        public static readonly string[] StickerIds = { "estrella", "corazon", "rayo", "flor", "sonrisa", "huella", "arcoiris", "planeta", "llama", "siete" };
        public static readonly string[] StickerNames = { "Star", "Heart", "Bolt", "Flower", "Smile", "Paw", "Rainbow", "Planet", "Flame", "Seven" };

        public static readonly string[] DailyGoals = { "Win the race", "Finish without leaving the track", "Collect 10 coins", "Make 3 perfect curves", "Finish in the top 2" };

        static Color H(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static readonly TodDef[] Tods =
        {
            new TodDef { id = "manana", n = "Morning", bg = H("#bfe6d0"), fog = H("#bfe6d0"), sky = H("#fffaf0"), gnd = H("#7fb08f"), sun = H("#fff4dd"), sunI = 1.0f, sunDir = new Vector3(50, -25, 0), ground = H("#a8d8b9"), air = "polen" },
            new TodDef { id = "atardecer", n = "Sunset", bg = H("#f7b49a"), fog = H("#f4a88e"), sky = H("#ffd9b8"), gnd = H("#8a6a8a"), sun = H("#ffab6b"), sunI = 1.15f, sunDir = new Vector3(22, -50, 0), ground = H("#b9cf9a"), air = "hojas" },
            new TodDef { id = "noche", n = "Night", bg = H("#1b2142"), fog = H("#1b2142"), sky = H("#8fa0e6"), gnd = H("#2a3050"), sun = H("#9db4ff"), sunI = 0.35f, sunDir = new Vector3(55, 140, 0), ground = H("#33475a"), air = "luciernagas", lamps = true },
        };
        public static TodDef Tod(string id) { foreach (var t in Tods) if (t.id == id) return t; return Tods[1]; }

        public static readonly Color Ink = H("#2B2340"), Cream = H("#FFF4E0"), Coral = H("#FF6B4A"), Sun = H("#FFCF3D"), Sky = H("#73bfff"), Mint = H("#a8d8b9"), Grape = H("#734dbf");
        public static Color Hex(string h) => H(h);
    }
}
