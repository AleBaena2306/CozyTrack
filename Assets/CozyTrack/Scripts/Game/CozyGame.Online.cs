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

        // ---------- pantallas ----------
        void BuildOnline()
        {
            var s = MakeScreen("online"); var (_, c) = Scroll(s.transform, 40);
            var head = Row(c, 120); Btn(head, "‹ Back", Cream, Ink, () => { CloseNet(); ShowMenu(); }, 110, 38).GetComponent<LayoutElement>().preferredWidth = 260;
            var ht = T(head, "ONLINE", 72, Ink); ht.font = fontBlack; ht.gameObject.AddComponent<LayoutElement>().flexibleWidth = 3;
            // ficha del jugador: nombre, liga y trofeos con la barra hasta la siguiente liga
            var card = Card(c, Cream, "Ficha", out var f, 10); card.gameObject.AddComponent<LayoutElement>().preferredHeight = 300;
            onName = T(f.transform, "", 56, Ink); Place(onName.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(900, 70)); onName.font = fontBlack;
            onLeague = T(f.transform, "", 36, Ink); Place(onLeague.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(900, 50)); onLeague.font = fontBlack;
            var bar = Img(f.transform, new Color(0.9f, 0.88f, 0.83f), null, "Barra"); Place(bar.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 88), new Vector2(820, 34));
            onBar = Img(bar.transform, SunC, null, "Relleno"); var br = onBar.rectTransform; br.anchorMin = Vector2.zero; br.anchorMax = new Vector2(0, 1); br.offsetMin = br.offsetMax = Vector2.zero;
            onTrophies = T(f.transform, "", 30, Muted, TextAnchor.MiddleCenter, FontStyle.Normal); Place(onTrophies.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(900, 44));
            var find = CardButton(c, SunC, "Buscar", FindRace, out var ff, 16); find.gameObject.AddComponent<LayoutElement>().preferredHeight = 230;
            var fl = Outlined(T(ff.transform, "FIND A RACE", 84, Color.white), 6); Fill(fl.rectTransform, 10, 0, 10, 0); fl.font = fontBlack;
            LayoutText(c, "1 vs 1 on a surprise track · win trophies, coins and chests", 30, Muted, 60, FontStyle.Normal);
            onStatus = LayoutText(c, "", 30, Coral, 50);
            WideCard(c, CozyData.Hex("#9fd8c0"), "Leaderboard", "Top racers by trophies, and this month's season. Top 100 win chests!", OpenBoard);
            var acc = CardButton(c, CozyData.Hex("#c9b8f2"), "Cuenta", OpenAccount, out var af, 12); acc.gameObject.AddComponent<LayoutElement>().preferredHeight = 170;
            onAccount = T(af.transform, "", 34, Ink); Fill(onAccount.rectTransform, 30, 10, 30, 10); onAccount.font = fontBlack;

            // buscando rival
            var w = MakeScreen("searching"); var wc = VList(w.transform, 30, new RectOffset(60, 60, 0, 0)); Fill(wc); wc.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            searchText = LayoutText(wc, "", 56, Ink, 260); searchText.font = fontBlack;
            Btn(wc, "Cancel", Cream, Ink, () => { CloseNet(); OpenOnline(); }, 120, 40);

            // ranking
            var b = MakeScreen("board"); var (_, bc) = Scroll(b.transform, 40);
            var bh = Row(bc, 120); Btn(bh, "‹ Back", Cream, Ink, OpenOnline, 110, 38).GetComponent<LayoutElement>().preferredWidth = 260;
            var bt = T(bh, "LEADERBOARD", 64, Ink); bt.font = fontBlack; bt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 3;
            var tabs = Row(bc, 100);
            Btn(tabs, "Trophies", Cream, Ink, () => { boardMode = "trophies"; RefreshBoard(); }, 100, 38, null);
            Btn(tabs, "Season", Cream, Ink, () => { boardMode = "season"; RefreshBoard(); }, 100, 38, null);
            boardTabs["trophies"] = tabs.GetChild(0).GetComponent<Image>(); boardTabs["season"] = tabs.GetChild(1).GetComponent<Image>();
            boardInfo = LayoutText(bc, "", 30, Muted, 90, FontStyle.Normal);
            boardMe = LayoutText(bc, "", 36, Ink, 60);
            boardList = VList(bc, 8);

            // cuenta de Google (con puerta parental)
            var a = MakeScreen("account"); var (_, ac) = Scroll(a.transform, 40);
            var ah = Row(ac, 120); Btn(ah, "‹ Back", Cream, Ink, () => { linkCode = null; OpenOnline(); }, 110, 38).GetComponent<LayoutElement>().preferredWidth = 260;
            var at = T(ah, "ACCOUNT", 64, Ink); at.font = fontBlack; at.gameObject.AddComponent<LayoutElement>().flexibleWidth = 3;
            accountList = VList(ac, 22);
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
            onLeague.text = $"<color={Leagues.Colors[k]}>{Leagues.Names[k].ToUpper()}</color> LEAGUE";
            onBar.rectTransform.anchorMax = new Vector2(Leagues.Progress(t), 1);
            onTrophies.text = k < Leagues.Names.Length - 1 ? $"{t} trophies · {Leagues.Floors[k + 1] - t} to {Leagues.Names[k + 1]}" : $"{t} trophies · top league!";
            onAccount.text = SignedIn ? "Signed in with Google ✓\n<size=26>Your name and trophies are saved</size>" : "Save your progress\n<size=26>Sign in with Google (ask a grown-up)</size>";
        }

        // ---------- buscar carrera ----------
        void FindRace()
        {
            CloseNet();
            if (Application.internetReachability == NetworkReachability.NotReachable) { onStatus.text = "No internet connection."; return; }
            onStatus.text = "";
            PlayerPrefs.SetString("ctCar", choice);
            net = new RaceConnection(NetId, NetName, choice); netStage = "connecting"; netResult = null; netOpp = null;
            searchText.text = "Connecting…"; ShowScreen("searching");
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
            if (netStage == "queued") searchText.text = "Finding a racer" + new string('.', 1 + Mathf.FloorToInt(Time.time * 2) % 3);
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
            var chest = new List<string>();
            if (!netRewarded)
            {
                netRewarded = true;
                prof.coins += (m.rewards != null ? m.rewards.coins : 0) + (player != null ? player.coins : 0);
                if (m.rewards != null && !string.IsNullOrEmpty(m.rewards.chest))
                    chest.Add(AddChest(ChestKindFor(m.rewards.chest)) ? "Chest added to your slots: open it on the home screen." : "Slots full: +25 coins.");
                SaveProfile();
            }
            string opp = netOpp != null ? netOpp.name : "Your rival";
            string msg = win ? (m.reason == "left" ? $"{opp} left the race. You win!" : "You won the race!")
                             : m.reason == "left" ? "You left the race." : $"{opp} crossed the line first. Next one's yours!";
            var rows = new List<string>
            {
                $"You\t{(m.you > 0 ? Fmt(m.you / 1000f) : "—")}",
                $"{opp}\t{(m.them > 0 ? Fmt(m.them / 1000f) : "—")}",
                $"Trophies\t{(m.delta >= 0 ? "+" : "")}{m.delta}  ({m.trophies})",
                $"League\t{Leagues.Name(m.trophies)}",
                $"Coins\t+{(m.rewards != null ? m.rewards.coins : 0) + (player != null ? player.coins : 0)}",
            };
            ShowResults(win ? "1st" : "2nd", msg, rows, chest.Count > 0 ? chest : null);
            CloseNet();
        }

        // Mientras los cofres sean de 2 tipos («normal» y «oro»), los 4 niveles del servidor se reparten así
        static string ChestKindFor(string tier) => tier == "epic" || tier == "legendary" ? "oro" : "normal";

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
                    boardInfo.text = $"Season {r.season} · ends in {h / 24}d {h % 24}h\n<size=26>#1 legendary · #2-3 epic · #4-10 rare · #11-100 common chest</size>";
                }
                else boardInfo.text = $"{r.total} racers with trophies";
                boardMe.text = r.me != null && r.me.rank > 0 ? $"You: #{r.me.rank} · {r.me.score}" : "Race online to get on the board!";
                for (int k = 0; k < r.top.Count; k++)
                {
                    var row = r.top[k];
                    var card = Card(boardList, row.me ? SunC : k < 3 ? CozyData.Hex("#FFF4E0") : Cream, "Fila", out var f, 6);
                    card.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
                    var rk = T(f.transform, "#" + row.rank, 36, Ink, TextAnchor.MiddleLeft); Place(rk.rectTransform, new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(140, 70)); rk.font = fontBlack;
                    var nm = T(f.transform, row.name, 36, Ink, TextAnchor.MiddleLeft); Place(nm.rectTransform, new Vector2(0, 0.5f), new Vector2(170, 0), new Vector2(560, 70));
                    var sc = T(f.transform, row.score.ToString(), 36, Ink, TextAnchor.MiddleRight); Place(sc.rectTransform, new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(220, 70)); sc.font = fontBlack;
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
