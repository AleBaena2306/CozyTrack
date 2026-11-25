using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace CozyTrack.Online
{
    // Mensajes del servidor (worker/cozytrack/lobby.js del portfolio). Un solo tipo con todos los campos
    // porque JsonUtility no admite tipos variables; «t» dice cuál es.
    [Serializable]
    public class NetOpp
    {
        public string name, car, league, kind;   // kind: human, ghost (carrera real grabada) o bot
        public int trophies, ms;                 // ms: tiempo del fantasma
        public float k;                          // nivel del bot
        public List<float> trace = new List<float>();   // fantasma: [t ms, progreso, carril, …]
    }

    [Serializable] public class NetRewards { public int coins; public string chest; }

    [Serializable]
    public class NetMsg
    {
        public string t;                 // welcome, queued, match, opp, oppfin, result, pong, error
        public int online;
        public string name, league;      // welcome / result
        public int trophies, delta;      // welcome / result
        public string track; public int laps, lead; public NetOpp opp;   // match
        public float p, o, v;            // opp: progreso (piezas), carril y velocidad
        public int ms;                   // oppfin
        public string r, reason;         // result: win/lose · finish/left/time
        public int place, you, them;     // result
        public NetRewards rewards;       // result
        public string code;              // error
    }

    [Serializable] public class TopRow { public int rank; public string name; public int score; public bool me; }
    [Serializable] public class MyRank { public int rank, score; }
    [Serializable]
    public class TopResponse
    {
        public bool ok; public string mode; public int season, total; public long seasonEnds;
        public List<TopRow> top = new List<TopRow>(); public MyRank me;
        public long MsLeft() => Math.Max(0, seasonEnds - (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
    }
    [Serializable] public class SeasonReward { public int season, place; public string tier; }
    [Serializable] public class ClaimResponse { public bool ok; public List<SeasonReward> rewards = new List<SeasonReward>(); }
    [Serializable] public class LinkStatus { public bool ok; public string state, player, name, token, error, code; public int trophies; }

    // Ligas: las mismas que en el servidor (LEAGUES de lobby.js)
    public static class Leagues
    {
        public static readonly string[] Names = { "Wood", "Bronze", "Silver", "Gold", "Platinum", "Diamond" };
        public static readonly int[] Floors = { 0, 200, 500, 1000, 1800, 3000 };
        public static readonly string[] Colors = { "#B07A4A", "#C98A4B", "#9FB3C8", "#FFCF3D", "#6FD3C7", "#7FB8FF" };
        public static int Index(int trophies) { int k = 0; for (int i = 0; i < Floors.Length; i++) if (trophies >= Floors[i]) k = i; return k; }
        public static string Name(int trophies) => Names[Index(trophies)];
        // 0→1 dentro de la liga actual (1 en la última)
        public static float Progress(int trophies)
        {
            int k = Index(trophies); if (k == Floors.Length - 1) return 1;
            return Mathf.Clamp01((trophies - Floors[k]) / (float)(Floors[k + 1] - Floors[k]));
        }
    }

    // Nombres generados («Teal Fox 42»): el juego es para niños, no hay texto libre. El servidor solo acepta nombres
    // de estas listas (worker/colorify/names.js del portfolio, compartidas con Colorify).
    public static class PlayerNames
    {
        public static readonly string[] Colors =
        {
            "Amber", "Azure", "Coral", "Cobalt", "Copper", "Cream", "Cyan", "Denim", "Fern", "Ginger",
            "Honey", "Indigo", "Ivory", "Jade", "Khaki", "Lemon", "Lilac", "Lime", "Maple", "Mauve",
            "Mint", "Moss", "Ochre", "Olive", "Peach", "Pearl", "Pine", "Plum", "Rose", "Ruby",
            "Rust", "Saffron", "Sage", "Sand", "Sky", "Slate", "Teal", "Topaz", "Violet", "Wheat",
        };
        public static readonly string[] Animals =
        {
            "Badger", "Beaver", "Bison", "Crane", "Deer", "Dingo", "Falcon", "Ferret", "Finch", "Fox",
            "Frog", "Gazelle", "Gecko", "Hare", "Heron", "Ibis", "Koala", "Kiwi", "Lemur", "Lynx",
            "Marten", "Moose", "Newt", "Ocelot", "Otter", "Owl", "Panda", "Puffin", "Quail", "Raven",
            "Robin", "Seal", "Swan", "Tapir", "Toucan", "Turtle", "Walrus", "Wolf", "Wombat", "Yak",
        };
        public static string Random() => Colors[UnityEngine.Random.Range(0, Colors.Length)] + " " + Animals[UnityEngine.Random.Range(0, Animals.Length)] + " " + UnityEngine.Random.Range(10, 100);
        public static bool IsValid(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var p = name.Split(' ');
            return p.Length == 3 && p[2].Length == 2 && p[2][0] != '0' && Array.IndexOf(Colors, p[0]) >= 0 && Array.IndexOf(Animals, p[1]) >= 0 && int.TryParse(p[2], out _);
        }
        public static string NewPlayerId() => Guid.NewGuid().ToString("N");
    }

    // HTTP con el servidor de Kovu's Pride (portfolio): ranking, recompensas de temporada y cuenta de Google
    public static class CozyApi
    {
        public const int Protocol = 1;
        public const string Fallback = "https://alejandrobaenaportfolio.com";

        // En la web del portfolio, el mismo dominio (vale para vistas previas); en portales, editor y Android, el portfolio
        public static string BaseUrl()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (KovusPride.Publishing.StorePlatform.IsPortal) return Fallback;
            try
            {
                var u = new Uri(Application.absoluteURL);
                if ((u.Scheme == "https" || u.Scheme == "http") && u.Host != "localhost" && u.Host != "127.0.0.1") return u.GetLeftPart(UriPartial.Authority);
            }
            catch (Exception) { }
#endif
            return Fallback;
        }
        public static string Api(string path) => BaseUrl() + "/api/cozytrack/" + path;
        public static string SocketUrl() { string b = BaseUrl(); return (b.StartsWith("https") ? "wss" + b.Substring(5) : "ws" + b.Substring(4)) + "/api/cozytrack/play"; }

        public static IEnumerator Get(string path, Action<string> done)
        {
            using (var req = UnityWebRequest.Get(Api(path)))
            {
                req.timeout = 10;
                yield return req.SendWebRequest();
                done?.Invoke(req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null);
            }
        }
        public static IEnumerator Post(string path, string json, Action<string> done)
        {
            using (var req = new UnityWebRequest(Api(path), "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 15;
                yield return req.SendWebRequest();
                done?.Invoke(req.result == UnityWebRequest.Result.ConnectionError ? null : req.downloadHandler?.text);
            }
        }
        public static T Parse<T>(string text) where T : class
        {
            if (string.IsNullOrEmpty(text)) return null;
            try { return JsonUtility.FromJson<T>(text); } catch (Exception) { return null; }
        }
        public static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        public static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }

    // Conexión de una carrera online: abre el WebSocket, se presenta, mantiene viva la conexión y entrega los mensajes en Update
    public class RaceConnection : IDisposable
    {
        readonly KpSocket socket; readonly string hello; bool helloSent; float nextPing, openedAt;
        public bool Welcomed { get; private set; }
        public bool EverOpened { get; private set; }
        public bool Closed => socket.State == KpSocket.Status.Closed;
        public bool TimedOut => !EverOpened && Time.realtimeSinceStartup - openedAt > 10f;

        public RaceConnection(string player, string name, string car)
        {
            hello = "{\"t\":\"hello\",\"v\":" + CozyApi.Protocol + ",\"player\":\"" + player + "\",\"name\":\"" + CozyApi.Esc(name) + "\",\"car\":\"" + CozyApi.Esc(car) + "\"}";
            socket = new KpSocket(CozyApi.SocketUrl()); openedAt = Time.realtimeSinceStartup;
        }
        public void Poll(List<NetMsg> into)
        {
            while (socket.TryReceive(out var raw))
            {
                var m = CozyApi.Parse<NetMsg>(raw);
                if (m == null || string.IsNullOrEmpty(m.t)) continue;
                if (m.t == "welcome") Welcomed = true;
                into.Add(m);
            }
            if (socket.State != KpSocket.Status.Open) return;
            EverOpened = true;
            if (!helloSent) { socket.Send(hello); helloSent = true; }
            if (Welcomed && Time.realtimeSinceStartup >= nextPing) { nextPing = Time.realtimeSinceStartup + 5f; socket.Send("{\"t\":\"ping\"}"); }
        }
        public void Queue() => socket.Send("{\"t\":\"queue\"}");
        public void Cancel() => socket.Send("{\"t\":\"cancel\"}");
        public void Pos(float p, float o, float v) => socket.Send("{\"t\":\"pos\",\"p\":" + CozyApi.F(p) + ",\"o\":" + CozyApi.F(o) + ",\"v\":" + CozyApi.F(v) + "}");
        public void Fin(bool botWon) => socket.Send("{\"t\":\"fin\",\"botWon\":" + (botWon ? "true" : "false") + "}");
        public void Dispose() => socket.Dispose();
    }
}
