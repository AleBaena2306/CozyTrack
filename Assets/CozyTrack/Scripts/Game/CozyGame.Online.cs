using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CozyTrack.Online;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace CozyTrack
{
    // Modo Online (como en Colorify): carreras 1 contra 1 por WebSocket con el servidor del portfolio
    // (worker/cozytrack/lobby.js), trofeos y ligas, ranking total y de temporada, recompensas por puesto
    // (en cada carrera y al acabar la temporada), fantasmas y bots según tus trofeos, y cuenta de Google.
    //
    // Juego para niños: nombre siempre generado («Teal Fox 42»), sin chat ni texto libre, y el inicio de sesión
    // con Google va detrás de una puerta parental. El rival online no choca ni usa objetos: solo cuenta quién llega antes.
    public partial class CozyGame
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void KP_OpenOnRelease(string url);
#endif
        RaceConnection net; readonly List<NetMsg> netIn = new List<NetMsg>();
        string netStage = "";                    // "", connecting, queued, racing, done
        NetOpp netOpp; RaceCar oppCar; NetMsg netResult; float netSendT; bool netFinSent, netRewarded;
        Text onName, onLeague, onTrophies, onStatus, onAccount, searchText, boardInfo, boardMe; Image onBar; Transform boardList, accountList;
        string boardMode = "trophies"; readonly Dictionary<string, Image> boardTabs = new Dictionary<string, Image>();
        string linkCode; Coroutine linkPoll;

        // ---------- identidad (anónima) y cuenta ----------
        string NetId
        {
            get { var s = PlayerPrefs.GetString("ctPlayer", ""); if (s.Length != 32) { s = PlayerNames.NewPlayerId(); PlayerPrefs.SetString("ctPlayer", s); PlayerPrefs.Save(); } return s; }
        }
        string NetName
        {
            get { var s = PlayerPrefs.GetString("ctName", ""); if (!PlayerNames.IsValid(s)) { s = PlayerNames.Random(); PlayerPrefs.SetString("ctName", s); PlayerPrefs.Save(); } return s; }
        }
        int NetTrophies { get => PlayerPrefs.GetInt("ctTrophies", 0); set { PlayerPrefs.SetInt("ctTrophies", Mathf.Max(0, value)); PlayerPrefs.Save(); } }
        string NetToken => PlayerPrefs.GetString("ctToken", "");
        bool SignedIn => NetToken.Length >= 32;

        // ---------- pantallas (diseño «Online · 1 vs 1» del lienzo) ----------
        Image onShield; Transform onRoad; Text searchMe, searchMeTr, searchClock; float searchT;
        void BuildOnline()
        {
            var s = MakeScreen("online").transform;
            BackButton(s, () => { CloseNet(); ShowMenu(); });
            TB(s, "ONLINE", 38, Ink, true, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 120, 16, 12, 40));
            Cap(s, "1 VS 1 · SURPRISE TRACK", 11, null, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 120, 16, 52, 16));
            Band(s, 330, 150, -7);
            ShowroomImage(s, 340, 230, 140);
            // ficha del corredor con su liga
            var card = Sticker(s, Cream, out var f, 18, 3, 4, -1, "Ficha"); TopBox(card, 18, 18, 92, 156);
            onShield = Icn(f.transform, null, 12, 30, 64, 72);
            Cap(f.transform, "YOUR RACER NAME", 11, null, TextAnchor.MiddleLeft).rectTransform.Let(r => TopBox(r, 92, 12, 12, 14));
            onName = TB(f.transform, "", 26, Ink, true, TextAnchor.MiddleLeft); TopBox(onName.rectTransform, 92, 12, 26, 32);
            Icn(f.transform, B("trophy"), 92, 64, 20, 20);
            onTrophies = TB(f.transform, "", 19, Ink, true, TextAnchor.MiddleLeft); TopBox(onTrophies.rectTransform, 116, 12, 62, 24);
            onLeague = Cap(f.transform, "", 11, null, TextAnchor.MiddleRight); TopBox(onLeague.rectTransform, 150, 14, 66, 18);
            var br = R("Barra", f.transform); TopBox(br, 92, 14, 92, 14); onBar = Bar(br, SunC, 0); Fill((RectTransform)onBar.transform.parent.parent);
            onStatus = Cap(f.transform, "", 11, null, TextAnchor.MiddleLeft); TopBox(onStatus.rectTransform, 92, 12, 112, 16);
            // camino de ligas
            onRoad = R("Ligas", s); TopBox((RectTransform)onRoad, 18, 18, 262, 56);
            var rh = onRoad.gameObject.AddComponent<HorizontalLayoutGroup>(); rh.childControlWidth = rh.childControlHeight = true; rh.childForceExpandWidth = rh.childForceExpandHeight = true; rh.childAlignment = TextAnchor.LowerCenter;
            // buscar carrera
            var find = StickerButton(s, SunC, FindRace, out var ff, 20, 3.5f, 6, -1.5f, "Buscar"); TopBox((RectTransform)find.transform, 20, 20, 540, 104);
            var chk = Checker(ff.transform); var ckr = chk.rectTransform; ckr.anchorMin = Vector2.zero; ckr.anchorMax = new Vector2(0, 1); ckr.pivot = new Vector2(0, 0.5f); ckr.sizeDelta = new Vector2(42 * K, 0); ckr.anchoredPosition = Vector2.zero;
            TB(ff.transform, "FIND A RACE", 38, Ink, true, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 58, 12, 10, 26));
            Cap(ff.transform, "WIN: +TROPHIES · COINS · CHEST", 12, null, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 60, 64, 10, 12));
            var lb = StickerButton(s, MintB, OpenBoard, out var lf, 18, 3.5f, 5, 1.2f, "Ranking"); HalfBox((RectTransform)lb.transform, true, 672, 84);
            TB(lf.transform, "LEADERBOARD", 20, Ink, true, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 14, 36, 6, 14));
            Cap(lf.transform, "TOP 100 WIN CHESTS", 10, null, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 14, 16, 6, 50));
            var acc = StickerButton(s, Purple, OpenAccount, out var af, 18, 3.5f, 5, -1, "Cuenta"); HalfBox((RectTransform)acc.transform, false, 672, 84);
            onAccount = TB(af.transform, "", 20, Cream, true, TextAnchor.MiddleLeft); Inset(onAccount.rectTransform, 14, 36, 6, 14);
            Cap(af.transform, "WITH A GROWN-UP", 10, Cream, TextAnchor.MiddleLeft).rectTransform.Let(r => Inset(r, 14, 16, 6, 50));
            TB(s, "No chat · names are made up for you · racers with similar trophies first", 11, Ink, false).rectTransform.Let(r => BotBox(r, 20, 20, 18, 20));

            // buscando rival: tú contra el hueco del rival
            var w = MakeScreen("searching").transform;
            var cancel = StickerButton(w, Cream, () => { CloseNet(); OpenOnline(); }, out var cf, 12, 3, 4, -3, "Cancelar"); TopAt((RectTransform)cancel.transform, 14, 18, 104, 34);
            TB(cf.transform, "✕ CANCEL", 14, Ink, false);
            TB(w, "ONLINE", 38, Ink, true, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 130, 16, 12, 40));
            Cap(w, "FINDING A RACER", 11, null, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 130, 16, 52, 16));
            var tk = Sticker(w, Cream, out var tkf, 16, 3, 4, -1.5f, "Sorpresa"); TopBox(tk, 40, 40, 116, 84);
            Cap(tkf.transform, "SURPRISE TRACK", 11).rectTransform.Let(r => TopBox(r, 4, 4, 10, 16));
            var chips = R("Pistas", tkf.transform); TopBox(chips, 12, 12, 34, 38);
            var chl = chips.gameObject.AddComponent<HorizontalLayoutGroup>(); chl.spacing = 6 * K; chl.childControlWidth = chl.childControlHeight = true; chl.childForceExpandWidth = chl.childForceExpandHeight = true;
            string[] cbg = { "#A8D8B9", "#F2D6C4", "#9FD8C0", "#8FC8F0" };
            for (int i = 0; i < CozyData.Circuits.Length; i++) { Sticker(chips, CozyData.Hex(cbg[i % 4]), out var chf, 10, 2.5f, 0, 0, "Pista"); Cap(chf.transform, CozyData.Circuits[i].n.ToUpper(), 10).rectTransform.Let(r => Inset(r, 2, 0, 2, 0)); }
            Band(w, 250, 230, -7);
            var me = Sticker(w, Cream, out var mf, 16, 3, 4, -4, "Yo"); TopAt(me, 16, 286, 142, 120);
            var slot = Img(mf.transform, new Color(0.17f, 0.14f, 0.25f, 0.08f), null, "Coche"); Radius(slot, 12); TopBox(slot.rectTransform, 10, 10, 10, 60);
            Cap(slot.transform, "your car", 11);
            searchMe = TB(mf.transform, "", 16, Ink); TopBox(searchMe.rectTransform, 4, 4, 74, 20);
            searchMeTr = Cap(mf.transform, "", 12); TopBox(searchMeTr.rectTransform, 4, 4, 96, 16);
            var vs = TB(w, "VS", 44, Cream); TopCenter(vs.rectTransform, 320, 80, 54); vs.gameObject.AddComponent<Outline>().effectColor = Ink; Tilt(vs.transform, -7);
            var them = Sticker(w, SkyB, out var thf, 16, 3, 4, 3, "Rival"); TopAt(them, 16, 286, 142, 120, true);
            TB(thf.transform, "?", 44, Ink).rectTransform.Let(r => TopBox(r, 10, 10, 10, 60));
            searchText = TB(thf.transform, "Searching…", 15, Ink); TopBox(searchText.rectTransform, 4, 4, 74, 20);
            Cap(thf.transform, "SIMILAR TROPHIES", 11).rectTransform.Let(r => TopBox(r, 4, 4, 96, 16));
            searchClock = TB(w, "0:00", 56, Ink); TopCenter(searchClock.rectTransform, 540, 200, 60);
            Cap(w, "WAITING", 12).rectTransform.Let(r => TopCenter(r, 598, 200, 16));
            var note = Sticker(w, Cream, out var nf, 14, 3, 4, 1, "Nota"); TopBox(note, 28, 28, 640, 76); Tape(note, 44, 0, -4, -10);
            TB(nf.transform, "Nobody around after 6 s? You race a <b>replay</b> of a real racer with similar trophies, or a <b>BOT</b> at your level. They're always labelled.", 12, Ink, false).rectTransform.Let(r => Inset(r, 12, 8, 12, 8));

            // ranking
            var b = MakeScreen("board").transform;
            BackButton(b, OpenOnline);
            TB(b, "RANKING", 36, Ink, true, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 120, 16, 12, 40));
            Cap(b, "TOP RACERS", 11, null, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 120, 16, 52, 16));
            var tabs = R("Pestanas", b); TopBox(tabs, 20, 20, 86, 46);
            var th = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); th.spacing = 12 * K; th.childControlWidth = th.childControlHeight = true; th.childForceExpandWidth = th.childForceExpandHeight = true;
            StickerButton(tabs, Cream, () => { boardMode = "trophies"; RefreshBoard(); }, out var t1, 14, 3, 3, -1, "Trofeos"); Cap(t1.transform, "TROPHIES", 14); boardTabs["trophies"] = t1;
            StickerButton(tabs, Ink, () => { boardMode = "season"; RefreshBoard(); }, out var t2, 14, 3, 3, 1, "Temporada"); Cap(t2.transform, "SEASON", 14); boardTabs["season"] = t2;
            var info = Sticker(b, Blue, out var inf, 16, 3, 4, -0.8f, "Temporada"); TopBox(info, 18, 18, 150, 66);
            boardInfo = TB(inf.transform, "", 15, Cream, true); Inset(boardInfo.rectTransform, 10, 4, 10, 4);
            var holder = R("Lista", b); holder.anchorMin = Vector2.zero; holder.anchorMax = Vector2.one; holder.offsetMin = new Vector2(0, 96 * K); holder.offsetMax = new Vector2(0, -228 * K);
            var (_, bc) = Scroll(holder, 6); boardList = bc; bc.GetComponent<VerticalLayoutGroup>().spacing = 8 * K;
            bc.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(Mathf.RoundToInt(18 * K), Mathf.RoundToInt(18 * K), 8, 20);
            var meBar = Sticker(b, SunC, out var mbf, 16, 3.5f, 5, -1, "Tu puesto"); BotBox(meBar, 14, 14, 20, 62);
            boardMe = TB(mbf.transform, "", 18, Ink, true, TextAnchor.MiddleLeft); Inset(boardMe.rectTransform, 14, 0, 14, 0);

            // cuenta de Google (con puerta parental)
            var a = MakeScreen("account").transform;
            BackButton(a, () => { linkCode = null; OpenOnline(); });
            TB(a, "SAVE", 38, Ink, true, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 120, 16, 12, 40));
            Cap(a, "YOUR PROGRESS", 11, null, TextAnchor.MiddleRight).rectTransform.Let(r => TopBox(r, 120, 16, 52, 16));
            var ah = R("Contenido", a); ah.anchorMin = Vector2.zero; ah.anchorMax = Vector2.one; ah.offsetMin = Vector2.zero; ah.offsetMax = new Vector2(0, -84 * K);
            var (_, ac) = Scroll(ah, 20); accountList = ac;
        }

        void OpenOnline()
        {
            state = "menu"; ShowScreen("online"); RefreshOnline();
            StartCoroutine(ClaimSeasonRewards());
        }
        void RefreshOnline()
        {
            if (!onName) return;
            int t = NetTrophies, k = Leagues.Index(t);
            onName.text = NetName;
            onShield.sprite = B("league_" + Leagues.Names[k].ToLower());
            onTrophies.text = N0(t);
            onLeague.text = Leagues.Names[k].ToUpper() + " LEAGUE";
            SetBar(onBar, Leagues.Progress(t));
            if (string.IsNullOrEmpty(onStatus.text) || onStatus.text.Contains(" to ") || onStatus.text.Contains("top league"))
                onStatus.text = k < Leagues.Names.Length - 1 ? $"{N0(Leagues.Floors[k + 1] - t)} more to {Leagues.Names[k + 1]}" : "Top league!";
            onAccount.text = SignedIn ? "SIGNED IN ✓" : "SAVE PROGRESS";
            foreach (Transform c in onRoad) Destroy(c.gameObject);
            for (int i = 0; i < Leagues.Names.Length; i++)
            {
                var cell = R(Leagues.Names[i], onRoad);
                var ic = Icn(cell, B("league_" + Leagues.Names[i].ToLower()), 0, 0, 30, 34); ic.rectTransform.Let(r => { r.anchorMin = r.anchorMax = new Vector2(0.5f, 1); r.anchoredPosition = new Vector2(0, -(i == k ? 18 : 20) * K); r.sizeDelta = new Vector2((i == k ? 36 : 30) * K, (i == k ? 40 : 34) * K); });
                ic.color = i <= k ? Color.white : new Color(1, 1, 1, 0.45f);
                Cap(cell, Leagues.Names[i].ToUpper(), 9).rectTransform.Let(r => BotBox(r, 0, 0, 0, 14));
            }
        }

        // ---------- buscar carrera ----------
        void FindRace()
        {
            CloseNet();
            if (Application.internetReachability == NetworkReachability.NotReachable) { onStatus.text = "No internet connection."; return; }
            onStatus.text = ""; searchT = 0; searchMe.text = NetName; searchMeTr.text = N0(NetTrophies) + " TROPHIES";
            PlayerPrefs.SetString("ctCar", choice);
            net = new RaceConnection(NetId, NetName, choice); netStage = "connecting"; netResult = null; netOpp = null;
            searchText.text = "Connecting…"; searchClock.text = "0:00"; ShowScreen("searching");
        }
        void CloseNet()
        {
            if (net != null) { net.Dispose(); net = null; }
            netStage = ""; netIn.Clear();
        }

        void OnlineTick(float dt)
        {
            if (net == null) return;
            net.Poll(netIn);
            foreach (var m in netIn.ToList()) HandleNet(m);
            netIn.Clear();
            if (net == null) return;
            if (netStage == "connecting" && (net.TimedOut || net.Closed)) { CloseNet(); OpenOnline(); onStatus.text = "Couldn't reach the server. Try again in a moment."; return; }
            if (net.Closed && netStage != "done")
            {
                bool racing = netStage == "racing"; CloseNet();
                if (racing && mode == "online") { netResult = null; state = "end"; ShowResults("—", "Connection lost. The race didn't count.", new List<string>(), null); }
                else { OpenOnline(); onStatus.text = "Connection lost."; }
                return;
            }
            if (netStage == "queued" || netStage == "connecting") { searchT += dt; searchClock.text = $"{(int)searchT / 60}:{(int)searchT % 60:00}"; searchText.text = "Searching" + new string('.', 1 + Mathf.FloorToInt(Time.time * 2) % 3); }
            if (netStage == "racing" && state == "race" && player != null && player.done < 0)
            {
                netSendT -= dt;
                if (netSendT <= 0) { netSendT = 0.1f; net.Pos(Prog(player), player.o, player.v); }
            }
        }

        void HandleNet(NetMsg m)
        {
            switch (m.t)
            {
                case "welcome":
                    if (PlayerNames.IsValid(m.name)) PlayerPrefs.SetString("ctName", m.name);
                    NetTrophies = m.trophies; netStage = "queued"; net.Queue(); break;
                case "match":
                    netOpp = m.opp; netStage = "racing"; netFinSent = false; netRewarded = false; netResult = null;
                    mode = "online"; levelNum = 0;
                    BuildCircuit(CozyData.Circuit(m.track));
                    StartRace();
                    break;
                case "opp":
                    if (oppCar != null && oppCar.remote) { oppCar.netP = m.p; oppCar.netO = m.o; oppCar.netV = m.v; oppCar.netAt = Time.time; }
                    break;
                case "oppfin":
                    if (oppCar != null && oppCar.done < 0) oppCar.done = m.ms / 1000f;
                    break;
                case "result":
                    netResult = m; netStage = "done"; NetTrophies = m.trophies;
                    if (mode == "online") ShowOnlineResult();
                    break;
                case "error":
                    CloseNet(); OpenOnline();
                    onStatus.text = m.code == "update" ? "Update the game to race online." : "The server said no. Try again later.";
                    break;
            }
        }

        // StartRace en modo online: solo el jugador y el rival (persona, carrera grabada o bot); sin objetos
        string OnlineCars()
        {
            string car = CozyData.Cars.Any(x => x.name == netOpp.car) ? netOpp.car : "Dart";
            oppCar = MakeCar(car, 2, false); oppCar.name = netOpp.name;
            if (netOpp.kind == "bot") { oppCar.k = netOpp.k; oppCar.kStraight = netOpp.k + 0.02f; oppCar.react = 0.15f; }
            else { oppCar.remote = true; oppCar.netP = Prog(oppCar); oppCar.netO = oppCar.o; oppCar.netAt = Time.time; oppCar.shownP = oppCar.netP; }
            cars.Add(oppCar);
            string tag = netOpp.kind == "ghost" ? " · replay" : "";
            return $"vs {netOpp.name}{tag} · {netOpp.trophies} trophies";
        }

        // Rival remoto: se coloca con lo que manda el servidor (interpolado) o con la carrera grabada del fantasma
        void StepRemote(RaceCar c, float dt)
        {
            float target, o;
            if (netOpp != null && netOpp.kind == "ghost" && netOpp.trace.Count >= 6)
            {
                float t = state == "count" ? 0 : raceT * 1000f; var s = netOpp.trace; int n = s.Count / 3, k = 0;
                while (k < n - 2 && s[(k + 1) * 3] <= t) k++;
                int a = k * 3, b = Mathf.Min(n - 1, k + 1) * 3; float f = s[b] > s[a] ? Mathf.Clamp01((t - s[a]) / (s[b] - s[a])) : 1;
                target = Mathf.Lerp(s[a + 1], s[b + 1], f); o = Mathf.Lerp(s[a + 2], s[b + 2], f);
                c.v = (s[b + 1] - s[a + 1]) / Mathf.Max(0.05f, (s[b] - s[a]) / 1000f) * Track.PieceLen(c.i, c.o);
                if (c.done < 0 && raceT * 1000f >= netOpp.ms) c.done = netOpp.ms / 1000f;
                c.shownP = target;
            }
            else
            {
                // extrapola con la última velocidad conocida (como mucho 0,5 s) y se acerca suavemente
                float rate = c.netV / Mathf.Max(1f, Track.PieceLen(c.i, c.o));
                target = c.netP + rate * Mathf.Min(0.5f, Time.time - c.netAt);
                c.shownP += (target - c.shownP) * Mathf.Min(1, dt * 8); o = c.netO; c.v = c.netV;
            }
            c.o += (o - c.o) * Mathf.Min(1, dt * 8);
            float p = Mathf.Max(0, c.shownP); int N = Track.N, w = Mathf.FloorToInt(p / N); float rem = p - w * N; int i = Mathf.Clamp(Mathf.FloorToInt(rem), 0, N - 1);
            c.wraps = w; c.i = i; c.u = rem - i;
            int lane = 0; for (int l = 1; l < 4; l++) if (Mathf.Abs(Track.LaneO(l) - c.o) < Mathf.Abs(Track.LaneO(lane) - c.o)) lane = l;
            c.lane = lane;
        }

        // Finish() en modo online: avisa al servidor y espera su veredicto (si no ha llegado ya)
        void OnlineFinish()
        {
            state = "end";
            if (net != null && !netFinSent)
            {
                netFinSent = true;
                bool botWon = oppCar != null && !oppCar.remote && oppCar.done >= 0 && oppCar.done < player.done;
                net.Fin(botWon);
            }
            if (netResult != null) ShowOnlineResult();
            else ShowResults(Fmt(player.done), "Waiting for the result…", new List<string>(), null);
        }

        void ShowOnlineResult()
        {
            var m = netResult; if (m == null) return;
            state = "end";
            bool win = m.r == "win";
            int coins = (m.rewards != null ? m.rewards.coins : 0) + (player != null ? player.coins : 0);
            string tier = m.rewards != null && !string.IsNullOrEmpty(m.rewards.chest) ? ChestKindFor(m.rewards.chest) : null; bool slot = true;
            if (!netRewarded)
            {
                netRewarded = true; prof.coins += coins;
                if (tier != null) slot = AddChest(tier);
                SaveProfile();
            }
            string opp = netOpp != null ? netOpp.name : "Your rival";
            var v = new ResultView
            {
                big = win ? $"1<size={Mathf.RoundToInt(24 * K)}>ST</size>" : $"2<size={Mathf.RoundToInt(24 * K)}>ND</size>",
                sub = win ? (m.reason == "left" ? "RIVAL LEFT!" : "YOU WON!") : (m.reason == "left" ? "YOU LEFT" : "SO CLOSE!"), disc = win ? SunC : Paper,
                back = "‹ ONLINE", onBack = () => { CloseNet(); mode = "race"; MenuGrid(); OpenOnline(); },
                primary = "RACE AGAIN", onPrimary = () => { mode = "race"; FindRace(); }, secondary = "DONE", onSecondary = () => { CloseNet(); mode = "race"; MenuGrid(); OpenOnline(); },
            };
            var me = (win ? "1" : "2", NetName + " (you)", m.you > 0 ? Fmt(m.you / 1000f) : "—", true); var them = (win ? "2" : "1", opp, m.them > 0 ? Fmt(m.them / 1000f) : "—", false);
            if (win) { v.rows.Add(me); v.rows.Add(them); } else { v.rows.Add(them); v.rows.Add(me); }
            v.stats.Add(("+" + coins, "COINS")); v.stats.Add(((m.delta >= 0 ? "+" : "") + m.delta, "TROPHIES")); v.stats.Add((Leagues.Name(m.trophies), "LEAGUE"));
            if (tier != null) { v.chestTier = tier; v.chestTitle = TierName(tier) + " chest!"; v.chestSub = slot ? $"Added to your slots · opens in {ChestSeconds(tier) / 60} min" : "Slots full: +25 coins instead"; }
            else v.tip = win ? "Nice racing!" : "Lose: fewer trophies against stronger racers. Next one's yours!";
            OfferDouble(coins);
            ShowResultView(v);
            CloseNet();
        }

        // Mientras los cofres sean de 2 tipos («normal» y «oro»), los 4 niveles del servidor se reparten así
        static string ChestKindFor(string tier) => string.IsNullOrEmpty(tier) ? "common" : tier;

        // ---------- recompensas de temporada (top 100 del mes) ----------
        IEnumerator ClaimSeasonRewards()
        {
            ClaimResponse r = null;
            yield return CozyApi.Post("claim", "{\"player\":\"" + NetId + "\"}", t => r = CozyApi.Parse<ClaimResponse>(t));
            if (r == null || !r.ok || r.rewards.Count == 0) yield break;
            foreach (var rw in r.rewards)
            {
                AddChest(ChestKindFor(rw.tier));
                Toast($"Season {rw.season}: you finished #{rw.place}! {char.ToUpper(rw.tier[0]) + rw.tier.Substring(1)} chest");
            }
            SaveProfile();
        }

        // ---------- ranking ----------
        void OpenBoard() { ShowScreen("board"); RefreshBoard(); }
        void RefreshBoard()
        {
            foreach (var kv in boardTabs) { bool on = kv.Key == boardMode; kv.Value.color = on ? Ink : Cream; kv.Value.GetComponentInChildren<Text>().color = on ? Cream : Ink; }
            boardInfo.transform.parent.parent.parent.gameObject.SetActive(true);
            foreach (Transform t in boardList) Destroy(t.gameObject);
            boardMe.text = ""; boardInfo.text = "Loading…";
            StartCoroutine(CozyApi.Get($"top?mode={boardMode}&player={NetId}", text =>
            {
                var r = CozyApi.Parse<TopResponse>(text);
                if (r == null || !r.ok) { boardInfo.text = "Couldn't load the leaderboard."; return; }
                if (r.mode != boardMode) return;
                if (boardMode == "season")
                {
                    long h = r.MsLeft() / 3_600_000;
                    boardInfo.text = $"Season {r.season} · ends in {h / 24}d {h % 24}h\n<size={Mathf.RoundToInt(11 * K)}>#1 LEGENDARY · #2–3 EPIC · #4–10 RARE · #11–100 COMMON CHEST</size>";
                }
                else boardInfo.text = $"{r.total} racers with trophies";
                boardMe.text = r.me != null && r.me.rank > 0 ? $"#{r.me.rank}   {NetName} · you   {N0(r.me.score)}" : "Race online to get on the board!";
                string[] medal = { "#FFCF3D", "#D3D2DA", "#E0A36A" };
                for (int k = 0; k < r.top.Count; k++)
                {
                    var row = r.top[k];
                    var card = Sticker(boardList, row.me ? SunC : k < 3 ? Cream : CozyData.Hex("#F6E6C8"), out var f, 12, 3, 3, k % 2 == 0 ? -0.6f : 0.6f, "Fila");
                    card.gameObject.AddComponent<LayoutElement>().preferredHeight = 44 * K;
                    var m = Sticker(f.transform, k < 3 ? CozyData.Hex(medal[k]) : Cream, out var mf, 8, 2.5f, 0, 0, "Puesto"); m.anchorMin = m.anchorMax = new Vector2(0, 0.5f); m.sizeDelta = new Vector2(38 * K, 30 * K); m.anchoredPosition = new Vector2(28 * K, 0);
                    TB(mf.transform, row.rank.ToString(), 15, Ink);
                    TB(f.transform, row.name, 15, Ink, false, TextAnchor.MiddleLeft).rectTransform.Let(rr => Inset(rr, 56, 0, 80, 0));
                    TB(f.transform, N0(row.score), 17, Ink, true, TextAnchor.MiddleRight).rectTransform.Let(rr => Inset(rr, 0, 0, 12, 0));
                }
            }));
        }

        // ---------- cuenta de Google ----------
        void OpenAccount() { ShowScreen("account"); RefreshAccount(); }
        void RefreshAccount(string note = null)
        {
            foreach (Transform t in accountList) Destroy(t.gameObject);
            if (SignedIn)
            {
                LayoutText(accountList, $"Signed in as\n<size=60>{NetName}</size>", 40, Ink, 200);
                LayoutText(accountList, "Your racer name and trophies are saved to your Google account, so you keep them on any device. We only store an anonymous account ID, never your email.", 30, Muted, 170, FontStyle.Normal);
                Btn(accountList, "Sign out", Cream, Ink, () => { BecomeGuest(); RefreshAccount("Signed out. Sign in again to get your trophies back."); }, 120, 40);
                Btn(accountList, "Delete account", Coral, Color.white, () => ParentalGate(DeleteAccount), 120, 40);
            }
            else if (linkCode != null)
            {
                string url = CozyApi.Api("link?code=" + linkCode).Replace("https://", "");
                LayoutText(accountList, "Finish signing in on the page that just opened, then come back here.", 40, Ink, 200);
                LayoutText(accountList, "If it didn't open, go to:\n" + url, 28, Muted, 120, FontStyle.Normal);
                Btn(accountList, "Cancel", Cream, Ink, () => { linkCode = null; RefreshAccount(); }, 120, 40);
            }
            else
            {
                LayoutText(accountList, "Keep your racer name and trophies on any device", 44, Ink, 160);
                LayoutText(accountList, "Signing in uses a Google account, so a grown-up needs to help. We only store an anonymous account ID, never your email or name.", 30, Muted, 170, FontStyle.Normal);
                Btn(accountList, "Ask a grown-up to sign in", SunC, Ink, () => ParentalGate(StartGoogleSignIn), 140, 42);
            }
            if (note != null) LayoutText(accountList, note, 30, Coral, 90);
        }

        // Puerta parental: una multiplicación que un niño pequeño no suele saber
        void ParentalGate(System.Action ok)
        {
            foreach (Transform t in accountList) Destroy(t.gameObject);
            int a = Random.Range(6, 10), b = Random.Range(6, 10), right = a * b;
            LayoutText(accountList, "For grown-ups", 56, Ink, 90).font = fontBlack;
            LayoutText(accountList, $"What is {a} × {b}?", 48, Ink, 120);
            var opts = new List<int> { right, right + Random.Range(2, 6), right - Random.Range(2, 6) }.OrderBy(_ => Random.value).ToList();
            var row = Row(accountList, 140);
            foreach (var v in opts) { int vv = v; Btn(row, v.ToString(), Cream, Ink, () => { if (vv == right) ok(); else RefreshAccount("That's not right."); }, 140, 48); }
            Btn(accountList, "Back", Cream, Muted, () => RefreshAccount(), 110, 36);
        }

        void StartGoogleSignIn()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var chars = new char[8]; for (int i = 0; i < 8; i++) chars[i] = alphabet[Random.Range(0, alphabet.Length)];
            linkCode = new string(chars);
            string url = CozyApi.Api("link?code=" + linkCode);
