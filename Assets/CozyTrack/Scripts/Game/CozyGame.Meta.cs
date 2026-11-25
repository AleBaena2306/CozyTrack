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
        public List<string> skinCar = new List<string>(), skinVal = new List<string>();   // skin por vehículo: pintura|detalles|ruedas|llantas
        public List<string> ownedSkins = new List<string>();                              // estampados y ruedas desbloqueados («p:…», «w:…»)
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
            foreach (var c in prof.chests) c.kind = TierOf(c.kind ?? "");
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

        // ---------- cofres: 4 ranuras y 4 niveles (common 5 min, rare 10, epic 20, legendary 30); un anuncio los abre ya ----------
        //   common: 2 piezas · rare: 3 piezas y quizá una pegatina · epic: 4 piezas, pegatina y quizá una skin · legendary: un vehículo entero, pegatina y skin
        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        static string TierOf(string kind) => kind == "oro" ? "legendary" : kind == "normal" ? "common" : kind;
        bool AddChest(string kind)
        {
            kind = TierOf(kind);
            foreach (var s in prof.chests) if (s.kind == "") { s.kind = kind; s.opening = false; s.readyAt = 0; SaveProfile(); return true; }
            prof.coins += 25; SaveProfile(); return false;   // ranuras llenas: monedas
        }
        void TapChest(int k)
        {
            var s = prof.chests[k]; if (s.kind == "") return;
            if (s.opening && Now >= s.readyAt) { var tier = TierOf(s.kind); var loot = OpenChest(tier); s.kind = ""; s.opening = false; SaveProfile(); ShowChestReward(tier, loot); return; }
            if (!s.opening)
            {
                if (prof.chests.Any(o => o.opening && Now < o.readyAt)) { Toast("Another chest is already opening"); return; }
                s.opening = true; s.readyAt = Now + ChestSeconds(TierOf(s.kind)); SaveProfile();
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
        // Botín de un cofre: cada línea «título|subtítulo|color de fondo»
        List<string> OpenChest(string tier)
        {
            var outL = new List<string>();
            int parts = tier == "legendary" ? 0 : tier == "epic" ? 4 : tier == "rare" ? 3 : 2;
            if (tier == "legendary")
            {
                // un vehículo entero que aún no tienes (el que más piezas tenga)
                var pool = CozyData.Cars.Select(c => c.name).Where(v => !prof.owned.Contains(v)).OrderByDescending(Pieces).ToList();
                if (pool.Count > 0) { var v = pool[0]; while (Pieces(v) < 3) AddPiece(v); prof.owned.Add(v); outL.Add($"{v}!|NEW VEHICLE|#FFE07A"); }
                else { prof.coins += 150; outL.Add("+150 coins|ALBUM COMPLETE|#FFE07A"); }
            }
            for (int k = 0; k < parts; k++)
            {
                var pool = CozyData.Cars.Select(c => c.name).Where(v => !prof.owned.Contains(v)).ToList();
                if (pool.Count == 0) { prof.coins += 40; outL.Add("+40 coins|ALBUM COMPLETE|#FFE07A"); continue; }
                var v2 = pool[UnityEngine.Random.Range(0, pool.Count)];
                AddPiece(v2);
                if (Pieces(v2) >= 3) { prof.owned.Add(v2); outL.Add($"{v2}!|NEW VEHICLE|#A8E6C4"); }
                else outL.Add($"{v2} piece|{Pieces(v2)} / 3 PIECES|#FFF4E0");
            }
            float stickerOdds = tier == "common" ? 0.25f : tier == "rare" ? 0.6f : 1f;
            if (UnityEngine.Random.value < stickerOdds)
            {
                var sp = CozyData.StickerIds.Where(s => !prof.stickers.Contains(s)).ToList();
                if (sp.Count > 0) { var s = sp[UnityEngine.Random.Range(0, sp.Count)]; prof.stickers.Add(s); outL.Add($"{CozyData.StickerNames[Array.IndexOf(CozyData.StickerIds, s)]} sticker|FOR YOUR ROOF|#C9B8F2"); }
                else { prof.coins += 15; outL.Add("+15 coins|REPEATED STICKER|#FFE07A"); }
            }
            float skinOdds = tier == "legendary" ? 1f : tier == "epic" ? 0.5f : 0f;
            if (UnityEngine.Random.value < skinOdds) { var sk = ChestSkin(); if (sk != null) outL.Add($"{sk}|NEW SKIN · PAINT TAB|#FFC6D9"); }
            int coinsWon = UnityEngine.Random.Range(20, 41) * (tier == "legendary" ? 4 : tier == "epic" ? 3 : tier == "rare" ? 2 : 1); prof.coins += coinsWon; outL.Add($"+{coinsWon} coins|COINS|#FFE07A");
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
        //   cofres: ganar una carrera o quedar entre los 2 primeros en Survival → common · superar un nivel → rare (cada 3 niveles, epic)
        //   desafío diario → rare (cada 7 días seguidos, legendary)
        void Finish()
        {
            if (mode == "online") { OnlineFinish(); return; }
            state = "end";
            var act = cars.Where(c => !c.isOut).OrderBy(c => c.done >= 0 ? c.done : 1e9f).ThenByDescending(Prog).ToList();
            var order = act.Concat(cars.Where(c => c.isOut).OrderByDescending(c => c.outAt)).ToList();
            int place = order.IndexOf(player) + 1;
            bool beat = rival == null || order.IndexOf(player) < order.IndexOf(rival);
            string chestKind = null; var v = new ResultView { big = place + $"<size={Mathf.RoundToInt(24 * K)}>{Ordinal(place)}</size>" };
            prof.coins += player.coins; OfferDouble(player.coins);
            foreach (var (c, k) in order.Select((c, k) => (c, k)))
                v.rows.Add(((k + 1).ToString(), c.isPlayer ? NetName + " (you)" : c.name, c.isOut ? "out lap " + c.outLap : c.done >= 0 ? Fmt(c.done) : "—", c.isPlayer));
            v.stats.Add((player.coins.ToString(), "COINS")); v.stats.Add((perfects.ToString(), "PERFECT CURVES")); v.stats.Add((crashes.ToString(), "OFF THE TRACK"));
            if (mode == "time")
            {
                float t = player.done; v.big = Fmt(t); v.rows.Clear();
                bool had = PlayerPrefs.HasKey(GhostKey); float best = ghostData != null ? ghostTime : 1e9f;
                bool isRec = !had || t < best;
                if (isRec) PlayerPrefs.SetString(GhostKey, JsonUtility.ToJson(new GhostSave { t = t, car = choice, s = rec.ToArray() }));
                v.sub = isRec ? "NEW RECORD!" : "TIME TRIAL"; v.disc = isRec ? MintB : Paper;
                v.rows.Add(("", "This run", Fmt(t), true));
                if (had) { v.rows.Add(("", "Old best (ghost)", Fmt(best), false)); v.rows.Add(("", isRec ? "Improved by" : "Behind by", (isRec ? "−" : "+") + Mathf.Abs(best - t).ToString("0.00") + " s", false)); }
                v.tip = isRec ? (had ? "Your ghost has been updated. Next time you race against this run." : "First record saved: next time you race against your ghost.") : $"Your record is {Fmt(best)}. Try a cleaner line!";
                v.primary = isRec ? "BEAT IT AGAIN" : "TRY AGAIN"; v.back = "‹ LEVELS"; v.onBack = BackToPick; v.secondary = "DONE"; v.onSecondary = BackToPick;
                SaveProfile(); ShowResultView(v); return;
            }
            if (mode == "elim")
            {
                v.sub = player.isOut ? $"OUT ON LAP {player.outLap}" : "LAST ONE STANDING!"; v.disc = place == 1 ? SunC : place == 2 ? Peach : Paper;
                if (place <= 2) chestKind = "common";
                v.primary = "GO AGAIN"; v.primaryBg = Coral;
            }
            else if (mode == "daily")
            {
                var d = DailyDef();
                bool ok = d.goal == 0 ? place == 1 : d.goal == 1 ? crashes == 0 : d.goal == 2 ? player.coins >= 10 : d.goal == 3 ? perfects >= 3 : place <= 2;
                if (ok && prof.dailyLast != d.key)
                {
                    int streak = StreakNow() + 1; prof.dailyLast = d.key; prof.dailyStreak = streak;
                    chestKind = streak % 7 == 0 ? "legendary" : "rare";
                    v.big = $"DAY {streak}"; v.sub = "STREAK!"; v.disc = SunC;
                }
                else if (prof.dailyLast == d.key) { v.sub = "ALREADY DONE"; v.tip = "You already beat today's challenge. Come back tomorrow!"; v.disc = Paper; }
                else { v.sub = "NOT YET"; v.tip = $"Challenge: {CozyData.DailyGoals[d.goal].ToLower()}. You can try again today."; v.disc = Paper; }
                v.back = "‹ RACE"; v.onBack = BackToPick; v.primary = ok ? "GREAT!" : "TRY AGAIN"; if (ok) v.onPrimary = BackToPick;
            }
            else if (mode == "level")
            {
                v.back = "‹ LEVELS"; v.onBack = BackToPick;
                if (beat)
                {
                    if (levelNum > prof.level) prof.level = levelNum;
                    chestKind = levelNum % 3 == 0 ? "epic" : "rare";
                    v.sub = $"LEVEL {levelNum:00} CLEAR!"; v.disc = SunC;
                    if (levelNum < CozyData.LevelCount) { int nx = levelNum + 1; v.primary = "NEXT LEVEL"; v.onPrimary = () => { pickLevel = nx; pickTime = false; OpenCarPick(); }; v.secondary = "REPLAY"; v.onSecondary = () => StartLevel(levelNum); }
                }
                else
                {
                    v.sub = "SO CLOSE!"; v.disc = CozyData.Hex("#D3D2DA");
                    v.tip = "Win to clear the level. Upgrade your brakes, or let go a bit earlier before the big curves.";
                    v.primary = "TRY AGAIN"; v.secondary = "UPGRADE"; v.onSecondary = () => { pickLevel = levelNum; pickTime = false; carTab = "up"; OpenCarPick(); };
                }
            }
            else
            {
                v.sub = place == 1 ? "YOU WON!" : "NICE RACE!"; v.disc = place == 1 ? SunC : Paper;
                if (place == 1) chestKind = "common";
            }
            if (chestKind != null)
            {
                bool slot = AddChest(chestKind); v.chestTier = chestKind;
                v.chestTitle = TierName(chestKind) + " chest!"; v.chestSub = slot ? $"Added to your slots · opens in {ChestSeconds(chestKind) / 60} min" : "Slots full: +25 coins instead";
            }
            else if (v.tip == null && mode != "daily") v.tip = place == 1 ? "Keep winning to fill your chest slots." : "Win the race to earn a chest.";
            v.photo = true; photoReady = false;
            SaveProfile();
            ShowResultView(v);
            StartCoroutine(PodiumPhoto(order));
        }
        void BackToPick() { mode = "race"; QuitToPick(); }
        void QuitToPick() { if (circId.StartsWith("nivel")) BuildCircuit(CozyData.Circuit(PlayerPrefs.GetString("ctCirc", "jardin"))); MenuGrid(); ShowMenu(); OpenPick(pickMode == "elim" ? "elim" : "race"); }
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
            if (!Usable(n)) { Toast($"{3 - Pieces(n)} more pieces needed"); return; }
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
                var m = SpawnCar(top[k].name, holder, top[k].isPlayer);
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
            ShowPhoto(photoRT, string.Join("   ", top.Select((c, k) => $"{k + 1}{Ordinal(k + 1).ToLower()} {c.name}")), $"Cozy Track · {circ.n} · {DateTime.Now:dd/MM/yyyy}");
        }
    }
}
