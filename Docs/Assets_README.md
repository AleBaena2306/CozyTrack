# Cozy Track · Assets 3D

Generados el 24/11/2026 con scripts de Blender 5.3 (carpeta `tools/`). Todo es geometría y patrones hechos por código: no hay arte de terceros ni imágenes generadas por IA. El prefijo de los materiales y archivos es `CT_` (Cozy Track).

## Contenido

| Carpeta | Qué hay |
|---|---|
| `FBX/Track` | 21 piezas de pista: rectas de 8 y 4 m, curvas de 45° y 90° (radio 8 m), curvas cerradas de 90° (radio 5 m), rampa de subida de 1,5 m y salida/meta con arco. Todas en versión de 2 y 4 carriles, más la bifurcación de 2 a 4 carriles (girada 180°, sirve de 4 a 2) |
| `FBX/Cars` | 4 coches: `Car_Dart` (velocidad), `Car_Hopper` (aceleración), `Car_Gecko` (agarre y aceleración) y `Car_Rhino` (peso y agarre). Cada rueda es un vacío `WheelSocket_XX` escalado a (ancho, radio, radio) con un hijo `Wheel_XX` (malla de rueda intercambiable). El pivote del chasis está a ras de pista. Stats en `cars.json` |
| `FBX/Wheels` | 7 diseños de rueda de tamaño unidad (radio 1, ancho 1): `Wheel_Classic`, `Wheel_Spokes`, `Wheel_Star`, `Wheel_Turbine`, `Wheel_Racing`, `Wheel_Knobby` y `Wheel_Whitewall` |
| `FBX/Items` | Caja de objeto, moneda, escudo, proyectil, imán y placa de turbo |
| `FBX/Scenery` | 18 decorados: sala de juegos, jardín, nieve, espacio y comunes (cono y bandera) |
| `Textures/Skins` | 12 patrones de 512×512 que se repiten sin costura |
| `Unity` | Scripts del sistema de skins, `skins.json` (8 skins de carrocería) y `wheel_skins.json` (8 skins de rueda) |
| `Blend` | Los .blend para retocar en Blender (pista, props, coches y circuito de muestra) |
| `Previews` | Vistas previas en PNG, incluidas las pruebas de skins (`skins_on_cars.png`, `wheel_styles.png` y `wheel_skins_on_cars.png`) |
| `tools` | Scripts que lo generan todo |

## Convenciones

- **Escala:** 1 unidad = 1 m. Carril de 1,2 m, bordillo de 0,3 m, coches de entre 1,1 y 1,5 m de largo.
- **Dirección:** todo avanza hacia −Y de Blender, que en Unity es **+Z**.
- **Hijos vacíos de cada pieza de pista (transforms en Unity):**
  - `Socket_Start` y `Socket_End`: para encadenar piezas. La siguiente pieza se coloca en la posición y rotación de `Socket_End` de la anterior.
  - `LaneK_NN`: puntos del centro de cada carril, uno por metro aproximadamente. El carril 0 es el de la izquierda en el sentido de la marcha. Sirven para construir las splines de los carriles.
- **Ruedas:** `WheelSocket_XX` da el tamaño y la posición; `Wheel_XX` tiene el pivote en el eje y gira en su X local. Como la escala del socket es igual en Y y Z, el giro no deforma la rueda.

## Skins

Una skin solo toca dos ranuras de material del chasis de cada coche:

- **`CT_Paint_<Coche>` (pintura):** color liso o patrón.
- **`CT_Trim_<Coche>` (detalles):** franjas, alerón, arco o defensa, siempre en color liso.

Cristales, ruedas, faros, pilotos y números no cambian, y así el coche se sigue reconociendo con cualquier skin.

Para que el mismo patrón tenga el mismo tamaño en los cuatro coches, los UV están a escala del mundo: una repetición de textura cada 0,6 m, por proyección de caja. En las uniones entre caras se nota algo el corte del patrón, cosa que encaja con el aspecto de juguete.

Qué funciona mejor, según la prueba:

- Patrones de dos colores con formas medianas (rayas, lunares, estrellas, rayos). Se leen bien incluso con la cámara del juego.
- Los cuadros son muy llamativos: conviene reservarlos para una skin especial (por ejemplo, la de campeón).
- Los patrones con mucho detalle pequeño (como el confeti) se convierten en ruido a distancia. No los he usado en coches.
- Las skins bicolores (pintura lisa con detalles de otro color) son las más baratas de producir y se ven muy limpias.

