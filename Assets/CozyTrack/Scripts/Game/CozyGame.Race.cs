using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyTrack
{
    public partial class CozyGame
    {
        class Shot { public string type; public RaceCar from, target; public int i, wraps; public float u, o, life, speed; public GameObject m; }
        class Trap { public string type; public int i; public float u, o, life, safe; public GameObject m; public RaceCar owner; }
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Trap> traps = new List<Trap>();
        int elimOut, elimLaps; float recT; readonly List<float> rec = new List<float>();

        float Prog(RaceCar c) => c.Prog(Track.N);
        int LapsDone(float p) => p < 0.5f ? 0 : Mathf.FloorToInt((p - 0.5f) / Track.N) + 1;
        float VSafe(RaceCar c, int i, float o) => Mathf.Sqrt(c.alat * Track.Surf(i).grip * Track.GripMul * (c.superT > 0 ? 1.35f : 1) * Track.Radius(i, o));

        // ---------- coches ----------
        RaceCar MakeCar(string name, int lane, bool isPlayer)
        {
            var c = new RaceCar { name = name, isPlayer = isPlayer, lane = lane, o = Track.LaneO(lane), aiT = Random.value * 2 };
            var def = CozyData.Car(name);
            var up = isPlayer ? Upgrades(name) : new int[3];
            c.SetPhysics(def.st, up[0], up[1], up[2]);
            if (isPlayer) c.g = SpawnCar(name, null, true);   // con su skin
            else { c.g = Spawn("Car_" + name, null, true, true); ApplySticker(c.g, name); }
            SetupWheels(c.g, out c.wheels, out c.wheelAxis, out c.wheelR);
            c.renderers = c.g.GetComponentsInChildren<Renderer>();
            c.origMats = c.renderers.Select(r => r.sharedMaterials).ToArray();
            return c;
        }
        SkidMarks skids;
        void ClearCars()
        {
            skids?.Clear();
            foreach (var c in cars) if (c.g) Destroy(c.g);
            cars.Clear(); player = null; rival = null;
        }
        // en el menú solo se ve el coche elegido, en la plataforma del escaparate
        void MenuGrid() { ClearCars(); UpdateShowroom(); }

        // ruedas: giran sobre su propio eje (el «derecha» del coche visto desde la rueda), con su radio real
        static void SetupWheels(GameObject g, out Transform[] ws, out Vector3[] axes, out float[] radii)
        {
            var l = new List<Transform>(); foreach (var t in g.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("Wheel_")) l.Add(t);
            ws = l.ToArray(); axes = new Vector3[ws.Length]; radii = new float[ws.Length];
            for (int k = 0; k < ws.Length; k++)
            {
                axes[k] = ws[k].InverseTransformDirection(g.transform.right).normalized;
                var r = ws[k].GetComponentInChildren<Renderer>();
                radii[k] = r ? Mathf.Max(0.05f, r.bounds.size.y / 2) : 0.3f;
            }
        }
        List<string> PickRivals()
        {
            var pool = CozyData.Cars.Select(c => c.name).Where(n => n != choice).ToList();
            for (int i = pool.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (pool[i], pool[j]) = (pool[j], pool[i]); }
            return pool.Take(3).ToList();
        }

        void PlaceCar(RaceCar c, float dt)
        {
            if (!c.g) return;
            if (c.isOut) { c.g.SetActive(false); return; }
            if (c.crash > 0)
            {
                // gira sobre su centro (no sobre los bajos) y nunca se mete bajo el suelo; tras la explosión, desaparece
                var rot = Quaternion.Euler(c.flyRot * 17, c.flyRot * 57, c.flyRot * 11); var mid = Vector3.up * 0.25f;
                c.g.transform.rotation = rot; c.g.transform.position = c.flyPos + mid - rot * mid;
                c.g.SetActive(!c.exploded); return;
            }
            if (c.exploded) c.exploded = false;
            // tambaleo después de un choque (visual)
            c.wobbleT += dt; c.wobble = Mathf.Max(0, c.wobble - dt * 2.2f);
            float wob = Mathf.Sin(c.wobbleT * 38) * c.wobble;
            c.g.SetActive(true);
            var w = Track.World(c.i, c.u, c.o);
            float lat = Track.LaneO(c.lane) - c.o;
            float r = Track.Radius(c.i, c.o); int s = r < float.PositiveInfinity ? Track.Pieces[c.i].s : 0; float lean = Mathf.Min(1, c.q);
            c.pitch += ((-c.acc) * 0.006f - c.pitch) * Mathf.Min(1, dt * 8);
            // giro: rumbo + inclinación hacia el carril nuevo + cola que sale en el derrape + trompo
            float yaw = w.h + lat * 0.25f - s * c.slip * 0.9f + (c.spin > 0 ? c.spin * 12 : 0);
            c.g.transform.position = w.pos + Vector3.up * (Mathf.Abs(wob) * 0.12f);
            c.g.transform.rotation = Quaternion.Euler(0, (yaw + wob * 0.25f) * Mathf.Rad2Deg, 0) * Quaternion.Euler(c.pitch * Mathf.Rad2Deg, 0, (w.roll + s * lean * lean * 0.12f + wob * 0.35f) * Mathf.Rad2Deg);
            for (int k = 0; k < c.wheels.Length; k++) c.wheels[k].Rotate(c.wheelAxis[k], c.v * dt / c.wheelR[k] * Mathf.Rad2Deg, Space.Self);
            if (c.shield > 0)
            {
                if (!c.shieldFx) { c.shieldFx = Spawn("Item_Shield", c.g.transform); c.shieldFx.transform.localScale = Vector3.one * 1.15f; c.shieldFx.transform.localPosition = Vector3.up * 0.45f; }
                c.shieldFx.SetActive(true);
            }
            else if (c.shieldFx) c.shieldFx.SetActive(false);
            bool on = c.superT > 0 || c.blackT > 0;
            if (on || c.fxOn)
            {
                for (int k = 0; k < c.renderers.Length; k++)
                {
                    if (!on) { c.renderers[k].SetPropertyBlock(null); continue; }
                    var mpb = new MaterialPropertyBlock(); c.renderers[k].GetPropertyBlock(mpb);
                    if (c.superT > 0) mpb.SetColor("_EmissionColor", Color.HSVToRGB((airT * 1.5f) % 1, 1, 0.7f));
                    else mpb.SetColor("_EmissionColor", new Color(-0.3f, -0.3f, -0.3f) * (Random.value < 0.2f ? 1.6f : 1));
                    c.renderers[k].SetPropertyBlock(mpb);
                }
                c.fxOn = on;
            }
        }

        // ---------- carrera ----------
        void StartRace()
        {
            ClearCars();
            foreach (var s in shots) Destroy(s.m); shots.Clear();
            foreach (var t in traps) Destroy(t.m); traps.Clear();
            if (ghost != null) { Destroy(ghost); ghost = null; }
            laps = mode == "time" ? 2 : 3;
            player = MakeCar(choice, 1, true); cars.Add(player);
            string chip;
            oppCar = null;
            if (mode == "online") chip = OnlineCars();
            else if (mode != "time")
            {
                var others = mode == "level" ? new List<string> { levelRival }.Concat(PickRivals().Where(n => n != levelRival).Take(2)).ToList() : PickRivals();
                int[] lanes = { 0, 2, 3 };
                for (int k = 0; k < others.Count; k++) { var c = MakeCar(others[k], lanes[k], false); c.k = 0.8f + k * 0.03f; c.kStraight = 0.88f + k * 0.02f; c.react = 0.25f; cars.Add(c); }
                rival = mode == "level" ? cars[1] : cars[1 + Random.Range(0, 3)];
                rival.k = mode == "level" ? levelSkill : 0.9f; rival.kStraight = rival.k + 0.02f; rival.react = 0.15f;
                chip = mode == "elim" ? "Last car each lap is out" : "";   // el rival a batir es interno: el jugador no lo ve
            }
            else chip = LoadGhost() ? "" : "No record yet: set one!";
            perfects = 0; crashes = 0; countGas = false; pressedAt = 99;
            rec.Clear(); recT = 0; elimOut = 0; elimLaps = 0;
            SetRivalChip(chip);
            foreach (var co in coins) { co.taken = false; co.m.SetActive(true); }
            foreach (var b in boxes) { b.t = 0; b.m.SetActive(mode != "online"); }   // online: sin objetos
            raceT = 0; countT = 3.2f; state = "count";
            camPos = Track.World(0, 0, 0).pos + new Vector3(0, 10, -7); camFocus = Track.World(0, 0, 0).pos; camHeading = Track.World(0, 0, 0).h * Mathf.Rad2Deg;
            ShowScreen("hud");
        }

        void RaceTick(float dt)
        {
            if (state == "race") raceT += dt;
            foreach (var c in cars) Step(c, dt);
            Collisions();
            if (mode == "elim" && state == "race")
            {
                // al cerrar cada vuelta el líder, el último queda fuera (la última vuelta decide el ganador)
                var act = cars.Where(c => !c.isOut).ToList();
                var lead = act.OrderByDescending(Prog).First(); int L = LapsDone(Prog(lead)) - 1;
                if (L > elimLaps && L < laps && act.Count > 1)
                {
                    elimLaps = L; var last = act.OrderBy(Prog).First();
                    Sparks(last, Color.white, 16); last.isOut = true; last.outAt = ++elimOut; last.outLap = L;
                    Toast(last.isPlayer ? "You're out!" : last.name.ToUpper() + " IS OUT!");
                    if (last.isPlayer) { shake = 0.4f; Finish(); return; }
                }
            }
            if (mode == "time" && state == "race") { recT += dt; if (recT >= 0.1f) { recT = 0; RecordGhost(); } }
            UpdateGhost();
            foreach (var c in cars)
            {
                if (c.crash > 0 || c.isOut) continue;
                foreach (var b in boxes)
                    if (b.m.activeSelf && b.i == c.i && Mathf.Abs(b.u - c.u) * Track.PieceLen(b.i, c.o) < 0.8f && Mathf.Abs(b.o - c.o) < 0.7f)
                    {
                        b.m.SetActive(false); b.t = 3;
                        if (c.item == null) { c.item = RollItem(c); c.uses = c.item == "turbo3" ? 3 : 0; if (c.isPlayer) Toast("Got: " + CozyData.Items[c.item][1]); }
                    }
                var pc = Track.World(c.i, c.u, c.o).pos;
                if (c.isPlayer)
                {
                    float rad = c.magnetT > 0 ? 3.2f : 0.75f;
                    foreach (var co in coins) if (!co.taken && Vector3.Distance(co.m.transform.position, pc + Vector3.up * 0.55f) < rad + 0.3f) { co.taken = true; co.m.SetActive(false); c.coins++; Sparks(c, CozyData.Hex("#ffc733"), 4); }
                }
                for (int t = traps.Count - 1; t >= 0; t--)
                {
                    var tr = traps[t]; if (tr.owner == c && tr.safe > 0) continue;
                    var tp = tr.m.transform.position;
                    if (Vector2.Distance(new Vector2(tp.x, tp.z), new Vector2(pc.x, pc.z)) < 0.75f) { HitCar(c, tr.owner, tr.type == "oil" ? "slip" : "spin"); Destroy(tr.m); traps.RemoveAt(t); }
                }
            }
            if (mode != "online") foreach (var b in boxes) if (!b.m.activeSelf) { b.t -= dt; if (b.t <= 0) b.m.SetActive(true); }
            for (int k = shots.Count - 1; k >= 0; k--)
            {
                var s = shots[k]; s.life -= dt;
                AdvanceShot(s, s.speed * dt);
                if (s.target != null) s.o += Mathf.Sign(s.target.o - s.o) * Mathf.Min(Mathf.Abs(s.target.o - s.o), (s.type == "comet" ? 12 : 5) * dt);
                var w = Track.World(s.i, s.u, s.o); float y = 0.45f;
                if (s.type == "comet")
                {
                    var tp = s.target != null ? Track.World(s.target.i, s.target.u, s.target.o).pos : w.pos;
                    float dd = Vector2.Distance(new Vector2(tp.x, tp.z), new Vector2(w.pos.x, w.pos.z)); y = Mathf.Min(3, 0.4f + dd * 0.25f);
                    if (s.target != null && dd < 2) { s.i = s.target.i; s.u = s.target.u; }
                }
                s.m.transform.position = w.pos + Vector3.up * y; s.m.transform.Rotate(0, dt * 570, 0);
                RaceCar hit = null;
                foreach (var c in cars) if (c != s.from && c.crash <= 0 && !c.isOut && (s.type != "comet" || c == s.target))
                {
                    var p = Track.World(c.i, c.u, c.o).pos;
                    if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(w.pos.x, w.pos.z)) < (s.type == "comet" ? 1.4f : 0.9f)) hit = c;
                }
                if (hit != null) HitCar(hit, s.from, "spin");
                if (hit != null || s.life <= 0) { Destroy(s.m); shots.RemoveAt(k); }
            }
            for (int t = traps.Count - 1; t >= 0; t--) { var tr = traps[t]; tr.life -= dt; tr.safe -= dt; if (tr.type == "fakebox") tr.m.transform.Rotate(0, dt * 86, 0); if (tr.life <= 0) { Destroy(tr.m); traps.RemoveAt(t); } }
            if (state == "race" && player.done >= 0) { if (mode == "time") RecordGhost(); Finish(); }
        }

        void Advance(RaceCar c, float dist)
        {
            float left = dist;
            while (left > 0)
            {
                float L = Track.PieceLen(c.i, c.o), rem = (1 - c.u) * L;
                if (left < rem) { c.u += left / L; left = 0; }
                else { left -= rem; c.u = 0; c.i++; if (c.i >= Track.N) { c.i = 0; c.wraps++; } }
            }
        }
        void AdvanceShot(Shot s, float dist)
        {
            float left = dist;
            while (left > 0)
            {
                float L = Track.PieceLen(s.i, s.o), rem = (1 - s.u) * L;
                if (left < rem) { s.u += left / L; left = 0; }
                else { left -= rem; s.u = 0; s.i++; if (s.i >= Track.N) { s.i = 0; s.wraps++; } }
            }
        }

        (float d, float v, int i)? NextCurve(RaceCar c)
        {
            float d = 0;
            for (int k = 0; k < 3; k++)
            {
                int i = (c.i + k) % Track.N;
                if (!Track.Pieces[i].straight) return (k == 0 ? 0 : d, VSafe(c, i, c.o), i);
                d += k == 0 ? (1 - c.u) * Track.PieceLen(i, c.o) : Track.PieceLen(i, c.o);
            }
            return null;
        }
        (float vt, float brakeAt)? BrakeTarget(RaceCar c, float factor)
        {
            float d = (1 - c.u) * Track.PieceLen(c.i, c.o); (float vt, float brakeAt, float need)? best = null;
            for (int k = 1; k <= 6 && d < 45; k++)
            {
                int i = (c.i + k) % Track.N;
                if (!Track.Pieces[i].straight)
                {
                    float vt = VSafe(c, i, c.o) * factor, need = (c.v * c.v - vt * vt) / (2 * c.brake) - d;
                    if (best == null || need > best.Value.need) best = (vt, d - Mathf.Max(0, (c.v * c.v - vt * vt) / (2 * c.brake)), need);
                }
                d += Track.PieceLen(i, c.o);
            }
            if (best == null) return null;
            return (best.Value.vt, best.Value.brakeAt);
        }

        void Crash(RaceCar c)
        {
            if (c.crash > 0) return;
            var w = Track.World(c.i, c.u, c.o); var p = Track.Pieces[c.i];
            c.slip = 0; c.crash = 1.2f;
            var f = new Vector3(Mathf.Sin(w.h), 0, Mathf.Cos(w.h)); var right = new Vector3(Mathf.Cos(w.h), 0, -Mathf.Sin(w.h));
            // la fuerza centrífuga empuja hacia fuera de la curva (a la derecha en una curva a la izquierda)
            c.flyPos = w.pos; c.flyVel = f * c.v * 0.7f + right * p.s * 5 + Vector3.up * 4; c.flyRot = 0; c.exploded = false; c.skidOn = false;
            c.v = 0; c.turbo = 0; c.slipPeak = 0;
            if (c.isPlayer) { shake = 0.5f; Toast("Off the track!"); crashes++; }
        }

        void Step(RaceCar c, float dt)
        {
            c.cool = Mathf.Max(0, c.cool - dt); c.shield = Mathf.Max(0, c.shield - dt); c.turbo = Mathf.Max(0, c.turbo - dt);
            c.superT = Mathf.Max(0, c.superT - dt); c.autoT = Mathf.Max(0, c.autoT - dt); c.blackT = Mathf.Max(0, c.blackT - dt); c.bubbleT = Mathf.Max(0, c.bubbleT - dt); c.magnetT = Mathf.Max(0, c.magnetT - dt);
            if (c.remote) { StepRemote(c, dt); return; }
            if (c.isOut || (c.done >= 0 && !c.isPlayer && c.v < 0.1f)) return;
            if (c.crash > 0)
            {
                c.crash -= dt; c.flyVel.y -= 14 * dt; c.flyPos += c.flyVel * dt; c.flyRot += dt * (c.flyPos.y > Floor + 0.02f ? 9 : 3);
                // rebota en el suelo en vez de atravesarlo
                if (c.flyPos.y < Floor) { c.flyPos.y = Floor; if (c.flyVel.y < 0) c.flyVel.y = -c.flyVel.y * 0.35f; c.flyVel.x *= 0.6f; c.flyVel.z *= 0.6f; }
                if (!c.exploded && c.crash <= 0.6f) Explode(c);
                if (c.crash <= 0) { c.v = 0; c.spin = 0; c.exploded = false; }
                return;
            }
            bool gas = c.isPlayer ? (c.done < 0 && (botRun ? BotThrottle(c, dt) : Gas())) : AiThrottle(c, dt);
            if (state != "race") gas = false;
            if (c.spin > 0) { c.spin -= dt; gas = false; }
            c.gas = gas;
            var sf = Track.Surf(c.i); float alat = c.alat * sf.grip * Track.GripMul * (c.superT > 0 ? 1.35f : 1);
            if (c.autoT > 0) gas = c.v < Mathf.Min(c.vtop * 1.45f, VSafe(c, c.i, c.o) * 1.05f);   // piloto automático: conduce perfecto
            float r0 = Track.Radius(c.i, c.o), q0 = r0 < float.PositiveInfinity ? (c.v * c.v / r0) / alat : 0;
            float corner = r0 < float.PositiveInfinity ? Mathf.Clamp01((q0 - RaceCar.CornerQ0) / (RaceCar.CornerQEq - RaceCar.CornerQ0)) * c.F : 0;
            c.draft = DraftOf(c);
            float power = (c.turbo > 0 ? 1.6f : 1) * (c.draft ? 1.25f : 1) * sf.boost * (c.superT > 0 ? 1.25f : 1) * (c.autoT > 0 ? 1.5f : 1) * (c.blackT > 0 ? 0.35f : 1) * (c.magnetT > 0 ? 1.08f : 1);
            float engine = gas ? c.F * power : 0;
            float a = (engine - c.Cd * c.v * c.v - c.Crr * sf.roll * c.v - corner) / c.m;
            if (!gas) a -= c.brake;
            c.acc = a;
            a -= RaceCar.SlipScrub * sf.scrub * c.slip;   // derrapar frena (en hielo casi nada)
            c.v = Mathf.Max(0, c.v + a * dt);
            float target = Track.LaneO(c.lane); c.o += Mathf.Sign(target - c.o) * Mathf.Min(Mathf.Abs(target - c.o), 6 * dt);
            float before = Prog(c); bool wasCurve = !Track.Pieces[c.i].straight; int prevI = c.i;
            Advance(c, c.v * dt);
            if (c.isPlayer && c.i != prevI) PieceChanged(c, wasCurve);
            if (LapsDone(Prog(c)) > LapsDone(before) && LapsDone(Prog(c)) > laps && c.done < 0) c.done = raceT;
            // derrape: la cola sale cuando la aceleración lateral pedida supera el agarre
            float r = Track.Radius(c.i, c.o), q = r < float.PositiveInfinity ? (c.v * c.v / r) / alat : 0;
            c.q = q;
            bool immune = c.shield > 0.001f || c.autoT > 0 || c.turbo > 0;   // con turbo no te sales
            if (q > 1 && !immune) c.slip += RaceCar.SlipGrow * (q - 1) * dt;
            else c.slip = Mathf.Max(0, c.slip - RaceCar.SlipRecover * Mathf.Max(0.4f, sf.grip) * dt);
            SurfaceFx(c, sf);
            if (c.slip > 0.03f && Random.value < 0.4f + c.slip) Puff(c);
            c.slipPeak = Mathf.Max(c.slipPeak, c.slip);
            if (c.slip >= 1) Crash(c);
            else if (c.isPlayer && c.slip == 0 && c.slipPeak >= 0.4f) { c.slipPeak = 0; c.coins += 2; Toast("Saved the slide! +2"); }
            else if (c.slip == 0) c.slipPeak = 0;
        }

        // rebufo: otro coche delante, en el mismo carril y a menos de 3,5 m
        bool DraftOf(RaceCar c)
        {
            if (c.v < 4) return false;
            var pc = Track.World(c.i, c.u, c.o).pos;
            foreach (var o in cars)
            {
                if (o == c || o.isOut || o.crash > 0 || Mathf.Abs(o.o - c.o) > 0.5f) continue;
                float ahead = Prog(o) - Prog(c); if (ahead <= 0 || ahead > 0.6f) continue;
                float d = Vector3.Distance(Track.World(o.i, o.u, o.o).pos, pc);
                if (d > 1.4f && d < 3.5f) return true;
            }
            return false;
        }

        // curva perfecta: entrar entre el 72 % y el 100 % del límite y salir sin derrapar → miniturbo
        void PieceChanged(RaceCar c, bool wasCurve)
        {
            // cada pieza curva cuenta por separado: en circuitos de curvas encadenadas (nieve) también hay curvas perfectas
            bool nowCurve = !Track.Pieces[c.i].straight;
            if (wasCurve)
            {
                if (c.cleanCurve && c.slipPeak < 0.05f) { c.turbo = Mathf.Max(c.turbo, 0.6f); if (c.isPlayer) Toast("Perfect curve!"); if (c.isPlayer) perfects++; }
                c.cleanCurve = false;
            }
            if (nowCurve) { float q = (c.v * c.v / Track.Radius(c.i, c.o)) / c.alat; c.entryQ = q; c.cleanCurve = q >= 0.72f && q <= 1.0f; }
            if (nowCurve && c.slip > 0.05f) c.cleanCurve = false;
        }

        bool AiThrottle(RaceCar c, float dt)
        {
            c.aiT -= dt;
            if (c == rival && player != null)
            {
                // como Delbruck/Mueggler: el rival afloja cuando va por delante (del todo con 1/3 de vuelta de ventaja)
                float gap = Prog(player) - Prog(c), baseK = mode == "level" ? levelSkill : 0.9f;
                c.k = gap >= 0 ? baseK + Mathf.Min(0.06f, gap * 0.03f) : baseK - Mathf.Min(0.14f, -gap / (Track.N / 3f) * 0.14f);
                c.kStraight = c.k + 0.02f;
            }
            float target = c.vtop * c.kStraight * (c.turbo > 0 ? 1.2f : 1);
            float r = Track.Radius(c.i, c.o), kk = c.k * (c.bubbleT > 0 ? 0.85f : 1);
            if (r < float.PositiveInfinity) target = Mathf.Min(target, VSafe(c, c.i, c.o) * kk);
            var bt = BrakeTarget(c, kk);
            if (bt != null && bt.Value.brakeAt <= c.v * c.react) target = Mathf.Min(target, bt.Value.vt);
            if (c.aiT < 0)
            {
                c.aiT = 0.5f + Random.value;
                foreach (var o in cars) if (o != c && !o.isOut && Mathf.Abs(o.o - c.o) < 0.7f) { float d = Prog(o) - Prog(c); if (d > 0 && d < 0.35f && o.v < c.v) ChangeLane(c, c.lane >= 2 ? -1 : 1); }
                foreach (var ob in obstacles) if (ob.d.i == (c.i + 1) % Track.N && Mathf.Abs(ob.o - c.o) < 1.2f) ChangeLane(c, ob.o > c.o ? -1 : 1);   // esquiva al gato y compañía
                if (c.item != null && Random.value < 0.45f) UseItem(c);
            }
            return c.v < target;
        }

        void ChangeLane(RaceCar c, int d)
        {
            if (c == null || c.crash > 0) return;
            int t = Mathf.Clamp(c.lane + d, 0, Track.NL - 1);
            if (t != c.lane) { c.prevLane = c.lane; c.lane = t; }
        }

        // ---------- choques por peso ----------
        void Collisions()
        {
            for (int x = 0; x < cars.Count; x++) for (int y = x + 1; y < cars.Count; y++)
            {
                var A = cars[x]; var B = cars[y];
                if (A.remote || B.remote || A.isOut || B.isOut || A.crash > 0 || B.crash > 0 || A.cool > 0 || B.cool > 0) continue;
                var pa = Track.World(A.i, A.u, A.o).pos; var pb = Track.World(B.i, B.u, B.o).pos;
                float mA = A.m * (A.shield > 0 ? 4 : 1), mB = B.m * (B.shield > 0 ? 4 : 1);
                // cruce a nivel del ocho: los dos pasos se cortan en ángulo recto; el más ligero sale girando
                if (Track.Pieces[A.i].cross && Track.Pieces[B.i].cross && A.i != B.i)
                {
                    if (Vector2.Distance(new Vector2(pa.x, pa.z), new Vector2(pb.x, pb.z)) < 1.15f)
                    {
                        var light = mA <= mB ? A : B; HitCar(light, light == A ? B : A, "spin"); light.v *= 2.5f; A.cool = B.cool = 0.8f;
                        BumpFx(A, B, mA, mB, pa, pb, 6);
                        if (A.isPlayer || B.isPlayer) { shake = 0.4f; Toast(light.isPlayer ? "Crash at the crossing!" : "You rammed them at the crossing!"); }
                    }
                    continue;
                }
                if (Vector3.Distance(pa, pb) > 1.25f || Mathf.Abs(A.o - B.o) > 0.95f) continue;
                // impulso en la dirección de la pista: el de detrás frena y empuja al de delante según las masas
                float dp = Prog(B) - Prog(A); var rear = dp > 0 ? A : B; var front = rear == A ? B : A;
                float mR = rear == A ? mA : mB, mF = front == A ? mA : mB, vr = rear.v - front.v;
                if (vr > 0) { float J = (1 + RaceCar.Restitution) * vr / (1 / mR + 1 / mF); rear.v = Mathf.Max(0, rear.v - J / mR); front.v += J / mF; }
                BumpFx(A, B, mA, mB, pa, pb, Mathf.Max(vr, 1.5f));
                // empujón lateral: si alguien cambiaba de carril, el más ligero vuelve al suyo y pierde velocidad
                var lt = mA <= mB ? A : B; var hv = lt == A ? B : A; float mL = Mathf.Min(mA, mB), mH = Mathf.Max(mA, mB);
                bool Changing(RaceCar c) => c.prevLane >= 0 && Mathf.Abs(Track.LaneO(c.lane) - c.o) > 0.05f;
                if (Changing(A) || Changing(B))
                {
                    if (Changing(lt)) lt.lane = lt.prevLane;
                    else { int away = (int)Mathf.Sign(lt.o - hv.o); if (away == 0) away = 1; int t = lt.lane + away; if (t >= 0 && t < Track.NL) lt.lane = t; }
                    lt.v *= 1 - 0.45f * mH / (mL + mH);
                }
                A.cool = B.cool = 0.5f;
                foreach (var c in new[] { A, B }) if (c.isPlayer) Toast(c == lt && mL < mH ? "Bump! " + (c == A ? B : A).name + " is heavier" : "Bump!");
            }
        }

        // ---------- objetos ----------
        RaceCar LeaderExcept(RaceCar c) { RaceCar best = null; foreach (var o in cars) if (o != c && !o.isOut && o.crash <= 0 && (best == null || Prog(o) > Prog(best))) best = o; return best; }
        RaceCar NextAhead(RaceCar c) { RaceCar best = null; float bd = 1e9f; foreach (var o in cars) if (o != c && !o.isOut) { float d = Prog(o) - Prog(c); if (d > 0 && d < bd) { bd = d; best = o; } } return best; }
        bool ProtectedHit(RaceCar c)
        {
            if (c.superT > 0 || c.autoT > 0 || c.turbo > 0) return true;
            if (c.shield > 0) { c.shield = 0; if (c.isPlayer) Toast("Your shield saved you"); return true; }
            return false;
        }
        void HitCar(RaceCar c, RaceCar from, string how)
        {
            if (c.crash > 0 || ProtectedHit(c)) return;
            if (how == "slip") { c.slip = Mathf.Min(0.95f, c.slip + 0.75f); c.v *= 0.7f; } else { c.v *= 0.15f; c.spin = 0.8f; }
            Sparks(c, CozyData.Hex("#ffd64d"), 10);
            if (c.isPlayer) { Toast(how == "slip" ? "Slipped!" : "You got hit!"); shake = 0.35f; }
            else if (from != null && from.isPlayer) Toast("Hit!");
        }
        void Fire(RaceCar c, string type)
        {
            var m = Spawn(type == "comet" ? "Prop_Space_Star" : "Item_Projectile", trackRoot, true);
            m.transform.localScale = Vector3.one * (type == "comet" ? 0.5f : type == "marble" ? 0.7f : 0.9f);
            var target = type == "homing" ? NextAhead(c) : type == "comet" ? LeaderExcept(c) : null;
            shots.Add(new Shot { type = type, from = c, target = target, i = c.i, u = c.u, wraps = c.wraps, o = c.o, m = m, life = type == "comet" ? 8 : 4, speed = type == "comet" ? 30 : type == "marble" ? 26 : 24 });
        }
        void DropTrap(RaceCar c, string type)
        {
            int i = c.i; float u = c.u - 1.6f / Track.PieceLen(c.i, c.o); while (u < 0) { i = (i - 1 + Track.N) % Track.N; u += 1; }
            GameObject m;
            if (type == "oil") m = Prim(PrimitiveType.Cylinder, Color.white, Vector3.zero, new Vector3(1.1f, 0.01f, 1.1f), trackRoot, 0, Mat(CozyData.Hex("#1b1530")));
            else { m = Spawn("Item_Box", trackRoot, true); m.transform.localScale = Vector3.one * 0.75f; foreach (var r in m.GetComponentsInChildren<Renderer>()) { var mpb = new MaterialPropertyBlock(); mpb.SetColor("_EmissionColor", CozyData.Hex("#ff3355") * 0.25f); r.SetPropertyBlock(mpb); } }
            var w = Track.World(i, u, c.o); m.transform.position = w.pos + Vector3.up * (type == "oil" ? 0.02f : 0.55f);
            traps.Add(new Trap { type = type, i = i, u = u, o = c.o, m = m, life = 25, owner = c, safe = 0.6f });
        }
        void UseItem(RaceCar c)
        {
            if (c == null || c.item == null || c.crash > 0) return;
            string it = c.item;
            if (it == "turbo3") { c.uses--; c.turbo = 1.2f; if (c.uses <= 0) { c.item = null; c.uses = 0; } } else c.item = null;
            if (it == "turbo") c.turbo = 1.6f;
            if (it == "shield") c.shield = 8;
            if (it == "marble" || it == "homing" || it == "comet") Fire(c, it);
            if (it == "oil" || it == "fakebox") DropTrap(c, it);
            if (it == "super") { c.superT = 5; c.slip = 0; }
            if (it == "autopilot") { c.autoT = 3; c.slip = 0; }
            if (it == "magnet") c.magnetT = 6;
            if (it == "blackout")
            {
                // corte de corriente en los carriles de los que van delante
                foreach (var o in cars) if (o != c && !o.isOut && Prog(o) > Prog(c) && !(o.shield > 0 || o.superT > 0)) { o.blackT = 2.5f; if (o.isPlayer) { Flash(CozyData.Hex("#20203a")); Toast("Blackout in your lane!"); } }
                if (c.isPlayer) Flash(Color.white);
            }
            if (it == "bubbles") foreach (var o in cars) if (o != c && !o.isOut && Prog(o) > Prog(c)) { o.bubbleT = 3.5f; if (o.isPlayer) Toast("Bubbles!"); }
            if (c.isPlayer && !autoTest) Toast(it == "blackout" ? "Blackout!" : CozyData.Items[it][1] + "!");
        }
        string RollItem(RaceCar c)
        {
            int place = cars.Where(x => !x.isOut).OrderByDescending(Prog).ToList().IndexOf(c);
            var odds = CozyData.ItemOdds[Mathf.Clamp(place, 0, 3)];
            float t = Random.value * odds.Sum(o => o.Item2);
            foreach (var (k, v) in odds) { t -= v; if (t <= 0) return k; }
            return "turbo";
        }

        // ---------- partículas: humo, polvo, salpicaduras, chispas ----------
        const float Floor = -0.2f;   // altura del suelo bajo la pista

        // ---------- choques: el efecto depende de quién pesa más ----------
        void BumpFx(RaceCar A, RaceCar B, float mA, float mB, Vector3 pa, Vector3 pb, float force)
        {
            if (botRun) return;
            var mid = (pa + pb) * 0.5f + Vector3.up * 0.35f;
            float ratio = Mathf.Max(mA, mB) / Mathf.Max(0.01f, Mathf.Min(mA, mB));
            float k = Mathf.Clamp(force / 6f, 0.5f, 1.5f);
            Vector3 Rv(float s, float up) => new Vector3((Random.value - 0.5f) * s, up * (0.6f + Random.value * 0.8f), (Random.value - 0.5f) * s);
            if (ratio < 1.12f)
            {
                // mismo peso: choque seco y simétrico, los dos se tambalean
                for (int n = 0; n < 16 * k; n++) fx.Add(mid, Random.value < 0.5f ? SunC : Color.white, 0.12f, 0.45f, Rv(7, 3), 9);
                fx.Add(mid, new Color(1, 1, 1, 0.9f), 0.5f, 0.18f, Vector3.zero, 0, true);
                for (int n = 0; n < 3; n++) fx.Add(mid - Vector3.up * 0.25f, new Color(1, 1, 1, 0.7f), 0.35f, 0.6f, Rv(1.5f, 0.6f), 0, true);
                A.wobble = B.wobble = 0.45f * k;
                if (A.isPlayer || B.isPlayer) shake = Mathf.Max(shake, 0.28f * k);
                return;
            }
            var light = mA < mB ? A : B; var heavy = light == A ? B : A;
            var lp = (light == A ? pa : pb) + Vector3.up * 0.4f;
            // el ligero sale despedido: chispas grandes, polvo y estrellitas de mareo
            for (int n = 0; n < 22 * k; n++) fx.Add(lp, Random.value < 0.6f ? SunC : Coral, 0.15f, 0.55f, Rv(8, 4), 10);
            for (int n = 0; n < 4; n++) fx.Add(lp - Vector3.up * 0.3f, new Color(0.95f, 0.9f, 0.8f, 0.8f), 0.45f, 0.8f, Rv(2, 0.8f), 0, true);
            for (int n = 0; n < 6; n++)
            {
                float a = n / 6f * Mathf.PI * 2; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                fx.Add(lp + Vector3.up * 0.55f + dir * 0.3f, SunC, 0.14f, 0.9f, new Vector3(-dir.z, 0, dir.x) * 1.6f + Vector3.up * 0.3f, 0);
            }
            light.wobble = 0.8f * k; heavy.wobble = 0.12f;
            // el pesado apenas lo nota: unas pocas chispas en su morro
            var hp = (heavy == A ? pa : pb) + Vector3.up * 0.4f;
            for (int n = 0; n < 5; n++) fx.Add(hp, Color.white, 0.09f, 0.3f, Rv(4, 2), 9);
            if (light.isPlayer) { shake = Mathf.Max(shake, 0.45f * k); Flash(new Color(1, 0.55f, 0.35f)); }
            else if (heavy.isPlayer) shake = Mathf.Max(shake, 0.12f);
        }

        // ---------- explosión al salirse (en vez de desaparecer) ----------
        void Explode(RaceCar c)
        {
            c.exploded = true;
            if (botRun) return;
            var p = c.flyPos + Vector3.up * 0.3f;
            Color paint = Coral;
            if (c.renderers != null && c.renderers.Length > 0 && c.renderers[0] && c.renderers[0].sharedMaterial && c.renderers[0].sharedMaterial.HasProperty("_BaseColor")) paint = c.renderers[0].sharedMaterial.GetColor("_BaseColor");
            Vector3 Rv(float s, float up) => new Vector3((Random.value - 0.5f) * s, up * (0.5f + Random.value), (Random.value - 0.5f) * s);
            fx.Add(p, new Color(1, 0.97f, 0.8f, 1), 1.3f, 0.22f, Vector3.zero, 0, true);                                   // destello
            for (int n = 0; n < 14; n++) fx.Add(p + Rv(0.4f, 0.2f), Color.Lerp(SunC, Coral, Random.value), 0.45f + Random.value * 0.3f, 0.45f + Random.value * 0.35f, Rv(4, 2.5f), 0, true);   // bola de fuego
            for (int n = 0; n < 10; n++) fx.Add(p + Rv(0.5f, 0.3f), new Color(0.35f, 0.32f, 0.38f, 0.75f), 0.5f, 1.1f + Random.value * 0.6f, Rv(1.5f, 1.8f), -0.6f, true);   // humo
            for (int n = 0; n < 18; n++) fx.Add(p, SunC, 0.1f, 0.6f, Rv(9, 6), 12);                                           // chispas
            for (int n = 0; n < 12; n++) fx.Add(p, Color.Lerp(paint, Ink, Random.value * 0.3f), 0.16f, 1.0f, Rv(7, 7), 14); // trozos de chapa (y tornillos)
            for (int n = 0; n < 6; n++) fx.Add(p, Ink, 0.08f, 1.0f, Rv(6, 6), 14);
            // mancha de quemado en el suelo
            var right = new Vector3(1, 0, 0); var gp = new Vector3(c.flyPos.x, Floor + 0.012f, c.flyPos.z);
            skids.Add(gp - Vector3.forward * 0.45f, gp + Vector3.forward * 0.45f, right, 0.9f, new Color(0.1f, 0.08f, 0.12f, 0.55f), new Color(0.1f, 0.08f, 0.12f, 0.55f));
            if (c.isPlayer) { shake = Mathf.Max(shake, 0.7f); Flash(new Color(1, 0.75f, 0.35f)); Toast("KABOOM!"); }
            else if (player != null && Vector3.Distance(player.g ? player.g.transform.position : p, p) < 8) shake = Mathf.Max(shake, 0.25f);
        }

        // ---------- huellas de las ruedas traseras ----------
        static readonly Color SkidBase = new Color(0.12f, 0.1f, 0.14f, 1);
        void Skid(RaceCar c)
        {
            if (!c.g || c.isOut || c.crash > 0 || c.remote && c.v < 0.1f || c.wheels.Length < 2) { c.skidOn = false; return; }
            if (c.v < 0.6f && c.slip <= 0) return;
            // las dos ruedas más traseras
            Transform r0 = null, r1 = null; float z0 = 1e9f, z1 = 1e9f;
            foreach (var wh in c.wheels)
            {
                float z = c.g.transform.InverseTransformPoint(wh.position).z;
                if (z < z0) { r1 = r0; z1 = z0; r0 = wh; z0 = z; } else if (z < z1) { r1 = wh; z1 = z; }
            }
            if (!r0 || !r1) return;
            var sf = Track.Surf(c.i);
            Color tint = SkidBase; float a = 0.09f;
            switch (sf.id) { case "barro": tint = CozyData.Hex("#4a3020"); a = 0.4f; break; case "arena": tint = CozyData.Hex("#a07a48"); a = 0.3f; break; case "agua": tint = CozyData.Hex("#3a4a60"); a = 0.22f; break; case "hielo": tint = Color.white; a = 0.28f; break; case "jabon": tint = Color.white; a = 0.2f; break; }
            a += Mathf.Clamp01(c.slip) * 0.55f + (c.spin > 0 ? 0.35f : 0) + (c.acc < -6 ? 0.12f : 0);
            var col = tint; col.a = Mathf.Clamp01(a);
            var right = c.g.transform.right; right.y = 0; right.Normalize();
            for (int k = 0; k < 2; k++)
            {
                var wh = k == 0 ? r0 : r1; int idx = System.Array.IndexOf(c.wheels, wh);
                float rad = idx >= 0 && idx < c.wheelR.Length ? c.wheelR[idx] : 0.08f;
                var pt = wh.position - Vector3.up * (rad - 0.01f);
                if (!c.skidOn) { c.skidPrev[k] = pt; c.skidCol[k] = col; continue; }
                float d = Vector3.Distance(pt, c.skidPrev[k]);
                if (d > 2.5f) { c.skidPrev[k] = pt; c.skidCol[k] = col; continue; }
                if (d < 0.22f) continue;
                skids.Add(c.skidPrev[k], pt, right, Mathf.Max(0.05f, rad * 0.9f), c.skidCol[k], col);
                c.skidPrev[k] = pt; c.skidCol[k] = col;
            }
            c.skidOn = true;
        }

        void Puff(RaceCar c) { var w = Track.World(c.i, c.u, c.o); var f = new Vector3(Mathf.Sin(w.h), 0, Mathf.Cos(w.h)); fx.Add(w.pos - f * 0.6f + new Vector3(Random.value - 0.5f, 0.15f, Random.value - 0.5f) * 0.5f, new Color(1, 1, 1, 0.8f), 0.36f, 0.6f, Vector3.up * 0.8f, 0, true); }
        void Sparks(RaceCar c, Color col, int n) { var w = Track.World(c.i, c.u, c.o); for (int k = 0; k < n; k++) fx.Add(w.pos + Vector3.up * 0.5f, col, 0.13f, 0.5f, new Vector3((Random.value - 0.5f) * 5, 2 + Random.value * 3, (Random.value - 0.5f) * 5), 9); }
        void SurfaceFx(RaceCar c, SurfDef sf)
        {
            if (!sf.hasFx || c.v < 2 || Random.value > Mathf.Min(1, c.v * 0.06f)) return;
            var w = Track.World(c.i, c.u, c.o); var f = new Vector3(Mathf.Sin(w.h), 0, Mathf.Cos(w.h)); var r = new Vector3(f.z, 0, -f.x);
            var b = w.pos - f * 0.55f + r * (Random.value - 0.5f) * 0.6f; Vector3 Rv(float s) => new Vector3((Random.value - 0.5f) * s, 0, (Random.value - 0.5f) * s);
            switch (sf.id)
            {
                case "arena": fx.Add(b + Vector3.up * 0.1f, sf.fx, 0.32f, 0.9f, Rv(0.8f) + Vector3.up * 0.9f, 0.5f, true); break;
                case "barro": fx.Add(b + Vector3.up * 0.2f, sf.fx, 0.2f, 0.6f, Rv(2.5f) + Vector3.up * 2.5f, 9); break;
                case "agua": fx.Add(b + Vector3.up * 0.15f, sf.fx, 0.15f, 0.5f, Rv(3) + Vector3.up * 3, 10); break;
                case "hielo": fx.Add(b + Vector3.up * 0.08f, sf.fx, 0.07f, 0.7f, Vector3.up * 0.3f, 0); break;
                case "jabon": fx.Add(b + Vector3.up * 0.3f, new Color(1, 1, 1, 0.7f), 0.15f + Random.value * 0.1f, 1.2f, Rv(0.6f) + Vector3.up * 0.6f, 0, true); break;
                case "turbo": fx.Add(b + Vector3.up * 0.15f, sf.fx, 0.11f, 0.35f, -f * 3 + Vector3.up * 0.5f, 0); break;
            }
            if (c.turbo > 0 || c.autoT > 0) fx.Add(b + Vector3.up * 0.3f, c.autoT > 0 ? CozyData.Hex("#ff9e40") : CozyData.Hex("#73bfff"), 0.16f, 0.3f, -f * 2, 0);
        }
    }
}
