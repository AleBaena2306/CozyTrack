using UnityEngine;

namespace CozyTrack.Cars
{
    /// <summary>
    /// Una skin de coche. Solo cambia dos ranuras de material del chasis:
    /// la pintura (CT_Paint_*) y los detalles (CT_Trim_*). Cristales, ruedas, faros y números no cambian.
    /// </summary>
    [CreateAssetMenu(menuName = "Cozy Track/Car Skin", fileName = "Skin_")]
    public class CarSkin : ScriptableObject
    {
        public string skinId;
        public string displayName;

        [Tooltip("Si está marcado, la skin devuelve el coche a sus colores de fábrica y se ignora el resto.")]
        public bool factory;

        [Header("Pintura")]
        [Tooltip("Patrón opcional. Importar con Wrap Mode = Repeat, mipmaps activados y sRGB.")]
        public Texture2D pattern;

        [Tooltip("Color de la pintura. Con patrón se multiplica por él: en blanco, el patrón se ve tal cual.")]
        public Color paintColor = Color.white;

        [Tooltip("Repeticiones del patrón. 1 = una repetición cada 0,6 m, igual en los cuatro coches.")]
        [Min(0.1f)] public float patternScale = 1f;

        [Header("Detalles")]
        [Tooltip("Color de franjas, alerón, arco o defensa.")]
        public Color trimColor = Color.white;

        [Header("Tienda")]
        public int price;
        public Sprite icon;
    }
}
