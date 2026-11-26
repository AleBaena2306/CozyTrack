using UnityEditor;

namespace CozyTrack.EditorTools
{
    // Todas las imágenes de interfaz (Resources/UI) se importan como sprites nítidos: sin mipmaps, sin compresión,
    // con transparencia limpia en los bordes. Con los ajustes por defecto salían bordes dentados y halos alrededor
    // de iconos, cofres y botones.
    public class CozyUiTextureImport : AssetPostprocessor
    {
        public override uint GetVersion() => 1;   // al cambiarlo, Unity vuelve a importar estas imágenes

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/CozyTrack/Resources/UI/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = UnityEngine.FilterMode.Bilinear;
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 1024;
        }
    }
}
