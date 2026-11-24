using UnityEngine;

namespace CozyTrack.Cars
{
    /// <summary>
    /// Skin de ruedas, independiente de la de carrocería: cualquier skin de rueda vale para cualquier coche.
    /// Diseño = malla de rueda de tamaño unidad (Wheel_Classic, Wheel_Spokes...), que cada coche escala
    /// con su WheelSocket. Colores = neumático, llanta y tapacubos.
    /// </summary>
    [CreateAssetMenu(menuName = "Cozy Track/Wheel Skin", fileName = "WheelSkin_")]
    public class WheelSkin : ScriptableObject
    {
        public string skinId;
        public string displayName;

        [Tooltip("Malla del diseño (de FBX/Wheels). Si se deja vacía se mantiene el diseño actual.")]
        public Mesh mesh;

        public Color tyreColor = new(0.17f, 0.17f, 0.21f);
        public Color rimColor = Color.white;
        public Color capColor = new(0.17f, 0.17f, 0.21f);

        [Header("Tienda")]
        public int price;
        public Sprite icon;
    }
}
