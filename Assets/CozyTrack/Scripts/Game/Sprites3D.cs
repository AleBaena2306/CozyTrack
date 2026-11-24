using System.Collections.Generic;
using UnityEngine;

namespace CozyTrack
{
    // Partículas sencillas en una sola malla (siempre de cara a la cámara): humo, polvo, chispas, aire.
    public class Sprites3D
    {
        public class P { public Vector3 pos, vel; public Color col; public float size, life, t, grav; public bool grow; }
        public readonly List<P> list = new List<P>();
        readonly int cap; readonly Mesh mesh; readonly Vector3[] vtx; readonly Color[] cols; readonly GameObject go;
        public Material mat;

        public Sprites3D(string name, int capacity, Material m)
        {
            cap = capacity; mat = m;
            mesh = new Mesh { name = name }; mesh.MarkDynamic();
            vtx = new Vector3[cap * 4]; cols = new Color[cap * 4];
            var uv = new Vector2[cap * 4]; var idx = new int[cap * 6];
            for (int k = 0; k < cap; k++)
            {
                uv[k * 4] = new Vector2(0, 0); uv[k * 4 + 1] = new Vector2(1, 0); uv[k * 4 + 2] = new Vector2(1, 1); uv[k * 4 + 3] = new Vector2(0, 1);
                idx[k * 6] = k * 4; idx[k * 6 + 1] = k * 4 + 2; idx[k * 6 + 2] = k * 4 + 1; idx[k * 6 + 3] = k * 4; idx[k * 6 + 4] = k * 4 + 3; idx[k * 6 + 5] = k * 4 + 2;
            }
            mesh.vertices = vtx; mesh.uv = uv; mesh.colors = cols; mesh.triangles = idx;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000);
            go = new GameObject(name); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }

        public void Add(Vector3 pos, Color col, float size, float life, Vector3 vel, float grav, bool grow = false)
        {
            if (list.Count >= cap) return;
            list.Add(new P { pos = pos, col = col, size = size, life = life, t = life, vel = vel, grav = grav, grow = grow });
        }

        public void Tick(float dt, Camera cam)
        {
            for (int k = list.Count - 1; k >= 0; k--)
            {
                var p = list[k]; p.t -= dt; p.vel.y -= p.grav * dt; p.pos += p.vel * dt;
                if (p.grav > 0 && p.pos.y < 0.03f) { p.pos.y = 0.03f; p.vel = Vector3.zero; }
                if (p.t <= 0) list.RemoveAt(k);
            }
            Render(cam, true);
        }

        public void Render(Camera cam, bool fade)
        {
            Vector3 r = cam.transform.right, up = cam.transform.up;
            for (int k = 0; k < cap; k++)
            {
                if (k < list.Count)
                {
                    var p = list[k]; float f = 1 - p.t / p.life, s = p.size * (p.grow ? 1 + f * 2 : 1) * 0.5f;
                    var c = p.col; if (fade) c.a *= Mathf.Clamp01(p.t / p.life);
                    vtx[k * 4] = p.pos + (-r - up) * s; vtx[k * 4 + 1] = p.pos + (r - up) * s; vtx[k * 4 + 2] = p.pos + (r + up) * s; vtx[k * 4 + 3] = p.pos + (-r + up) * s;
                    cols[k * 4] = cols[k * 4 + 1] = cols[k * 4 + 2] = cols[k * 4 + 3] = c;
                }
                else { vtx[k * 4] = vtx[k * 4 + 1] = vtx[k * 4 + 2] = vtx[k * 4 + 3] = Vector3.zero; }
            }
            mesh.vertices = vtx; mesh.colors = cols;
        }
        public void Clear() => list.Clear();
    }
}
