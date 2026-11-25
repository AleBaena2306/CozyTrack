using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CozyTrack
{
    // Referencias que necesita el juego en tiempo de ejecución. La rellena COZY TRACK > Configurar proyecto
    // (Assets/CozyTrack/Resources/CozyLibrary.asset): modelos de los FBX, shaders y el efecto de bordes.
    public class CozyLibrary : ScriptableObject
    {
        public GameObject[] models = new GameObject[0];
        public Shader toon, toonTransparent, unlit;
        public Texture2D[] skinPatterns = new Texture2D[0];   // estampados de pintura (Art/Textures/Skins)
        public ScriptableRendererFeature[] edgeFeatures = new ScriptableRendererFeature[0];

        Dictionary<string, GameObject> map;
        public GameObject Get(string n)
        {
            if (map == null) { map = new Dictionary<string, GameObject>(); foreach (var m in models) if (m) map[m.name] = m; }
            return map.TryGetValue(n, out var g) ? g : null;
        }

        static CozyLibrary inst;
        public static CozyLibrary I => inst ? inst : (inst = Resources.Load<CozyLibrary>("CozyLibrary"));
    }
}
