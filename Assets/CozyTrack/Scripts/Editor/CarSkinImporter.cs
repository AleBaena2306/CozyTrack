using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CozyTrack.Cars.Editor
{
    /// <summary>
    /// Crea o actualiza los assets CarSkin (skins.json) y WheelSkin (wheel_skins.json)
    /// y prepara la importación de las texturas de skins.
    /// Menú: Cozy Track > Skins
    /// </summary>
    public static class CarSkinImporter
    {
        const string JsonPath = "Assets/CozyTrack/Art/skins.json";
        const string TexturesFolder = "Assets/CozyTrack/Art/Textures/Skins";
        const string OutputFolder = "Assets/CozyTrack/Data/Skins";
        const string WheelJsonPath = "Assets/CozyTrack/Art/wheel_skins.json";
        const string WheelModelsFolder = "Assets/CozyTrack/Art/Models/Wheels";
        const string WheelOutputFolder = "Assets/CozyTrack/Data/WheelSkins";

        [Serializable] class SkinRow { public string id, name, pattern, paint, trim; public bool factory; }
        [Serializable] class SkinList { public SkinRow[] skins; }
        [Serializable] class WheelRow { public string id, name, mesh, tyre, rim, cap; }
        [Serializable] class WheelList { public WheelRow[] wheelSkins; }

        [MenuItem("Cozy Track/Skins/Crear todas las skins")]
        public static void CreateAll()
        {
            CreateSkins();
            CreateWheelSkins();
        }

        [MenuItem("Cozy Track/Skins/Crear skins de ruedas desde wheel_skins.json")]
        public static void CreateWheelSkins()
        {
            if (!File.Exists(WheelJsonPath))
            {
                EditorUtility.DisplayDialog("Cozy Track", $"No se encuentra {WheelJsonPath}.", "Vale");
                return;
            }
            var list = JsonUtility.FromJson<WheelList>(File.ReadAllText(WheelJsonPath));
            Directory.CreateDirectory(WheelOutputFolder);
            int count = 0;
            foreach (var row in list.wheelSkins)
            {
                string path = $"{WheelOutputFolder}/WheelSkin_{row.id}.asset";
                var skin = AssetDatabase.LoadAssetAtPath<WheelSkin>(path);
                if (skin == null)
                {
                    skin = ScriptableObject.CreateInstance<WheelSkin>();
                    AssetDatabase.CreateAsset(skin, path);
                }
                skin.skinId = row.id;
                skin.displayName = row.name;
                skin.mesh = AssetDatabase.LoadAllAssetsAtPath($"{WheelModelsFolder}/{row.mesh}.fbx").OfType<Mesh>().FirstOrDefault();
                if (skin.mesh == null)
                    Debug.LogWarning($"[Cozy Track] No se encuentra la malla {row.mesh} en {WheelModelsFolder}");
                skin.tyreColor = Parse(row.tyre, skin.tyreColor);
                skin.rimColor = Parse(row.rim, skin.rimColor);
                skin.capColor = Parse(row.cap, skin.capColor);
                EditorUtility.SetDirty(skin);
                count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Cozy Track] {count} skins de ruedas creadas o actualizadas en {WheelOutputFolder}");
        }

        [MenuItem("Cozy Track/Skins/Crear skins desde skins.json")]
        public static void CreateSkins()
        {
            if (!File.Exists(JsonPath))
            {
                EditorUtility.DisplayDialog("Cozy Track", $"No se encuentra {JsonPath}.", "Vale");
                return;
            }
            FixTextureSettings();
            var list = JsonUtility.FromJson<SkinList>(File.ReadAllText(JsonPath));
            Directory.CreateDirectory(OutputFolder);
            int count = 0;
            foreach (var row in list.skins)
            {
                string path = $"{OutputFolder}/Skin_{row.id}.asset";
                var skin = AssetDatabase.LoadAssetAtPath<CarSkin>(path);
                if (skin == null)
                {
                    skin = ScriptableObject.CreateInstance<CarSkin>();
                    AssetDatabase.CreateAsset(skin, path);
                }
                skin.skinId = row.id;
                skin.displayName = row.name;
                skin.factory = row.factory;
                skin.pattern = string.IsNullOrEmpty(row.pattern)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/{row.pattern}.png");
                skin.paintColor = Parse(row.paint, Color.white);
                skin.trimColor = Parse(row.trim, Color.white);
                EditorUtility.SetDirty(skin);
                count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Cozy Track] {count} skins creadas o actualizadas en {OutputFolder}");
        }

        /// <summary>Repeat + mipmaps + sRGB para que los patrones se repitan bien y no parpadeen de lejos.</summary>
        [MenuItem("Cozy Track/Skins/Ajustar importación de texturas de skins")]
        public static void FixTextureSettings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(p) is not TextureImporter ti) continue;
                bool changed = ti.wrapMode != TextureWrapMode.Repeat || !ti.mipmapEnabled || !ti.sRGBTexture
                               || ti.maxTextureSize != 512 || ti.filterMode != FilterMode.Trilinear;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.mipmapEnabled = true;
                ti.sRGBTexture = true;
                ti.filterMode = FilterMode.Trilinear;
                ti.anisoLevel = 2;
                ti.maxTextureSize = 512;
                if (changed) ti.SaveAndReimport();
            }
        }

        static Color Parse(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }
    }
}
