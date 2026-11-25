using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CozyTrack.EditorTools
{
    // COZY TRACK > Configurar proyecto: biblioteca de modelos y shaders, efecto de bordes (estilo «Cómic»),
    // escena del juego en Build Settings y Player Settings del estudio. Se ejecuta sola la primera vez.
    [InitializeOnLoad]
    public static class CozyTrackSetup
    {
        const string LibPath = "Assets/CozyTrack/Resources/CozyLibrary.asset";
        const string ScenePath = "Assets/CozyTrack/Scenes/CozyTrack.unity";
        const string EdgeMatPath = "Assets/CozyTrack/Resources/CozyEdges.mat";

        static CozyTrackSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var lib = AssetDatabase.LoadAssetAtPath<CozyLibrary>(LibPath);
                if (lib == null || lib.models.Length == 0 || lib.skinPatterns == null || lib.skinPatterns.Length == 0 || !File.Exists(ScenePath)) Run(false);
            };
        }

        [MenuItem("COZY TRACK/Configurar proyecto", priority = 1)]
        public static void RunMenu() => Run(true);

        [MenuItem("COZY TRACK/Abrir escena del juego", priority = 2)]
        public static void OpenScene() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }

        public static void Run(bool verbose)
        {
            Directory.CreateDirectory("Assets/CozyTrack/Resources"); Directory.CreateDirectory("Assets/CozyTrack/Scenes");
            AssetDatabase.Refresh();

            // 1. biblioteca
            var lib = AssetDatabase.LoadAssetAtPath<CozyLibrary>(LibPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<CozyLibrary>(); AssetDatabase.CreateAsset(lib, LibPath); }
            var models = new List<GameObject>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/CozyTrack/Art/FBX" }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go) models.Add(go);
            }
            lib.models = models.OrderBy(m => m.name).ToArray();
            lib.skinPatterns = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/CozyTrack/Art/Textures/Skins" })
                .Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g))).Where(t => t).OrderBy(t => t.name).ToArray();
            lib.toon = Shader.Find("CozyTrack/Toon"); lib.toonTransparent = Shader.Find("CozyTrack/Toon Transparent"); lib.unlit = Shader.Find("CozyTrack/Unlit");

            // 2. efecto de bordes en cada renderer de URP del proyecto
            var edgeShader = Shader.Find("CozyTrack/Edges");
            var edgeMat = AssetDatabase.LoadAssetAtPath<Material>(EdgeMatPath);
            if (edgeMat == null && edgeShader) { edgeMat = new Material(edgeShader); AssetDatabase.CreateAsset(edgeMat, EdgeMatPath); }
            var feats = new List<ScriptableRendererFeature>();
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null) continue;
                var f = data.rendererFeatures.FirstOrDefault(x => x != null && x.name == "CozyEdges") as FullScreenPassRendererFeature;
                if (f == null)
                {
                    f = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                    f.name = "CozyEdges";
                    f.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
                    f.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
                    f.passMaterial = edgeMat;
                    f.fetchColorBuffer = true;
                    AssetDatabase.AddObjectToAsset(f, data);
                    data.rendererFeatures.Add(f);
                    var so = new SerializedObject(data);
                    var map = so.FindProperty("m_RendererFeatureMap");
                    if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out string _, out long localId))
                    {
                        map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId; so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    f.SetActive(false);
                    EditorUtility.SetDirty(data);
                }
                else if (f.passMaterial == null) { f.passMaterial = edgeMat; EditorUtility.SetDirty(f); }
                feats.Add(f);
            }
            lib.edgeFeatures = feats.ToArray();
            EditorUtility.SetDirty(lib);

            // 3. escena del juego
            if (!File.Exists(ScenePath))
            {
                var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var go = new GameObject("CozyGame"); go.AddComponent<CozyGame>();
                SceneManager.MoveGameObjectToScene(go, sc);
                EditorSceneManager.SaveScene(sc, ScenePath);
                EditorSceneManager.CloseScene(sc, true);
            }
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).Select(s => { s.enabled = false; return s; }).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            // 4. Player Settings del estudio (solo si siguen los valores por defecto)
            if (PlayerSettings.productName != "Cozy Track")
            {
                PlayerSettings.companyName = "Kovu's Pride";
                PlayerSettings.productName = "Cozy Track";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.KovusPride.CozyTrack");
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.KovusPride.CozyTrack");
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
                if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1") PlayerSettings.bundleVersion = "0.1.0";
            }

            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.GetActiveScene().isDirty && EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            Debug.Log($"[CozyTrack] Proyecto configurado: {lib.models.Length} modelos, {feats.Count} renderers con el efecto de bordes. Pulsa Play en {ScenePath}.");
            if (verbose) EditorUtility.DisplayDialog("Cozy Track", $"Listo.\n\n· {lib.models.Length} modelos en la biblioteca\n· Efecto de bordes en {feats.Count} renderers de URP\n· Escena {ScenePath} en Build Settings\n\nPulsa Play.", "OK");
        }
    }
}
