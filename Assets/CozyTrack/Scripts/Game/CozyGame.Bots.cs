using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CozyTrack
{
    // ============================================================================================
    // Bots de prueba (solo se activan con el argumento -cozybots; el juego normal no los usa nunca).
    //
    // GitHub Actions compila el juego real para Linux y lo lanza sin pantalla:
    //   CozyTrack -batchmode -nographics -cozybots -botPlayers 12 -botHours 10 -botOut informe
    // Cientos de jugadores simulados de tres perfiles (novato, casual, experto) juegan con las mismas
    // reglas, física, IA, niveles, cofres, monedas y mejoras que una persona; solo cambia quién pulsa.
    // Al terminar se escribe report.md (en español), races.csv y alerts.txt, y el juego se cierra.
    // ============================================================================================
    public partial class CozyGame
    {
        bool botRun;
        static long botClock;                                     // reloj simulado (segundos UNIX) para los cofres

        class BotPersona
        {
            public string id, name;
            public float k, kNoise, react, mistake, mistakeOver, startMean, startSd, laneRate, adRate, buyRate, grind, menuSec, switchCar;
            public bool drafts, smartItems, avoid;
        }
        static readonly BotPersona[] BotPersonas =
        {
            new BotPersona { id = "novato", name = "Novato (6-8 años)", k = 0.80f, kNoise = 0.06f, react = 0.38f, mistake = 0.12f, mistakeOver = 0.16f,
                startMean = 0.2f, startSd = 0.9f, laneRate = 0.25f, adRate = 0.35f, buyRate = 0.4f, grind = 0.1f, menuSec = 40, switchCar = 0.4f, drafts = false, smartItems = false, avoid = false },
            new BotPersona { id = "casual", name = "Casual", k = 0.87f, kNoise = 0.04f, react = 0.27f, mistake = 0.05f, mistakeOver = 0.13f,
                startMean = 0.7f, startSd = 0.5f, laneRate = 0.6f, adRate = 0.5f, buyRate = 0.8f, grind = 0.25f, menuSec = 30, switchCar = 0.15f, drafts = false, smartItems = false, avoid = true },
            new BotPersona { id = "experto", name = "Experto", k = 0.95f, kNoise = 0.02f, react = 0.16f, mistake = 0.012f, mistakeOver = 0.1f,
                startMean = 0.35f, startSd = 0.2f, laneRate = 1f, adRate = 0.7f, buyRate = 1f, grind = 0.35f, menuSec = 20, switchCar = 0f, drafts = true, smartItems = true, avoid = true },
        };

        class BotRace
        {
            public string persona, phase, mode, car, world; public int bot, level, upgrades, place, cars;
            public bool beat, dnf, perfectStart, falseStart, closeFinish, blowout, hopeless;
            public float time, gap, simMin; public int crashes, perfects, saves, hits, items, overtakes, passedBy, leadChanges, coins;
            public float drift, leadShare;
        }
        class BotPlayer
        {
            public BotPersona p; public int id; public float simSec; public int attempts, failStreak, races, adsToday; public long dayStart;
            public bool done, stuck; public int stuckLevel; public string startCar;
            public Dictionary<int, int> attemptsAt = new Dictionary<int, int>(); public Dictionary<int, float> clearedMin = new Dictionary<int, float>();
            public Dictionary<int, int> coinsAt = new Dictionary<int, int>(), upgradesAt = new Dictionary<int, int>(), carsAt = new Dictionary<int, int>();
            public int chestsEarned, chestsLost, chestsOpened, adsWatched, carSwitches, coinsEarned, coinsSpent;
        }

        readonly List<BotRace> botRaces = new List<BotRace>();
        readonly List<BotPlayer> botPlayers = new List<BotPlayer>();
        readonly Queue<Action> botJobs = new Queue<Action>();
        readonly Dictionary<string, int> botLogs = new Dictionary<string, int>();
        BotPlayer botCur; BotPersona botP; string botOut = "bot-report"; int botPerPersona = 12; float botHours = 10; int botSeed = 1234;
        float botCurveK, botItemT, botLaneT; bool botMist; int botCurveI = -1; readonly long botStartT = System.Diagnostics.Stopwatch.GetTimestamp();
        const float BotDt = 1f / 60f;

        static string Arg(string name, string def)
        {
            var a = Environment.GetCommandLineArgs();
            for (int k = 0; k < a.Length - 1; k++) if (a[k] == name) return a[k + 1];
            return def;
        }

        void BotInit()
        {
            botPerPersona = int.Parse(Arg("-botPlayers", "12"));
            botHours = float.Parse(Arg("-botHours", "10"), CultureInfo.InvariantCulture);
            botSeed = int.Parse(Arg("-botSeed", "1234"));
            botOut = Arg("-botOut", "bot-report");
            int carReps = int.Parse(Arg("-botCarReps", "2")), elimRaces = int.Parse(Arg("-botElim", "30"));
            UnityEngine.Random.InitState(botSeed);
            Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0;
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (type == LogType.Log) return;
                string key = type + ": " + msg.Split('\n')[0];
                if (key.Length > 220) key = key.Substring(0, 220);
                botLogs[key] = botLogs.TryGetValue(key, out var n) ? n + 1 : 1;
            };
            PlayerPrefs.DeleteAll();
            botClock = 1_800_000_000;
            Debug.Log($"[CozyBots] inicio: {botPerPersona} jugadores por perfil, máximo {botHours} h simuladas cada uno");

            // 1) progresión completa: cada bot empieza de cero y juega hasta acabar los 30 niveles o atascarse
            int id = 0;
            foreach (var p in BotPersonas)
                for (int k = 0; k < botPerPersona; k++)
                {
                    var bp = new BotPlayer { p = p, id = ++id, startCar = CozyData.StartCars[k % CozyData.StartCars.Length] };
                    botPlayers.Add(bp);
                    botJobs.Enqueue(() => BotBeginPlayer(bp));
                }
            // 2) equilibrio de vehículos: contrarreloj de cada coche sin mejoras en un circuito de cada mundo
            foreach (var car in CozyData.Cars)
                foreach (int lv in new[] { 3, 13, 23, 29 })
                    foreach (var p in new[] { BotPersonas[1], BotPersonas[2] })
                        for (int r = 0; r < carReps; r++)
                        {
                            string cn = car.name; int l = lv; var pp = p;
                            botJobs.Enqueue(() => BotSoloRace(pp, "coches", "time", cn, l));
                        }
            // 3) supervivencia con coches iniciales
            foreach (var p in BotPersonas)
                for (int r = 0; r < elimRaces; r++)
                {
                    string cn = CozyData.StartCars[r % 4]; int l = 1 + (r * 7) % CozyData.LevelCount; var pp = p;
                    botJobs.Enqueue(() => BotSoloRace(pp, "supervivencia", "elim", cn, l));
                }
        }

        // ---------- un fotograma = una tarea (normalmente una carrera completa) ----------
        void BotFrame()
        {
            if (botCur != null && !botCur.done) { try { BotProgressStep(botCur); } catch (Exception e) { BotLogEx(e); botCur.done = true; } return; }
            if (botJobs.Count == 0) { BotWriteReport(); Application.Quit(0); enabled = false; return; }
            try { botJobs.Dequeue()(); } catch (Exception e) { BotLogEx(e); }
            if (botRaces.Count % 100 == 0 && botRaces.Count > 0) Debug.Log($"[CozyBots] {botRaces.Count} carreras, quedan {botJobs.Count} tareas");
        }
        void BotLogEx(Exception e) { string key = "Exception: " + e.GetType().Name + ": " + e.Message + " @ " + (e.StackTrace ?? "").Split('\n').FirstOrDefault()?.Trim(); botLogs[key] = botLogs.TryGetValue(key, out var n) ? n + 1 : 1; }

        void BotFreshProfile()
        {
            prof = new Profile(); while (prof.chests.Count < 4) prof.chests.Add(new ChestSlot());
            allUnlocked = false; assistOn = true;
        }

        void BotBeginPlayer(BotPlayer bp)
        {
            BotFreshProfile();
            botCur = bp; botP = bp.p; bp.dayStart = botClock;
            choice = bp.startCar;
        }

        void BotProgressStep(BotPlayer bp)
        {
            int next = prof.level + 1;
            if (next > CozyData.LevelCount) { bp.done = true; Debug.Log($"[CozyBots] {bp.p.id} #{bp.id} terminó el juego en {bp.simSec / 3600f:0.0} h"); return; }
            if (bp.simSec > botHours * 3600) { bp.done = true; return; }
            if (bp.attempts >= 25) { bp.done = true; bp.stuck = true; bp.stuckLevel = next; Debug.Log($"[CozyBots] {bp.p.id} #{bp.id} atascado en el nivel {next}"); return; }

            BotBetweenRaces(bp);
            bool grind = bp.failStreak >= 3 && UnityEngine.Random.value < bp.p.grind;
            int coinsBefore = prof.coins;
            BotRace r;
            if (grind)
            {
                BuildCircuit(CozyData.Level(Mathf.Max(1, next - 1), out _, out _)); mode = "race";
                r = BotRunRace("progresion", next);
            }
            else
            {
                if (!bp.attemptsAt.ContainsKey(next)) bp.attemptsAt[next] = 0;
                bp.attemptsAt[next]++; bp.attempts++;
                var def = CozyData.Level(next, out var key, out float skill);
                levelNum = next; levelRival = key.rival; levelSkill = skill; mode = "level"; BuildCircuit(def);
                r = BotRunRace("progresion", next);
                if (r.beat && !r.dnf)
                {
                    bp.clearedMin[next] = bp.simSec / 60f; bp.coinsAt[next] = prof.coins;
                    bp.upgradesAt[next] = prof.upVals.Sum(); bp.carsAt[next] = prof.owned.Count;
                    bp.attempts = 0; bp.failStreak = 0;
                }
                else bp.failStreak++;
            }
            bp.races++;
            bp.coinsEarned += Mathf.Max(0, prof.coins - coinsBefore);
            r.bot = bp.id; r.simMin = bp.simSec / 60f;
            float spent = r.time + bp.p.menuSec + 3.2f;
            bp.simSec += spent; botClock += (long)spent;
        }

        // ---------- entre carreras: cofres, anuncios, mejoras y coche ----------
        void BotBetweenRaces(BotPlayer bp)
        {
            if (botClock - bp.dayStart > 86400) { bp.dayStart = botClock; bp.adsToday = 0; }
            // abrir los cofres listos
            foreach (var s in prof.chests)
                if (s.kind != "" && s.opening && Now >= s.readyAt) { OpenChest(TierOf(s.kind)); s.kind = ""; s.opening = false; bp.chestsOpened++; }
            // anuncio para abrir uno ya (máx. 8 al día)
            var waiting = prof.chests.Where(s => s.kind != "" && !(s.opening && Now < s.readyAt)).ToList();
            if (waiting.Count > 0 && bp.adsToday < 8 && UnityEngine.Random.value < bp.p.adRate)
            {
                var s = waiting.OrderByDescending(c => ChestSeconds(TierOf(c.kind))).First();
                OpenChest(TierOf(s.kind)); s.kind = ""; s.opening = false; bp.chestsOpened++; bp.adsToday++; bp.adsWatched++;
                bp.simSec += 30; botClock += 30;
            }
            // empezar a abrir el mejor si no hay ninguno abriéndose
            if (!prof.chests.Any(s => s.kind != "" && s.opening))
            {
                var s = prof.chests.Where(c => c.kind != "").OrderByDescending(c => ChestSeconds(TierOf(c.kind))).FirstOrDefault();
                if (s != null) { s.opening = true; s.readyAt = Now + ChestSeconds(TierOf(s.kind)); }
            }
            // coche nuevo
            var newest = prof.owned.LastOrDefault();
            if (newest != null && newest != choice && !CozyData.StartCars.Contains(newest) && UnityEngine.Random.value < bp.p.switchCar * 0.3f) { choice = newest; bp.carSwitches++; }
            // mejoras
            bool lostLast = bp.failStreak > 0;
            if (UnityEngine.Random.value < bp.p.buyRate && (lostLast || bp.p.id == "experto" || UnityEngine.Random.value < 0.3f))
            {
                for (int guard = 0; guard < 6; guard++)
                {
                    var up = Upgrades(choice);
                    int stat = bp.p.id == "novato" ? UnityEngine.Random.Range(0, 3) : new[] { 2, 1, 0 }.OrderBy(s => up[s]).First();
                    int before = prof.coins;
                    if (!BuyUpgrade(choice, stat)) break;
                    bp.coinsSpent += before - prof.coins;
                    if (bp.p.id != "experto") break;
                }
            }
        }

        // ---------- carreras sueltas (equilibrio de coches y supervivencia) ----------
        void BotSoloRace(BotPersona p, string phase, string m, string car, int level)
        {
            BotFreshProfile(); allUnlocked = true; botP = p; botCur = null;
            choice = car; mode = m;
            var def = CozyData.Level(level, out var key, out float skill);
            levelNum = level; levelRival = key.rival; levelSkill = skill; BuildCircuit(def);
            PlayerPrefs.DeleteKey(GhostKey);
            var r = BotRunRace(phase, level);
            r.bot = 0;
        }

        // ---------- una carrera completa, paso a paso con dt fijo ----------
        BotRace BotRunRace(string phase, int level)
        {
            CozyData.Level(level, out var key, out _);
            var r = new BotRace { persona = botP.id, phase = phase, mode = mode, car = choice, level = level, world = key.world, upgrades = Upgrades(choice).Sum() };
            StartRace();
            r.cars = cars.Count;
            botCurveI = -1; botItemT = 1; botLaneT = 1;
            // cuenta atrás: el bot pulsa cuando le parece (salida perfecta si pulsa en los últimos 0,6 s)
            float press = botP.startMean + Gauss() * botP.startSd; bool pressed = false;
            countGas = false; pressedAt = 99;
            while (state == "count")
            {
                countT -= BotDt;
                if (!pressed && countT <= press) { pressed = true; pressedAt = countT; }
                countGas = pressed;
                if (countT <= 0)
                {
                    state = "race";
                    if (countGas && pressedAt <= 0.6f) { player.turbo = 0.8f; r.perfectStart = true; }
                    else if (countGas && pressedAt > 1.4f) { player.spin = 0.5f; r.falseStart = true; }
                }
            }
            int lastPos = BotPos(player), leader = -1; float leadT = 0, lastSlip = 0, slipPeak = 0, lastSpin = player.spin; string lastItem = null;
            while (state == "race")
            {
                BotThink(BotDt);
                RaceTick(BotDt);
                UpdateObstacles(BotDt);
                if (state != "race" && state != "end") break;
                if (raceT > 420) { r.dnf = true; break; }
                if (player == null) break;
                int pos = BotPos(player);
                if (pos < lastPos) r.overtakes += lastPos - pos; else if (pos > lastPos) r.passedBy += pos - lastPos;
                lastPos = pos;
                var lead = cars.Where(c => !c.isOut).OrderByDescending(Prog).FirstOrDefault();
                int li = lead != null ? cars.IndexOf(lead) : -1;
                if (leader >= 0 && li != leader && raceT > 3) r.leadChanges++;
                leader = li; if (lead == player) leadT += BotDt;
                if (player.slip > 0.03f) r.drift += BotDt;
                if (player.slip > 0) slipPeak = Mathf.Max(slipPeak, player.slip);
                if (player.slip == 0 && lastSlip > 0) { if (slipPeak >= 0.4f && player.crash <= 0) r.saves++; slipPeak = 0; }
                if (player.crash > 0) slipPeak = 0;
                if (player.spin > lastSpin + 0.3f || player.slip > lastSlip + 0.5f) r.hits++;
                if (lastItem != null && player.item == null) r.items++;
                lastSlip = player.slip; lastSpin = player.spin; lastItem = player.item;
            }
            r.time = raceT; r.crashes = crashes; r.perfects = perfects; r.coins = player.coins; r.leadShare = raceT > 0 ? leadT / raceT : 0;
            // puesto y diferencias (si el jugador acaba antes que otros, se estima su tiempo por el progreso)
            var order = cars.Where(c => !c.isOut).OrderBy(c => c.done >= 0 ? c.done : 1e9f).ThenByDescending(Prog).Concat(cars.Where(c => c.isOut).OrderByDescending(c => c.outAt)).ToList();
            r.place = order.IndexOf(player) + 1;
            r.beat = rival == null || order.IndexOf(player) < order.IndexOf(rival);
            float pace = Prog(player) > 1 ? raceT / Prog(player) : 1;
            float Est(RaceCar c) => c.done >= 0 ? c.done : raceT + (Prog(player) - Prog(c)) * pace;
            if (mode != "time" && cars.Count > 1 && !player.isOut)
            {
                var other = mode == "level" ? rival : order.FirstOrDefault(c => c != player);
                if (r.place == 1 && mode != "level") other = order.Count > 1 ? order[1] : null;
                if (other != null && player.done >= 0) r.gap = Est(other) - player.done;   // + = el jugador va delante
                r.closeFinish = Mathf.Abs(r.gap) < 1f; r.blowout = r.gap > 5f; r.hopeless = r.gap < -5f;
            }
            botRaces.Add(r);
            return r;
        }

        int BotPos(RaceCar c) => cars.Where(x => !x.isOut).OrderByDescending(Prog).ToList().IndexOf(c) + 1;
        static float Gauss() { float u1 = 1f - UnityEngine.Random.value, u2 = UnityEngine.Random.value; return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2); }

        // ---------- el "dedo" del bot: acelerar/soltar como lo haría una persona de su perfil ----------
        bool BotThrottle(RaceCar c, float dt)
        {
            // en cada curva nueva decide su margen (y si esta vez se le va la mano)
            int ci = -1; for (int k = 0; k < 3; k++) { int i = (c.i + k) % Track.N; if (!Track.Pieces[i].straight) { ci = i; break; } }
            if (ci != botCurveI && ci >= 0)
            {
                botCurveI = ci; botMist = UnityEngine.Random.value < botP.mistake;
                botCurveK = Mathf.Clamp(botP.k + Gauss() * botP.kNoise + (botMist ? botP.mistakeOver : 0), 0.6f, 1.2f);
            }
            float kk = botCurveK * (c.bubbleT > 0 ? 0.85f : 1);
            float target = c.vtop * 2;   // en recta, a fondo
            float r = Track.Radius(c.i, c.o);
            if (r < float.PositiveInfinity) target = Mathf.Min(target, VSafe(c, c.i, c.o) * kk);
            var bt = BrakeTarget(c, kk);
            if (bt != null && bt.Value.brakeAt <= c.v * (botP.react + (botMist ? 0.25f : 0))) target = Mathf.Min(target, bt.Value.vt);
            return c.v < target;
        }

        void BotThink(float dt)
        {
            var c = player; if (c == null || c.crash > 0 || c.isOut) return;
            botLaneT -= dt;
            if (botLaneT <= 0)
            {
                botLaneT = (0.4f + UnityEngine.Random.value * 0.8f) / Mathf.Max(0.1f, botP.laneRate);
                bool straight = Track.Pieces[c.i].straight;
                foreach (var o in cars)
                {
                    if (o == c || o.isOut || Mathf.Abs(o.o - c.o) > 0.7f) continue;
                    float d = Prog(o) - Prog(c);
                    if (d > 0 && d < 0.35f && o.v < c.v) { BotFreeLane(c); break; }
                }
                if (botP.avoid) foreach (var ob in obstacles) if (ob.d.i == (c.i + 1) % Track.N && Mathf.Abs(ob.o - c.o) < 1.2f) ChangeLane(c, ob.o > c.o ? -1 : 1);
                if (botP.drafts && straight && !c.draft)
                    foreach (var o in cars)
                    {
                        if (o == c || o.isOut || Mathf.Abs(o.lane - c.lane) != 1) continue;
                        float d = Prog(o) - Prog(c); if (d > 0.1f && d < 0.4f && o.v > c.v * 0.9f) { ChangeLane(c, o.lane - c.lane); break; }
                    }
            }
            if (c.item != null)
            {
                botItemT -= dt; if (botItemT > 0) return;
                botItemT = botP.smartItems ? 0.3f : 0.5f + UnityEngine.Random.value * 2.5f;
                if (!botP.smartItems || BotGoodMoment(c, c.item)) UseItem(c);
            }
            else botItemT = botP.smartItems ? 0.2f : 0.5f + UnityEngine.Random.value * 3f;
        }
        void BotFreeLane(RaceCar c)
        {
            foreach (int d in UnityEngine.Random.value < 0.5f ? new[] { -1, 1 } : new[] { 1, -1 })
            {
                int t = c.lane + d; if (t < 0 || t >= Track.NL) continue;
                bool busy = cars.Any(o => o != c && !o.isOut && o.lane == t && Mathf.Abs(Prog(o) - Prog(c)) < 0.25f);
                if (!busy) { ChangeLane(c, d); return; }
            }
        }
        bool BotGoodMoment(RaceCar c, string it)
        {
            var next = NextCurve(c); bool longStraight = next == null || next.Value.d > 14;
            RaceCar ahead = NextAhead(c); float da = ahead != null ? Prog(ahead) - Prog(c) : 99;
            bool behindClose = cars.Any(o => o != c && !o.isOut && Prog(c) - Prog(o) > 0 && Prog(c) - Prog(o) < 0.3f);
            switch (it)
            {
                case "turbo": case "turbo3": return longStraight;
                case "marble": return ahead != null && da < 0.6f && Mathf.Abs(ahead.o - c.o) < 0.6f;
                case "homing": case "comet": case "blackout": case "bubbles": return ahead != null;
                case "oil": case "fakebox": return behindClose;
                case "shield": return behindClose || UnityEngine.Random.value < 0.05f;
                default: return true;   // autopilot, super, magnet
            }
        }

        // ============================== informe ==============================
        static string F1(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);
        static string F2(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);
        static string Pc(float v) => Mathf.RoundToInt(v * 100) + " %";
        static float Med(IEnumerable<float> xs) { var l = xs.OrderBy(x => x).ToList(); if (l.Count == 0) return float.NaN; return l.Count % 2 == 1 ? l[l.Count / 2] : (l[l.Count / 2 - 1] + l[l.Count / 2]) / 2; }
        static float Avg(IEnumerable<float> xs) { var l = xs.ToList(); return l.Count == 0 ? float.NaN : l.Average(); }
        static string Na(float v, Func<float, string> f) => float.IsNaN(v) ? "—" : f(v);

        void BotWriteReport()
        {
            Directory.CreateDirectory(botOut);
            var sb = new StringBuilder(); var alerts = new List<string>();
            var prog = botRaces.Where(r => r.phase == "progresion").ToList();
            float realMin = (float)((System.Diagnostics.Stopwatch.GetTimestamp() - botStartT) / (double)System.Diagnostics.Stopwatch.Frequency / 60.0);
            sb.AppendLine("# Cozy Track · informe de los bots de prueba");
            sb.AppendLine();
            sb.AppendLine($"{botRaces.Count} carreras simuladas en {F1(realMin)} min reales · {botPerPersona} jugadores por perfil · semilla {botSeed} · versión {Application.version}");
            sb.AppendLine();
            sb.AppendLine("Los bots juegan el juego real (misma física, IA, niveles, monedas, cofres y mejoras). Solo cambia quién pulsa: cada perfil tiene su precisión en las curvas, su tiempo de reacción, sus despistes y su forma de gastar.");
            sb.AppendLine();

            // ---------- progresión ----------
            var sec = new StringBuilder();
            sec.AppendLine("## 1. Progresión (30 niveles)");
            sec.AppendLine();
            sec.AppendLine("| Perfil | Acaban el juego | Atascados | Horas medianas hasta el final | Carreras medianas | Monedas ganadas/h | Cofres/h | Anuncios/h |");
            sec.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var p in BotPersonas)
            {
                var bs = botPlayers.Where(b => b.p == p).ToList(); if (bs.Count == 0) continue;
                var fin = bs.Where(b => b.clearedMin.ContainsKey(CozyData.LevelCount)).ToList();
                float hours = Avg(bs.Select(b => b.simSec / 3600f));
                sec.AppendLine($"| {p.name} | {fin.Count}/{bs.Count} | {bs.Count(b => b.stuck)} | {Na(Med(fin.Select(b => b.clearedMin[CozyData.LevelCount] / 60f)), F1)} | {Na(Med(bs.Select(b => (float)b.races)), x => F1(x))} | {Na(Avg(bs.Select(b => b.coinsEarned / Mathf.Max(0.1f, b.simSec / 3600f))), x => Mathf.RoundToInt(x).ToString())} | {Na(Avg(bs.Select(b => (b.chestsEarned) / Mathf.Max(0.1f, b.simSec / 3600f))), F1)} | {Na(Avg(bs.Select(b => b.adsWatched / Mathf.Max(0.1f, b.simSec / 3600f))), F1)} |");
                foreach (var g in bs.Where(b => b.stuck).GroupBy(b => b.stuckLevel))
                    alerts.Add($"**Muro:** {g.Count()} bot(s) «{p.name}» se atascan en el nivel {g.Key} (25 intentos sin superarlo).");
            }
            sec.AppendLine();
            sec.AppendLine("Intentos medianos para superar cada nivel y minutos de juego acumulados al superarlo (mediana). «Ganadas» = % de intentos en que gana al rival.");
            sec.AppendLine();
            sec.AppendLine("| Nivel | Mundo | " + string.Join(" | ", BotPersonas.Select(p => p.name + ": intentos · min · gana")) + " |");
            sec.AppendLine("|---|---|" + string.Join("|", BotPersonas.Select(_ => "---")) + "|");
            for (int lv = 1; lv <= CozyData.LevelCount; lv++)
            {
                CozyData.Level(lv, out var key, out _);
                var cells = new List<string>();
                foreach (var p in BotPersonas)
                {
                    var bs = botPlayers.Where(b => b.p == p && b.attemptsAt.ContainsKey(lv)).ToList();
                    var rs = prog.Where(r => r.persona == p.id && r.mode == "level" && r.level == lv).ToList();
                    if (bs.Count == 0) { cells.Add("—"); continue; }
                    float att = Med(bs.Select(b => (float)b.attemptsAt[lv])), mins = Med(bs.Where(b => b.clearedMin.ContainsKey(lv)).Select(b => b.clearedMin[lv]));
                    float win = rs.Count == 0 ? float.NaN : rs.Count(r => r.beat) / (float)rs.Count;
                    cells.Add($"{F1(att)} · {Na(mins, x => Mathf.RoundToInt(x).ToString())} · {Na(win, Pc)}");
                    if (p.id == "casual" && att >= 4) alerts.Add($"**Pico de dificultad:** el nivel {lv} pide {F1(att)} intentos (mediana) a un jugador casual.");
                    if (p.id == "experto" && att >= 2.5f) alerts.Add($"**Nivel duro incluso para expertos:** nivel {lv} ({F1(att)} intentos de mediana).");
                    if (p.id == "novato" && lv >= 4 && !float.IsNaN(win) && win > 0.9f) alerts.Add($"**Quizá demasiado fácil:** un novato gana el nivel {lv} en el {Pc(win)} de los intentos.");
                }
                sec.AppendLine($"| {lv} | {key.world} | {string.Join(" | ", cells)} |");
            }
            sec.AppendLine();

            // ---------- sensación de juego ----------
            sec.AppendLine("## 2. Sensación de carrera (game feel)");
            sec.AppendLine();
            sec.AppendLine("Media por carrera de niveles. «Final ajustado» = menos de 1 s con el rival; «paliza» = más de 5 s de ventaja; «sin opción» = más de 5 s detrás.");
            sec.AppendLine();
            sec.AppendLine("| Perfil | Mundo | Carreras | Salidas de pista | Curvas perfectas | Derrapes salvados | Adelantamientos | Le adelantan | Cambios de líder | Golpes recibidos | Objetos usados | % derrapando | Final ajustado | Paliza | Sin opción | Salida perfecta | Salida en falso |");
            sec.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var p in BotPersonas)
                foreach (var w in new[] { "Playroom", "Garden", "Snow" })
                {
                    var rs = prog.Where(r => r.persona == p.id && r.mode == "level" && r.world == w && !r.dnf).ToList(); if (rs.Count == 0) continue;
                    float crash = Avg(rs.Select(r => (float)r.crashes)), close = rs.Count(r => r.closeFinish) / (float)rs.Count, blow = rs.Count(r => r.blowout) / (float)rs.Count, hope = rs.Count(r => r.hopeless) / (float)rs.Count;
                    float lc = Avg(rs.Select(r => (float)r.leadChanges));
                    sec.AppendLine($"| {p.name} | {w} | {rs.Count} | {F2(crash)} | {F2(Avg(rs.Select(r => (float)r.perfects)))} | {F2(Avg(rs.Select(r => (float)r.saves)))} | {F1(Avg(rs.Select(r => (float)r.overtakes)))} | {F1(Avg(rs.Select(r => (float)r.passedBy)))} | {F1(lc)} | {F2(Avg(rs.Select(r => (float)r.hits)))} | {F1(Avg(rs.Select(r => (float)r.items)))} | {Pc(Avg(rs.Select(r => r.time > 0 ? r.drift / r.time : 0)))} | {Pc(close)} | {Pc(blow)} | {Pc(hope)} | {Pc(rs.Count(r => r.perfectStart) / (float)rs.Count)} | {Pc(rs.Count(r => r.falseStart) / (float)rs.Count)} |");
                    if (p.id == "casual" && crash > 1f) alerts.Add($"**Frustración:** un casual se sale {F2(crash)} veces por carrera en {w}.");
                    if (p.id == "novato" && crash > 2f) alerts.Add($"**Frustración infantil:** un novato se sale {F2(crash)} veces por carrera en {w}.");
                    if (close < 0.15f) alerts.Add($"**Poca emoción:** solo el {Pc(close)} de las carreras de «{p.name}» en {w} acaban ajustadas (<1 s).");
                    if (blow > 0.45f) alerts.Add($"**Carreras resueltas pronto:** «{p.name}» gana por más de 5 s el {Pc(blow)} de las veces en {w}.");
                    if (hope > 0.35f) alerts.Add($"**Carreras sin opción:** «{p.name}» pierde por más de 5 s el {Pc(hope)} de las veces en {w}.");
                    if (lc < 0.5f) alerts.Add($"**Poco movimiento:** media de {F1(lc)} cambios de líder por carrera para «{p.name}» en {w}.");
                }
            sec.AppendLine();

            // ---------- economía ----------
            sec.AppendLine("## 3. Economía y recompensas");
            sec.AppendLine();
            sec.AppendLine("| Perfil | Monedas por carrera | Monedas gastadas en mejoras | Mejoras compradas al final | Vehículos al final | Cofres ganados | Cofres perdidos por ranuras llenas | Cofres abiertos | Cambios de coche |");
            sec.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var p in BotPersonas)
            {
                var bs = botPlayers.Where(b => b.p == p).ToList(); if (bs.Count == 0) continue;
                int lastLv(BotPlayer b) => b.clearedMin.Count == 0 ? 0 : b.clearedMin.Keys.Max();
                float lost = bs.Sum(b => b.chestsLost) / Mathf.Max(1f, bs.Sum(b => b.chestsEarned + b.chestsLost));
                sec.AppendLine($"| {p.name} | {F1(Avg(bs.Select(b => b.coinsEarned / Mathf.Max(1f, b.races))))} | {Mathf.RoundToInt(Avg(bs.Select(b => (float)b.coinsSpent)))} | {F1(Avg(bs.Select(b => b.upgradesAt.TryGetValue(lastLv(b), out var u) ? u : 0f)))} | {F1(Avg(bs.Select(b => b.carsAt.TryGetValue(lastLv(b), out var c) ? c : 4f)))} | {F1(Avg(bs.Select(b => (float)b.chestsEarned)))} | {Pc(lost)} | {F1(Avg(bs.Select(b => (float)b.chestsOpened)))} | {F1(Avg(bs.Select(b => (float)b.carSwitches)))} |");
                if (lost > 0.25f) alerts.Add($"**Cofres desperdiciados:** «{p.name}» pierde el {Pc(lost)} de los cofres por tener las 4 ranuras llenas.");
            }
            sec.AppendLine();

            // ---------- coches ----------
            sec.AppendLine("## 4. Equilibrio de vehículos (contrarreloj de 2 vueltas, sin mejoras)");
            sec.AppendLine();
            var carRs = botRaces.Where(r => r.phase == "coches" && !r.dnf && r.time > 0).ToList();
            if (carRs.Count > 0)
            {
                var byLevel = carRs.GroupBy(r => (r.level, r.persona)).ToDictionary(g => g.Key, g => Med(g.Select(r => r.time)));
                var rows = new List<(string car, float rel, float crash, float relE, float relC)>();
                foreach (var g in carRs.GroupBy(r => r.car))
                {
                    float RelOf(string per) { var l = g.Where(r => r.persona == per).Select(r => r.time / byLevel[(r.level, per)] - 1).ToList(); return l.Count == 0 ? float.NaN : l.Average(); }
                    float relE = RelOf("experto"), relC = RelOf("casual");
                    rows.Add((g.Key, Avg(new[] { relE, relC }.Where(x => !float.IsNaN(x))), Avg(g.Where(r => r.persona == "casual").Select(r => (float)r.crashes)), relE, relC));
                }
                sec.AppendLine("Diferencia de tiempo con la mediana de todos los coches (negativo = más rápido) y salidas de pista por carrera de un casual.");
                sec.AppendLine();
                sec.AppendLine("| Vehículo | Experto | Casual | Salidas (casual) |");
                sec.AppendLine("|---|---|---|---|");
                foreach (var row in rows.OrderBy(x => x.rel))
                {
                    sec.AppendLine($"| {row.car} | {Na(row.relE, x => (x >= 0 ? "+" : "") + F1(x * 100) + " %")} | {Na(row.relC, x => (x >= 0 ? "+" : "") + F1(x * 100) + " %")} | {Na(row.crash, F2)} |");
                    if (!float.IsNaN(row.rel) && Mathf.Abs(row.rel) > 0.05f) alerts.Add($"**Vehículo desequilibrado:** {row.car} va un {(row.rel < 0 ? "" : "+")}{F1(row.rel * 100)} % respecto a la mediana.");
                    if (!float.IsNaN(row.crash) && row.crash > 1.2f) alerts.Add($"**Vehículo difícil de llevar:** {row.car} se sale {F2(row.crash)} veces por carrera con un casual.");
                }
            }
            sec.AppendLine();

            // ---------- supervivencia ----------
            sec.AppendLine("## 5. Supervivencia");
            sec.AppendLine();
            sec.AppendLine("| Perfil | Carreras | Gana | Top 2 (cofre) | Eliminado en la vuelta 1 | Salidas de pista |");
            sec.AppendLine("|---|---|---|---|---|---|");
            foreach (var p in BotPersonas)
            {
                var rs = botRaces.Where(r => r.phase == "supervivencia" && r.persona == p.id).ToList(); if (rs.Count == 0) continue;
                sec.AppendLine($"| {p.name} | {rs.Count} | {Pc(rs.Count(r => r.place == 1) / (float)rs.Count)} | {Pc(rs.Count(r => r.place <= 2) / (float)rs.Count)} | {Pc(rs.Count(r => r.place == r.cars) / (float)rs.Count)} | {F2(Avg(rs.Select(r => (float)r.crashes)))} |");
            }
            sec.AppendLine();

            // ---------- errores ----------
            sec.AppendLine("## 6. Errores y avisos del juego durante las pruebas");
            sec.AppendLine();
            var bad = botLogs.Where(kv => !kv.Key.Contains("RenderTexture") && !kv.Key.Contains("graphics device")).OrderByDescending(kv => kv.Value).Take(40).ToList();
            if (bad.Count == 0) sec.AppendLine("Ninguno.");
            else { foreach (var kv in bad) sec.AppendLine($"- ×{kv.Value} `{kv.Key.Replace("`", "'")}`"); }
            int exc = botLogs.Where(kv => kv.Key.StartsWith("Exception")).Sum(kv => kv.Value);
            if (exc > 0) alerts.Insert(0, $"**Errores de código:** {exc} excepciones durante las pruebas (ver sección 6).");
            int dnf = botRaces.Count(r => r.dnf); if (dnf > 0) alerts.Insert(0, $"**Carreras que no terminan:** {dnf} carreras pasaron de 7 minutos sin acabar.");
            sec.AppendLine();

            sb.AppendLine("## Alertas");
            sb.AppendLine();
            if (alerts.Count == 0) sb.AppendLine("Ninguna: todo dentro de los márgenes.");
            foreach (var a in alerts.Distinct().Take(40)) sb.AppendLine("- " + a);
            sb.AppendLine();
            sb.Append(sec);
            sb.AppendLine("---");
            sb.AppendLine("Perfiles: **Novato** (curvas al 80 % del límite, reacciona en 0,38 s, se le va la mano en el 12 % de las curvas, apenas cambia de carril, usa los objetos al azar), **Casual** (87 %, 0,27 s, 5 %), **Experto** (95 %, 0,16 s, 1 %, busca el rebufo y usa los objetos en buen momento). Entre carreras abren cofres, ven anuncios para abrirlos antes (máx. 8 al día), compran mejoras y a veces juegan carreras libres cuando se atascan.");
            File.WriteAllText(Path.Combine(botOut, "report.md"), sb.ToString());

            var csv = new StringBuilder("persona,bot,phase,mode,level,world,car,upgrades,place,cars,beat,dnf,time,gap,crashes,perfects,saves,hits,items,overtakes,passedBy,leadChanges,drift,leadShare,coins,perfectStart,falseStart,simMin\n");
            foreach (var r in botRaces)
                csv.AppendLine(string.Join(",", r.persona, r.bot, r.phase, r.mode, r.level, r.world, r.car, r.upgrades, r.place, r.cars, r.beat ? 1 : 0, r.dnf ? 1 : 0, F2(r.time), F2(r.gap), r.crashes, r.perfects, r.saves, r.hits, r.items, r.overtakes, r.passedBy, r.leadChanges, F2(r.drift), F2(r.leadShare), r.coins, r.perfectStart ? 1 : 0, r.falseStart ? 1 : 0, F1(r.simMin)));
            File.WriteAllText(Path.Combine(botOut, "races.csv"), csv.ToString());
            File.WriteAllText(Path.Combine(botOut, "alerts.txt"), string.Join("\n", alerts.Distinct()));
            Debug.Log($"[CozyBots] informe escrito en {Path.GetFullPath(botOut)} ({botRaces.Count} carreras, {alerts.Count} alertas)");
        }
    }
}
