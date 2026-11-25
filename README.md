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

## Bots de prueba (GitHub Actions)

`.github/workflows/bots.yml` compila el juego real para Linux en los servidores de GitHub y lo lanza sin pantalla con
`-cozybots` (código en `Assets/CozyTrack/Scripts/Game/CozyGame.Bots.cs`). Tres perfiles de jugador simulado (novato,
casual y experto) juegan los 30 niveles desde cero con cofres, monedas y mejoras, más contrarrelojes de todos los
vehículos y partidas de Supervivencia. El informe (alertas, progresión, game feel, economía, equilibrio de coches y
errores) aparece en la pestaña **Summary** de la ejecución, como artefacto «informe-bots» y en la rama `bot-reports`.

- Se lanza solo al subir cambios en `Assets/CozyTrack/Scripts` o `Assets/CozyTrack/Data`, o a mano en
  **Actions > Bots de prueba > Run workflow** (jugadores por perfil, horas y semilla).
- Necesita los Secrets `UNITY_LICENSE`, `UNITY_EMAIL` y `UNITY_PASSWORD`.
- No usa el PC: en el juego normal los bots no existen salvo con el argumento `-cozybots`.
