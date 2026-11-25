using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyTrack
{
    [Serializable] public class ChestSlot { public string kind = ""; public long readyAt; public bool opening; }
    [Serializable] public class Profile
    {
        public int coins, level;                                   // level: último nivel superado
        public List<string> owned = new List<string>(CozyData.StartCars);
        public List<string> pieceCar = new List<string>(); public List<int> pieceN = new List<int>();
        public List<string> stickers = new List<string> { "estrella", "corazon" };
        public List<string> stickerCar = new List<string>(); public List<string> stickerId = new List<string>();
        public List<string> upCar = new List<string>(); public List<int> upVals = new List<int>();   // 3 valores por coche
        public List<ChestSlot> chests = new List<ChestSlot>();
        public string dailyLast = ""; public int dailyStreak;
    }

    public partial class CozyGame
    {
        Profile prof; bool allUnlocked = true;
        string levelRival = "Rhino";

        // ---------- perfil (guardado en PlayerPrefs) ----------
        void LoadProfile()
        {
            try { prof = JsonUtility.FromJson<Profile>(PlayerPrefs.GetString("ctProfile", "")); } catch { prof = null; }
            if (prof == null) prof = new Profile();
            while (prof.chests.Count < 4) prof.chests.Add(new ChestSlot());
            allUnlocked = PlayerPrefs.GetInt("ctAllUnlocked", 1) == 1;
        }
        void SaveProfile() { PlayerPrefs.SetString("ctProfile", JsonUtility.ToJson(prof)); PlayerPrefs.Save(); }
        bool Usable(string n) => allUnlocked || prof.owned.Contains(n);
        int Pieces(string n) { int k = prof.pieceCar.IndexOf(n); return k < 0 ? 0 : prof.pieceN[k]; }
        void AddPiece(string n) { int k = prof.pieceCar.IndexOf(n); if (k < 0) { prof.pieceCar.Add(n); prof.pieceN.Add(1); } else prof.pieceN[k]++; }
        string StickerOf(string car) { int k = prof.stickerCar.IndexOf(car); return k < 0 ? null : prof.stickerId[k]; }
        void SetSticker(string car, string id)
        {
            int k = prof.stickerCar.IndexOf(car);
            if (k >= 0) { prof.stickerCar.RemoveAt(k); prof.stickerId.RemoveAt(k); }
            if (!string.IsNullOrEmpty(id)) { prof.stickerCar.Add(car); prof.stickerId.Add(id); }
            SaveProfile();
        }
        int[] Upgrades(string car) { int k = prof.upCar.IndexOf(car); return k < 0 ? new int[3] : new[] { prof.upVals[k * 3], prof.upVals[k * 3 + 1], prof.upVals[k * 3 + 2] }; }
        bool BuyUpgrade(string car, int stat)
        {
            int k = prof.upCar.IndexOf(car);
            if (k < 0) { prof.upCar.Add(car); prof.upVals.AddRange(new[] { 0, 0, 0 }); k = prof.upCar.Count - 1; }
            int lvl = prof.upVals[k * 3 + stat]; if (lvl >= 5) return false;
            int cost = CozyData.UpgradeCost[lvl]; if (prof.coins < cost) return false;
            prof.coins -= cost; prof.upVals[k * 3 + stat] = lvl + 1; SaveProfile(); return true;
        }

        // ---------- cofres: 4 ranuras, se abren con tiempo (5 min normal, 30 min de oro) o con un anuncio ----------
        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool AddChest(string kind)
        {
            foreach (var s in prof.chests) if (s.kind == "") { s.kind = kind; s.opening = false; s.readyAt = 0; SaveProfile(); return true; }
            prof.coins += 25; SaveProfile(); return false;   // ranuras llenas: monedas
        }
        void TapChest(int k)
        {
            var s = prof.chests[k]; if (s.kind == "") return;
            if (s.opening && Now >= s.readyAt) { ShowChestReward(OpenChest(s.kind)); s.kind = ""; s.opening = false; SaveProfile(); return; }
            if (!s.opening)
            {
                if (prof.chests.Any(o => o.opening && Now < o.readyAt)) { Toast("Ya se está abriendo otro cofre"); return; }
                s.opening = true; s.readyAt = Now + (s.kind == "oro" ? 30 * 60 : 5 * 60); SaveProfile();
            }
        }
        void ChestAd(int k)
        {
            var s = prof.chests[k]; if (s.kind == "") return;
            StartCoroutine(FakeAd(() => { s.opening = true; s.readyAt = Now; SaveProfile(); TapChest(k); }));
        }
        IEnumerator FakeAd(Action done)
        {
            // anuncio de prueba: en la build de publisher lo sustituye su SDK (Assets/KovusPride/Publishing)
            ShowAdOverlay(true); yield return new WaitForSecondsRealtime(2f); ShowAdOverlay(false); done();
        }
        List<string> OpenChest(string kind)
        {
            var outL = new List<string>(); int n = kind == "oro" ? 3 : 2;
            for (int k = 0; k < n; k++)
            {
                var pool = CozyData.Cars.Select(c => c.name).Where(v => !prof.owned.Contains(v)).ToList();
                if (pool.Count == 0) { prof.coins += 40; outL.Add("40 monedas (álbum completo)"); continue; }
                var v2 = pool[UnityEngine.Random.Range(0, pool.Count)];
                AddPiece(v2);
                if (Pieces(v2) >= 3) { prof.owned.Add(v2); outL.Add("¡Vehículo nuevo: " + CozyData.Car(v2).role + "!"); }
                else outL.Add($"Pieza de {CozyData.Car(v2).role} ({Pieces(v2)}/3)");
            }
            if (UnityEngine.Random.value < (kind == "oro" ? 0.75f : 0.4f))
            {
                var sp = CozyData.StickerIds.Where(s => !prof.stickers.Contains(s)).ToList();
                if (sp.Count > 0) { var s = sp[UnityEngine.Random.Range(0, sp.Count)]; prof.stickers.Add(s); outL.Add("Pegatina: " + CozyData.StickerNames[Array.IndexOf(CozyData.StickerIds, s)]); }
                else { prof.coins += 15; outL.Add("15 monedas (pegatina repetida)"); }
            }
            int coinsWon = UnityEngine.Random.Range(20, 41) + (kind == "oro" ? 40 : 0); prof.coins += coinsWon; outL.Add(coinsWon + " monedas");
            SaveProfile(); return outL;
        }

        // ---------- pegatinas en el techo ----------
        void ApplySticker(GameObject g, string car)
        {
            string id = StickerOf(car); if (id == null || !g) return;
            var rs = g.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float s = Mathf.Min(b.size.x, b.size.z) * 0.6f;
            var q = Prim(PrimitiveType.Quad, Color.white, Vector3.zero, Vector3.one * s, g.transform, 0, StickerMat(id));
            q.name = "Pegatina";
            q.transform.position = new Vector3(b.center.x, b.max.y + 0.015f, b.center.z);
            q.transform.rotation = Quaternion.Euler(90, g.transform.eulerAngles.y, 0);
            q.transform.localScale = Vector3.one * s / Mathf.Max(0.001f, g.transform.lossyScale.x);
        }
        readonly Dictionary<string, Material> stickerMats = new Dictionary<string, Material>();
        Material StickerMat(string id) => stickerMats.TryGetValue(id, out var m) ? m : stickerMats[id] = Mat(Color.white, CozyTex.Get("sticker_" + id), true);

        // ---------- contrarreloj con fantasma ----------
        GameObject ghost; float[] ghostData; float ghostTime;
        string GhostKey => "ctGhost_" + circId;
        [Serializable] class GhostSave { public float t; public string car; public float[] s; }
        bool LoadGhost()
        {
            ghostData = null;
            var json = PlayerPrefs.GetString(GhostKey, ""); if (json == "") return false;
            var g = JsonUtility.FromJson<GhostSave>(json); if (g == null || g.s == null || g.s.Length < 9) return false;
            ghostData = g.s; ghostTime = g.t;
            ghost = Spawn("Car_" + g.car, null);
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int k = 0; k < ms.Length; k++) { var c = ms[k].GetColor("_BaseColor"); c.a = 0.35f; ms[k] = Mat(c, ms[k].GetTexture("_BaseMap"), true); }
                r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            ghostK = 0; return true;
        }
        int ghostK;
        void RecordGhost() { rec.Add((float)Math.Round(raceT, 2)); rec.Add(Prog(player)); rec.Add(player.o); }
        void UpdateGhost()
        {
            if (!ghost || ghostData == null) return;
            var s = ghostData; int n = s.Length / 3;
            while (ghostK < n - 2 && s[(ghostK + 1) * 3] <= raceT) ghostK++;
            int a = ghostK * 3, b = Mathf.Min(n - 1, ghostK + 1) * 3;
            float f = s[b] > s[a] ? Mathf.Clamp01((raceT - s[a]) / (s[b] - s[a])) : 0;
            float pr = Mathf.Lerp(s[a + 1], s[b + 1], f), o = Mathf.Lerp(s[a + 2], s[b + 2], f);
            int i = ((Mathf.FloorToInt(pr) % Track.N) + Track.N) % Track.N;
            var w = Track.World(i, pr - Mathf.Floor(pr), o);
            ghost.transform.position = w.pos; ghost.transform.rotation = Track.Rot(w);
            ghost.SetActive(state != "count" && raceT < s[(n - 1) * 3] + 1);
        }

        // ---------- desafío diario (igual para todos ese día: circuito, vehículo, hora y objetivo salen de la fecha) ----------
        static string DayKey(DateTime d) => d.ToString("yyyy-MM-dd");
        (string circ, string car, string tod, int goal, string key) DailyDef()
        {
            string key = DayKey(DateTime.Now); uint h = 2166136261;
            foreach (char ch in key) { h ^= ch; h *= 16777619; }
            float R() { h = h * 1103515245u + 12345u; return (h >> 8) / 16777216f; }
            var cs = CozyData.Circuits; var vs = CozyData.Cars; var ts = CozyData.Tods;
            return (cs[(int)(R() * cs.Length)].id, vs[(int)(R() * vs.Length)].name, ts[(int)(R() * ts.Length)].id, (int)(R() * CozyData.DailyGoals.Length), key);
        }
        int StreakNow() => prof.dailyLast == DayKey(DateTime.Now) || prof.dailyLast == DayKey(DateTime.Now.AddDays(-1)) ? prof.dailyStreak : 0;
        void StartDaily()
        {
            var d = DailyDef(); mode = "daily";
            if (circId != d.circ) BuildCircuit(CozyData.Circuit(d.circ));
            ApplyTod(d.tod); choice = d.car; StartRace();
        }
        void StartLevel(int n)
        {
            var def = CozyData.Level(n, out var key, out float skill);
            levelNum = n; levelRival = key.rival; levelSkill = skill; mode = "level";
            BuildCircuit(def);
            if (!Usable(choice)) choice = CozyData.StartCars[0];
            StartRace();
        }

        // ---------- final de carrera ----------
        void Finish()
        {
            state = "end";
            var act = cars.Where(c => !c.isOut).OrderBy(c => c.done >= 0 ? c.done : 1e9f).ThenByDescending(Prog).ToList();
            var order = act.Concat(cars.Where(c => c.isOut).OrderByDescending(c => c.outAt)).ToList();
            int place = order.IndexOf(player) + 1;
            bool beat = rival == null || order.IndexOf(player) < order.IndexOf(rival);
            string msg, big = place + "º"; List<string> chest = null; string chestKind = null;
            if (mode == "time")
            {
                float t = player.done; big = Fmt(t);
                bool had = PlayerPrefs.HasKey(GhostKey); float best = ghostData != null ? ghostTime : 1e9f;
                if (!had || t < best)
                {
                    PlayerPrefs.SetString(GhostKey, JsonUtility.ToJson(new GhostSave { t = t, car = choice, s = rec.ToArray() }));
                    msg = had ? $"¡Nuevo récord! Mejoras en {best - t:0.00} s. Tu fantasma se ha actualizado." : "Primer récord guardado: la próxima vez correrás contra tu fantasma.";
                }
                else msg = $"Récord: {Fmt(best)}. Te has quedado a {t - best:0.00} s.";
            }
            else if (player.isOut) msg = $"Te han eliminado en la vuelta {player.outLap}.";
            else msg = place == 1 ? "¡Has ganado!" : $"Has quedado {place}º. ¡A por la victoria!";
            prof.coins += player.coins;
            if (mode == "daily")
            {
                var d = DailyDef();
                bool ok = d.goal == 0 ? place == 1 : d.goal == 1 ? crashes == 0 : d.goal == 2 ? player.coins >= 10 : d.goal == 3 ? perfects >= 3 : place <= 2;
                if (ok && prof.dailyLast != d.key)
                {
                    int streak = StreakNow() + 1; prof.dailyLast = d.key; prof.dailyStreak = streak;
                    chestKind = streak % 7 == 0 ? "oro" : "normal";
                    msg = $"¡Desafío diario superado! Racha: {streak} {(streak == 1 ? "día" : "días")}.";
                }
                else if (prof.dailyLast == d.key) msg = "Ya habías superado el desafío de hoy. Vuelve mañana.";
                else msg = $"Desafío no superado: {CozyData.DailyGoals[d.goal].ToLower()}. Puedes repetirlo hoy.";
            }
            else if (mode == "level")
            {
                if (beat) { if (levelNum > prof.level) prof.level = levelNum; chestKind = "normal"; msg = $"¡Nivel {levelNum} superado!" + (levelNum < CozyData.LevelCount ? " Se desbloquea el siguiente." : ""); }
                else msg = "¡Casi! Mejora el coche o prueba otra trazada.";
            }
            else if (mode != "time" && (place == 1 || (mode == "race" && beat))) chestKind = "normal";
            if (chestKind != null) chest = new List<string> { AddChest(chestKind) ? $"Cofre {(chestKind == "oro" ? "de oro" : "normal")} a la ranura: ábrelo en el menú." : "Ranuras llenas: +25 monedas." };
            SaveProfile();
            var rows = new List<string>();
            for (int k = 0; k < order.Count; k++) { var c = order[k]; rows.Add($"{k + 1}º {c.name}\t{(c.isOut ? "fuera (vuelta " + c.outLap + ")" : c.done >= 0 ? Fmt(c.done) : "—")}"); }
            rows.Add($"Monedas\t{player.coins}"); rows.Add($"Curvas perfectas\t{perfects}"); rows.Add($"Salidas de pista\t{crashes}");
            ShowResults(big, msg, rows, chest);
            if (mode != "time") StartCoroutine(PodiumPhoto(order));
        }
        public static string Fmt(float t) => $"{Mathf.FloorToInt(t / 60)}:{(t % 60):00.0}";

        // ---------- garaje: estantería con toda la colección ----------
        class Slot { public string n; public GameObject m; public bool own; }
        readonly List<Slot> garageSlots = new List<Slot>(); Transform garageRoot; Vector3 garageView;
        void OpenGarage()
        {
            state = "garage"; BuildGarage(); foreach (var c in cars) if (c.g) c.g.SetActive(false);
            ShowScreen("garage"); RefreshGarageUI();
        }
        void CloseGarage() { state = "menu"; if (garageRoot) Destroy(garageRoot.gameObject); garageSlots.Clear(); MenuGrid(); ShowMenu(); }
        void BuildGarage()
        {
            if (garageRoot) Destroy(garageRoot.gameObject);
            garageRoot = new GameObject("Garaje").transform; garageRoot.position = new Vector3(200, 0, 0); garageSlots.Clear();
            int cols = 4, rows = Mathf.CeilToInt(CozyData.Cars.Length / (float)cols); float sw = 2.7f, sh = 1.75f;
            Prim(PrimitiveType.Cube, Color.white, new Vector3(0, rows * sh / 2 + 0.3f, 1.3f), new Vector3(cols * sw + 2, rows * sh + 2, 0.3f), garageRoot, 0, Mat(CozyData.Hex("#ffe9c7")));
            var wood = Mat(CozyData.Hex("#c98f5a"), null, false, false, null, 1);
            for (int r = 0; r < rows; r++) Prim(PrimitiveType.Cube, Color.white, new Vector3(0, r * sh + 0.25f, 0), new Vector3(cols * sw + 0.8f, 0.14f, 1.9f), garageRoot, 0, wood);
            foreach (var x in new[] { -1, 1 }) Prim(PrimitiveType.Cube, Color.white, new Vector3(x * (cols * sw / 2 + 0.55f), rows * sh / 2, 0), new Vector3(0.3f, rows * sh + 0.6f, 1.9f), garageRoot, 0, Mat(CozyData.Hex("#b47c4a"), null, false, false, null, 1));
            var sil = Mat(CozyData.Hex("#4a4860"), null, false, false, null, 0);
            for (int k = 0; k < CozyData.Cars.Length; k++)
            {
                string n = CozyData.Cars[k].name; int col = k % cols, row = rows - 1 - k / cols; bool own = Usable(n);
                var holder = new GameObject(n).transform; holder.SetParent(garageRoot, false);
                holder.localPosition = new Vector3((col - (cols - 1) / 2f) * sw, row * sh + 0.32f, -0.1f);
                var m = Spawn("Car_" + n, holder, own, own); ApplySticker(m, n);
                var rs = m.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float sc = Mathf.Min(2.1f / Mathf.Max(b.size.x, b.size.z), 1.25f / Mathf.Max(0.01f, b.size.y), 1);
                holder.localScale = Vector3.one * sc;
                if (!own) foreach (var r in rs) { var ms = r.sharedMaterials; for (int j = 0; j < ms.Length; j++) ms[j] = sil; r.sharedMaterials = ms; }
                var bc = holder.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(2.4f, 1.5f, 2.4f) / sc; bc.center = Vector3.up * 0.6f / sc;
                garageSlots.Add(new Slot { n = n, m = holder.gameObject, own = own });
            }
            float tan = Mathf.Tan(21 * Mathf.Deg2Rad);
            garageView = new Vector3(0, rows * sh / 2 + 0.3f, Mathf.Max(rows * sh / 2 / tan, (cols * sw / 2 + 0.8f) / (tan * cam.aspect)) + 3);
        }
        void PickGarage(Vector2 screen)
        {
            if (!Physics.Raycast(cam.ScreenPointToRay(screen), out var hit, 200)) return;
            var n = hit.collider.gameObject.name;
            if (!Usable(n)) { Toast($"Te faltan {3 - Pieces(n)} piezas"); return; }
            choice = n; RefreshGarageUI(); RefreshCards();
        }

        // ---------- foto de final de carrera (podio) ----------
        Transform podiumRoot; RenderTexture photoRT; Camera photoCam;
        IEnumerator PodiumPhoto(List<RaceCar> order)
        {
            if (podiumRoot) Destroy(podiumRoot.gameObject);
            podiumRoot = new GameObject("Podio").transform; podiumRoot.position = new Vector3(-200, 0, 0);
            float[] H = { 1.1f, 0.75f, 0.45f }, X = { 0, -2.7f, 2.7f }; string[] C = { "#ffd64d", "#dfe3ee", "#e8a874" };
            Prim(PrimitiveType.Cube, Color.white, new Vector3(0, 3, 2.5f), new Vector3(14, 7, 0.3f), podiumRoot, 0, Mat(CozyData.Tod(todId).bg));
            Prim(PrimitiveType.Cube, Color.white, new Vector3(0, -0.25f, 0), new Vector3(16, 0.1f, 10), podiumRoot, 0, Mat(CozyData.Tod(todId).ground));
            var top = order.Where(c => !c.isOut).Concat(order.Where(c => c.isOut)).Take(3).ToList();
            for (int k = 0; k < top.Count; k++)
            {
                Prim(PrimitiveType.Cube, Color.white, new Vector3(X[k], H[k] / 2 - 0.2f, 0), new Vector3(2.5f, H[k], 2.3f), podiumRoot, 0, Mat(CozyData.Hex(C[k]), null, false, false, null, 1));
                var holder = new GameObject(top[k].name).transform; holder.SetParent(podiumRoot, false);
                holder.localPosition = new Vector3(X[k], H[k] - 0.2f, 0); holder.localRotation = Quaternion.Euler(0, 180 + (k == 0 ? -20 : k == 1 ? -40 : 6), 0);
                var m = Spawn("Car_" + top[k].name, holder, true, true); ApplySticker(m, top[k].name);
            }
            string[] cc = { "#ff7366", "#ffd64d", "#73bfff", "#73e0a6", "#b38cf2", "#ff8ccc" };
            for (int k = 0; k < 70; k++)
            {
                var p = Prim(PrimitiveType.Quad, Color.white, new Vector3((UnityEngine.Random.value - 0.5f) * 9, 0.5f + UnityEngine.Random.value * 4.5f, (UnityEngine.Random.value - 0.5f) * 3), new Vector3(0.14f, 0.22f, 1), podiumRoot, 0, Mat(CozyData.Hex(cc[k % 6]), null, false, true, null, 0));
                p.transform.rotation = UnityEngine.Random.rotation;
            }
            if (!photoRT) photoRT = new RenderTexture(960, 690, 24);
            if (!photoCam) { photoCam = new GameObject("Camara foto").AddComponent<Camera>(); photoCam.enabled = false; }
            photoCam.fieldOfView = 46; photoCam.targetTexture = photoRT; photoCam.clearFlags = CameraClearFlags.SolidColor; photoCam.backgroundColor = CozyData.Tod(todId).bg;
            photoCam.transform.position = new Vector3(-200, 2.4f, -8.4f); photoCam.transform.LookAt(new Vector3(-200, 1.1f, 0));
            var oldSun = sun.transform.rotation; sun.transform.rotation = Quaternion.Euler(40, 20, 0);
            photoCam.enabled = true;
            yield return null; yield return new WaitForEndOfFrame();
            photoCam.enabled = false; sun.transform.rotation = oldSun;
            ShowPhoto(photoRT, string.Join("   ", top.Select((c, k) => $"{k + 1}º {c.name}")), $"Cozy Track · {circ.n} · {DateTime.Now:dd/MM/yyyy}");
        }
    }
}
