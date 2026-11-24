using System.Collections.Generic;
using UnityEngine;

namespace CozyTrack.Cars
{
    /// <summary>
    /// Aplica una CarSkin al chasis de un coche sin crear materiales nuevos (MaterialPropertyBlock por ranura).
    /// Busca las ranuras por el nombre del material importado: CT_Paint_* (pintura) y CT_Trim_* (detalles).
    /// Pensado para URP/Lit (_BaseMap, _BaseColor). Con pocos coches en pantalla no importa
    /// que los property blocks saquen al renderer del SRP Batcher.
    /// </summary>
    [DisallowMultipleComponent]
    public class CarSkinApplier : MonoBehaviour
    {
        const string PaintPrefix = "CT_Paint";
        const string TrimPrefix = "CT_Trim";

        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("MeshRenderer del chasis (Car_Dart, Car_Hopper...). Si se deja vacío se busca en este objeto.")]
        [SerializeField] Renderer bodyRenderer;

        [SerializeField] CarSkin startSkin;

        readonly List<int> paintSlots = new();
        readonly List<int> trimSlots = new();
        MaterialPropertyBlock block;

        public CarSkin Current { get; private set; }

        void Awake()
        {
            if (bodyRenderer == null) bodyRenderer = GetComponent<Renderer>();
            CacheSlots();
            if (startSkin != null) Apply(startSkin);
        }

        void CacheSlots()
        {
            paintSlots.Clear();
            trimSlots.Clear();
            if (bodyRenderer == null) return;
            var mats = bodyRenderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string n = mats[i].name;
                if (n.StartsWith(PaintPrefix)) paintSlots.Add(i);
                else if (n.StartsWith(TrimPrefix)) trimSlots.Add(i);
            }
            if (paintSlots.Count == 0)
                Debug.LogWarning($"[CarSkinApplier] {name}: no hay ninguna ranura {PaintPrefix}_*. ¿Se extrajeron los materiales del FBX?", this);
        }

        public void Apply(CarSkin skin)
        {
            if (bodyRenderer == null) return;
            if (skin == null || skin.factory)
            {
                Clear();
                Current = skin;
                return;
            }
            block ??= new MaterialPropertyBlock();

            foreach (int i in paintSlots)
            {
                block.Clear();
                block.SetColor(BaseColorId, skin.paintColor);
                if (skin.pattern != null)
                {
                    block.SetTexture(BaseMapId, skin.pattern);
                    block.SetVector(BaseMapStId, new Vector4(skin.patternScale, skin.patternScale, 0f, 0f));
                }
                else
                {
                    block.SetTexture(BaseMapId, Texture2D.whiteTexture);
                }
                bodyRenderer.SetPropertyBlock(block, i);
            }
            foreach (int i in trimSlots)
            {
                block.Clear();
                block.SetColor(BaseColorId, skin.trimColor);
                bodyRenderer.SetPropertyBlock(block, i);
            }
            Current = skin;
        }

        /// <summary>Vuelve a los colores de fábrica (los de los materiales importados).</summary>
        public void Clear()
        {
            if (bodyRenderer == null) return;
            block ??= new MaterialPropertyBlock();
            block.Clear();
            foreach (int i in paintSlots) bodyRenderer.SetPropertyBlock(block, i);
            foreach (int i in trimSlots) bodyRenderer.SetPropertyBlock(block, i);
        }
    }
}