### Skins de rueda

Son independientes de las de carrocería: cualquier skin de rueda vale para cualquier coche y con cualquier carrocería. Cada una combina:

- **Diseño:** la malla de `FBX/Wheels`. Todas miden lo mismo (radio 1, ancho 1) y cada coche las escala con su `WheelSocket`.
- **Tres colores:** neumático (`CT_WheelTyre`), llanta (`CT_WheelRim`) y tapacubos o tornillos (`CT_WheelCap`).

Las 7 mallas tienen esas tres ranuras en el mismo orden, así que en Unity basta con cambiar la malla del `MeshFilter` y los colores: los materiales no se tocan.

Skins de rueda de prueba: Clásica (la de fábrica), Radios oro, Estrella, Turbina, Deportiva, Todoterreno, Banda blanca y Caramelo (neumático rosa).

### En Unity

1. Copie `Unity/Scripts` a `Assets/CozyTrack/Scripts/Cars`; `skins.json` y `wheel_skins.json` a `Assets/CozyTrack/Art/`; `Textures/Skins` a `Assets/CozyTrack/Art/Textures/Skins`, y `FBX/Wheels` a `Assets/CozyTrack/Art/Models/Wheels`.
2. Menú **Cozy Track > Skins > Crear todas las skins**:
   - Ajusta la importación de las texturas (Repeat, mipmaps, sRGB, 512).
   - Crea un asset `CarSkin` por skin de carrocería en `Assets/CozyTrack/Data/Skins`.
   - Crea un asset `WheelSkin` por skin de rueda en `Assets/CozyTrack/Data/WheelSkins`.
3. En el objeto raíz de cada prefab de coche (`Car_*`), añada `CarSkinApplier` y `WheelSkinApplier`. Al elegir en el garaje, llame a `Apply(carSkin)` y `Apply(wheelSkin)` por separado. Para guardar la elección basta con tres ids por coche: coche, skin de carrocería y skin de rueda.
4. `WheelSkinApplier.Spin(distancia)` gira las ruedas según los metros recorridos. Si giran al revés tras importar, marque `invertSpin`.
5. Al importar los FBX, extraiga los materiales y conviértalos a **URP/Lit**. El componente busca las ranuras por nombre (`CT_Paint*` y `CT_Trim*`), así que no cambie esos nombres.

`CarSkinApplier` usa un `MaterialPropertyBlock` por ranura: no duplica materiales ni texturas. Con 4 coches en pantalla no importa que esos renderers queden fuera del SRP Batcher.

## Importar los FBX en Unity (URP)

1. Copie `FBX` a `Assets/CozyTrack/Art/Models`.
2. En la pestaña **Model**: Scale Factor 1 y Convert Units activado. Si alguna pieza aparece girada 90°, active **Bake Axis Conversion**.
3. En la pestaña **Materials**: «Extract Materials» a `Assets/CozyTrack/Art/Materials` y páselos a URP/Lit (o al shader toon que elija). Los materiales `CT_<Color>` se comparten entre modelos.
4. El escudo usa `CT_Shield` y debe ser transparente (Surface Type = Transparent).

Encadenar piezas en C#:

```csharp
Transform next = Instantiate(piecePrefab, prevSocketEnd.position, prevSocketEnd.rotation).transform;
Transform socketEnd = next.Find("Socket_End");
```

## Presupuesto de polígonos

- Piezas de pista: entre 124 y 2.052 triángulos.
- Coches: entre 2.700 y 4.900 triángulos con las ruedas.
- Decorados: entre 20 y 2.188.
- Un circuito de 20 piezas con 4 coches y 15 decorados queda muy por debajo de 60.000 triángulos, razonable para Safari en iPhone.

## Regenerar o modificar

Las medidas y los colores están al principio de `tools/common.py`, `tools/gen_track.py` y `tools/gen_cars.py`. Hace falta Python 3.13 con `pip install bpy==5.3.0 pillow`.

```bash
python tools/gen_textures.py out
python tools/gen_track.py out
python tools/gen_props.py out
python tools/gen_cars.py out
python tools/gen_skins_test.py out   # prueba de skins (necesita las texturas)
python tools/gen_wheel_skins_test.py out   # mallas de rueda y prueba de skins de rueda
python tools/gen_showcase.py out     # circuito de muestra
```

Los .blend se pueden abrir y retocar a mano en Blender. Si regenera con los scripts, se sobrescriben.
