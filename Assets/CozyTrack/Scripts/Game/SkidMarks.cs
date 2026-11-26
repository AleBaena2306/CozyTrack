using UnityEngine;

namespace CozyTrack
{
    // Huellas de las ruedas sobre la pista: una sola malla con un anillo de tramos (los más viejos se reutilizan).
    // Cada tramo lleva su propio color y opacidad: suave al rodar, oscuro al derrapar o frenar, y teñido según la
    // superficie (barro, arena, hielo...).
    public class SkidMarks
    {
        const int Max = 1600;
        readonly Mesh mesh; readonly Vector3[] v = new Vector3[Max * 4]; readonly Color[] col = new Color[Max * 4];
        int next; bool dirty;
        public readonly GameObject go;

        public SkidMarks(Material m)
        {
            mesh = new Mesh { name = "Huellas" }; mesh.MarkDynamic();
            var uv = new Vector2[Max * 4]; var tri = new int[Max * 6];
            for (int k = 0; k < Max; k++)
            {
                uv[k * 4] = new Vector2(0, 0); uv[k * 4 + 1] = new Vector2(1, 0); uv[k * 4 + 2] = new Vector2(0, 1); uv[k * 4 + 3] = new Vector2(1, 1);
                tri[k * 6] = k * 4; tri[k * 6 + 1] = k * 4 + 2; tri[k * 6 + 2] = k * 4 + 1; tri[k * 6 + 3] = k * 4 + 1; tri[k * 6 + 4] = k * 4 + 2; tri[k * 6 + 5] = k * 4 + 3;
            }
            mesh.vertices = v; mesh.uv = uv; mesh.colors = col; mesh.triangles = tri;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000);
            go = new GameObject("Huellas"); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }

        // tramo entre el punto anterior y el actual de una rueda
        public void Add(Vector3 a, Vector3 b, Vector3 right, float width, Color ca, Color cb)
        {
            int k = next * 4; next = (next + 1) % Max;
            var h = right * (width * 0.5f);
            v[k] = a - h; v[k + 1] = a + h; v[k + 2] = b - h; v[k + 3] = b + h;
            col[k] = col[k + 1] = ca; col[k + 2] = col[k + 3] = cb;
            dirty = true;
        }

        public void Clear()
        {
            for (int k = 0; k < v.Length; k++) { v[k] = Vector3.zero; col[k] = Color.clear; }
            next = 0; dirty = true;
        }

        public void Flush()
        {
            if (!dirty) return;
            mesh.vertices = v; mesh.colors = col; dirty = false;
        }
    }
}