#if UNITY_WEBGL && !UNITY_EDITOR
            KP_OpenOnRelease(url);
#else
            Application.OpenURL(url);
#endif
            RefreshAccount();
            string code = linkCode;
            string json = "{\"player\":\"" + NetId + "\",\"name\":\"" + CozyApi.Esc(NetName) + "\",\"code\":\"" + code + "\"}";
            StartCoroutine(CozyApi.Post("link/start", json, text =>
            {
                var r = CozyApi.Parse<LinkStatus>(text);
                if (linkCode != code) return;
                if (r == null || !r.ok) { linkCode = null; RefreshAccount("Couldn't reach the server. Try again."); return; }
                if (r.code != null && r.code != code) { linkCode = code = r.code; }
                if (linkPoll != null) StopCoroutine(linkPoll);
                linkPoll = StartCoroutine(PollLink(code));
            }));
        }

        IEnumerator PollLink(string code)
        {
            float until = Time.realtimeSinceStartup + 15 * 60f;
            while (Time.realtimeSinceStartup < until && linkCode == code)
            {
                yield return new WaitForSecondsRealtime(2f);
                LinkStatus r = null;
                yield return CozyApi.Post("link/status", "{\"code\":\"" + code + "\"}", t => r = CozyApi.Parse<LinkStatus>(t));
                if (linkCode != code) yield break;
                if (r == null) continue;
                if (!r.ok) { linkCode = null; RefreshAccount("The sign-in code expired. Try again."); yield break; }
                if (r.state != "done") continue;
                PlayerPrefs.SetString("ctPlayer", r.player); PlayerPrefs.SetString("ctName", r.name); PlayerPrefs.SetString("ctToken", r.token);
                NetTrophies = r.trophies; linkCode = null;
                RefreshAccount(); RefreshOnline();
                yield break;
            }
        }

        // Cerrar sesión: este dispositivo vuelve a correr como invitado con otro nombre (la cuenta sigue en el servidor)
        void BecomeGuest()
        {
            PlayerPrefs.DeleteKey("ctToken"); PlayerPrefs.DeleteKey("ctPlayer"); PlayerPrefs.DeleteKey("ctName"); NetTrophies = 0;
            CloseNet(); RefreshOnline();
        }
        void DeleteAccount()
        {
            StartCoroutine(CozyApi.Post("account/delete", "{\"token\":\"" + NetToken + "\"}", text =>
            {
                var r = CozyApi.Parse<LinkStatus>(text);
                if (r != null && r.ok) { BecomeGuest(); RefreshAccount("Your account and its online data were deleted."); }
                else RefreshAccount("Couldn't delete the account. Try again.");
            }));
        }
    }
}
