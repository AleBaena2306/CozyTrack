using UnityEngine;

namespace CozyTrack
{
    // Estado de un coche en carrera (física v4 del prototipo, tools/physics_sim.py).
    public class RaceCar
    {
        public const float G = 9.8f, CrrRatio = 30, Magnet = 2, SlipGrow = 30, SlipRecover = 3, SlipScrub = 1.5f, Restitution = 0.3f, BaseBrake = 12, CornerQ0 = 0.85f, CornerQEq = 0.97f;

        public string name; public bool isPlayer;
        public GameObject g; public Transform[] wheels = new Transform[0]; public Vector3[] wheelAxis = new Vector3[0]; public float[] wheelR = new float[0];
        public float m, F, Cd, Crr, mu, vtop, alat, brake;

        public int i, wraps, lane, prevLane = -1; public float u = 0.33f, o;
        public float v, slip, q, turbo, shield, spin, crash, cool, k = 1, kStraight = 0.92f, react = 0.25f, aiT;
        public float superT, autoT, blackT, bubbleT, magnetT, slipPeak, pitch, acc, obCool, entryQ;
        public int coins, uses, outAt, outLap; public float done = -1;   // done < 0: no ha terminado
        public string item; public bool draft, gas, cleanCurve, mustBrake, isOut;
        public Vector3 flyPos, flyVel; public float flyRot;
        public GameObject shieldFx, mark; public bool fxOn;
        public Renderer[] renderers; public Material[][] origMats;
        public bool remote; public float netP, netO, netV, netAt, shownP;   // rival online: lo mueve el servidor

        public void SetPhysics(int[] st, int upSpeed, int upAccel, int upBrake)
        {
            m = 0.8f + 0.15f * st[3];
            vtop = (11 + 0.5f * st[0]) * (1 + CozyData.UpSum(0, upSpeed));
            float a0 = (7 + 1.0f * st[1]) * (1 + CozyData.UpSum(1, upAccel));
            F = m * a0;
            Cd = F / (vtop * vtop + CrrRatio * vtop); Crr = CrrRatio * Cd;
            mu = 0.9f + 0.04f * st[2];
            alat = mu * (G + Magnet / m);
            brake = BaseBrake * (1 + CozyData.UpSum(2, upBrake));
        }
        public float Prog(int n) => wraps * n + i + u;
    }
}
