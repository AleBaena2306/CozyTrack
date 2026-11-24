# Cozy Track

Carreras de juguetes tipo Scalextric en 3D, vertical, para móvil (Kovu's Pride).

## Estructura

- `Assets/CozyTrack/Art/FBX`: piezas de pista, coches, 36 vehículos, ruedas, objetos y decorado (generados por script en Blender).
- `Assets/CozyTrack/Art/Textures`: patrones de skins.
- `Assets/CozyTrack/Scripts`: skins de carrocería y ruedas (`CarSkin`, `WheelSkin`, menú *Cozy Track > Skins*).
- `Assets/CozyTrack/Data`: datos de piezas, coches, vehículos, skins y progresión (JSON).
- `Tools/Blender`: scripts que generan los modelos y los .blend originales (fuera de `Assets` para que Unity no los importe).
- `Tools/Web`: prototipo jugable y herramienta de balanceo.
- `Docs/Previews`: renders de referencia.

## Unir el proyecto local con este repositorio (una sola vez)

Con Unity cerrado, en la carpeta del proyecto:

```
git init
git remote add origin https://github.com/AleBaena2306/CozyTrack.git
git pull origin main
git add .
git commit -m "Proyecto de Unity"
git push -u origin main
```
