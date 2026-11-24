using System.Collections.Generic;
using UnityEngine;

namespace CozyTrack
{
    public struct Pose { public Vector3 pos; public float h, roll; }   // h: rumbo (rad, horario visto desde arriba); roll: alabeo (rad)

    public class Piece
    {
        public string k; public bool straight = true, cross; public char twist; // 'i' entrada, 'r' techo, 'o' salida
        public float L, R = 8f, A; public int s;                              // s = +1 curva a la izquierda, -1 a la derecha
        public Vector3 P; public float yaw;
    }

    // Geometría del circuito. Eje X = derecha, Z = adelante. El desplazamiento lateral o es positivo hacia la derecha.
    public static class Track
    {
        public const float LaneW = 1.2f, Half = 2.7f, TwistR = 1.9f; public const int NL = 4;
        public static readonly List<Piece> Pieces = new List<Piece>();
        public static int N => Pieces.Count;
        public static string[] SurfIds = new string[0];
        public static float GripMul = 1f;
        public static float LaneO(float k) => (k - 1.5f) * LaneW;

        public static Piece Parse(string k)
        {
            var p = new Piece { k = k };
            float Num(string t, float d) => float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : d;
            if (k == "SF") p.L = 8;
            else if (k[0] == 'S') p.L = Num(k.Substring(1), 8);
            else if (k[0] == 'X') { p.L = Num(k.Substring(1), 5.4f); p.cross = true; }
            else if (k[0] == 'T') { p.L = Num(k.Substring(2), 8); p.twist = k[1]; }
            else
            {
                // C90L, C45R, C90L5 (radio 5)…
                p.straight = false;
                int d = k.IndexOfAny(new[] { 'L', 'R' });
                p.A = Num(k.Substring(1, d - 1), 90) * Mathf.Deg2Rad;
                p.s = k[d] == 'L' ? 1 : -1;
                p.R = d + 1 < k.Length ? Num(k.Substring(d + 1), 8) : 8;
            }
            return p;
        }

        public static void Build(CircuitDef def)
        {
            Pieces.Clear();
            Vector3 pos = Vector3.zero; float yaw = 0;
            foreach (var k in def.layout)
            {
                var p = Parse(k); p.P = pos; p.yaw = yaw; Pieces.Add(p);
                Local(p, 1, 0, out float lx, out float lz, out _);
                pos += Right(yaw) * lx + Fwd(yaw) * lz;
                if (!p.straight) yaw += -p.s * p.A;
            }
            SurfIds = def.surf ?? new string[0];
            GripMul = def.grip;
        }

        public static Vector3 Right(float yaw) => new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));
        public static Vector3 Fwd(float yaw) => new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw));

        static void Local(Piece p, float u, float o, out float lx, out float lz, out float dh)
        {
            if (p.straight) { lx = o; lz = p.L * u; dh = 0; return; }
            float a = p.A * u, s = p.s, c = Mathf.Cos(a), sn = Mathf.Sin(a);
            lx = -s * p.R + s * p.R * c + o * c;
            lz = p.R * sn + o * s * sn;
            dh = -s * a;
        }

        static float Ease(float u) => (1 - Mathf.Cos(Mathf.PI * u)) / 2;
        public static float RollOf(Piece p, float u) => p.twist == 0 ? 0 : p.twist == 'i' ? Mathf.PI * Ease(u) : p.twist == 'r' ? Mathf.PI : Mathf.PI * (1 + Ease(u));

        public static Pose World(int i, float u, float o)
        {
            var p = Pieces[i];
            Local(p, u, o, out float lx, out float lz, out float dh);
            float y = 0, roll = 0;
            if (p.twist != 0)
            {
                roll = RollOf(p, u); float cr = Mathf.Cos(roll), sr = Mathf.Sin(roll);
                y = TwistR + lx * sr - TwistR * cr; lx = lx * cr + TwistR * sr;
            }
            return new Pose { pos = p.P + Right(p.yaw) * lx + Fwd(p.yaw) * lz + Vector3.up * y, h = p.yaw + dh, roll = roll };
        }

        public static Quaternion Rot(Pose w) => Quaternion.Euler(0, w.h * Mathf.Rad2Deg, 0) * Quaternion.Euler(0, 0, w.roll * Mathf.Rad2Deg);
        public static float PieceLen(int i, float o) { var p = Pieces[i]; return p.straight ? p.L : p.A * Mathf.Max(1, p.R + p.s * o); }
        public static float Radius(int i, float o) { var p = Pieces[i]; return p.straight ? float.PositiveInfinity : p.R + p.s * o; }
        public static string SurfId(int i) => i < SurfIds.Length ? SurfIds[i] : "asfalto";
        public static SurfDef Surf(int i) => CozyData.Surfs.TryGetValue(SurfId(i), out var s) ? s : CozyData.Surfs["asfalto"];

        public static Bounds GetBounds()
        {
            var b = new Bounds(World(0, 0, 0).pos, Vector3.zero);
            for (int i = 0; i < N; i++) foreach (var u in new[] { 0f, 0.5f }) foreach (var o in new[] { -Half, Half }) b.Encapsulate(World(i, u, o).pos);
            return b;
        }
    }
}
