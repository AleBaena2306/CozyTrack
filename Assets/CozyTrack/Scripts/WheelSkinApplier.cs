using System.Collections.Generic;
using UnityEngine;

namespace CozyTrack.Cars
{
    /// <summary>
    /// Aplica una WheelSkin a las cuatro ruedas de un coche y las hace girar.
    /// Va en el objeto raíz del coche (Car_*), junto a CarSkinApplier si se quiere.
    ///
    /// Estructura que espera (la de los FBX de Cozy Track):
    ///   Car_X > WheelSocket_FL (escalado a ancho, radio, radio) > Wheel_FL (MeshFilter + MeshRenderer)
    /// Todas las mallas de rueda tienen las ranuras CT_WheelTyre, CT_WheelRim y CT_WheelCap en el mismo orden,
    /// por eso se puede cambiar la malla sin tocar los materiales.
    /// </summary>
    [DisallowMultipleComponent]
    public class WheelSkinApplier : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] WheelSkin startSkin;

        [Tooltip("Invierte el sentido de giro si las ruedas giran hacia atrás tras importar.")]
        [SerializeField] bool invertSpin;

        class Wheel
        {
            public Transform transform;
            public MeshFilter filter;
            public MeshRenderer renderer;
            public Mesh originalMesh;
            public float radius;          // en metros, tomado de la escala del WheelSocket
            public int tyre = -1, rim = -1, cap = -1;
        }

        readonly List<Wheel> wheels = new();
        MaterialPropertyBlock block;

        public WheelSkin Current { get; private set; }

        void Awake()
        {
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.name.StartsWith("Wheel_")) continue;
                var w = new Wheel
                {
                    transform = mf.transform,
                    filter = mf,
                    renderer = mf.GetComponent<MeshRenderer>(),
                    originalMesh = mf.sharedMesh,
                    radius = Mathf.Max(0.01f, mf.transform.parent != null ? mf.transform.parent.localScale.y : 0.15f),
                };
                var mats = w.renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name;
                    if (n.StartsWith("CT_WheelTyre")) w.tyre = i;
                    else if (n.StartsWith("CT_WheelRim")) w.rim = i;
                    else if (n.StartsWith("CT_WheelCap")) w.cap = i;
                }
                wheels.Add(w);
            }
            if (wheels.Count == 0)
                Debug.LogWarning($"[WheelSkinApplier] {name}: no se encontraron objetos Wheel_*.", this);
            if (startSkin != null) Apply(startSkin);
        }

        public void Apply(WheelSkin skin)
        {
            if (skin == null) { ResetToFactory(); return; }
            block ??= new MaterialPropertyBlock();
            foreach (var w in wheels)
            {
                if (skin.mesh != null) w.filter.sharedMesh = skin.mesh;
                SetColor(w, w.tyre, skin.tyreColor);
                SetColor(w, w.rim, skin.rimColor);
                SetColor(w, w.cap, skin.capColor);
            }
            Current = skin;
        }

        /// <summary>Diseño y colores de fábrica del coche.</summary>
        public void ResetToFactory()
        {
            block ??= new MaterialPropertyBlock();
            block.Clear();
            foreach (var w in wheels)
            {
                w.filter.sharedMesh = w.originalMesh;
                for (int i = 0; i < w.renderer.sharedMaterials.Length; i++) w.renderer.SetPropertyBlock(block, i);
            }
            Current = null;
        }

        /// <summary>Gira las ruedas según la distancia recorrida este fotograma (en metros).</summary>
        public void Spin(float distance)
        {
            float sign = invertSpin ? -1f : 1f;
            foreach (var w in wheels)
            {
                float degrees = distance / w.radius * Mathf.Rad2Deg * sign;
                w.transform.Rotate(Vector3.right, degrees, Space.Self);
            }
        }

        void SetColor(Wheel w, int slot, Color c)
        {
            if (slot < 0) return;
            block.Clear();
            block.SetColor(BaseColorId, c);
            w.renderer.SetPropertyBlock(block, slot);
        }
    }
}
