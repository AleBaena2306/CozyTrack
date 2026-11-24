# Cozy Track · progresión (generado por tools/progression.py)

| Nivel | Mundo | Nombre | Carriles | Agarre pista | Vuelta (m) | Rectas | Curva más cerrada (m) | Exigencia | Efecto de las mejoras | Rival (mejoras v/a/p, pericia) | Tiempo rival (s) | Mejora prevista | Mejora necesaria por coche | Coche al máximo con 2 salidas (s respecto al rival; >0 pierde) | Aviso |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Sala de juegos | Primera vuelta | 4 | 1.0 | 90 | 44% | 7.4 | 1.44 | 1.3% | Rhino 0/0/0 k=0.866 | 32.0 | 0 | Dart 0, Hopper 0, Gecko 0, Rhino 0 | Dart 1.2, Hopper 1.3, Gecko 0.4, Rhino 1.6 | poco sensible a mejoras: alargar rectas |
| 5 | Sala de juegos | Zigzag | 4 | 1.0 | 123 | 39% | 7.4 | 1.44 | 1.9% | Gecko 1/1/1 k=0.888 | 40.5 | 1 | Dart 1, Hopper 2, Gecko 0, Rhino None | Dart 2.4, Hopper 2.6, Gecko 1.6, Rhino 3.0 | poco sensible a mejoras: alargar rectas |
| 10 | Sala de juegos | La alfombra | 4 | 1.0 | 149 | 32% | 7.4 | 1.44 | 1.5% | Dart 2/2/2 k=0.914 | 48.9 | 2 | Dart 3, Hopper 5, Gecko 0, Rhino None | Dart 2.6, Hopper 3.0, Gecko 1.6, Rhino 3.3 | poco sensible a mejoras: alargar rectas |
| 15 | Jardín | Entre macetas | 4 | 1.0 | 144 | 50% | 4.4 | 1.87 | 1.9% | Rhino 3/3/3 k=0.925 | 52.0 | 2 | Dart 3, Hopper 3, Gecko 0, Rhino None | Dart 2.6, Hopper 2.8, Gecko 1.4, Rhino 3.4 | poco sensible a mejoras: alargar rectas |
| 20 | Jardín | El seto | 4 | 1.0 | 152 | 42% | 4.4 | 1.87 | 1.8% | Hopper 3/3/3 k=0.915 | 56.2 | 3 | Dart 4, Hopper 3, Gecko 0, Rhino None | Dart 2.7, Hopper 2.9, Gecko 1.4, Rhino 3.4 | poco sensible a mejoras: alargar rectas |
| 25 | Nieve | Horquillas | 2 | 0.85 | 115 | 70% | 2.4 | 2.75 | 2.0% | Rhino 4/4/3 k=0.887 | 47.9 | 4 | Dart 5, Hopper 2, Gecko 0, Rhino None | Dart 3.0, Hopper 2.5, Gecko 1.9, Rhino 3.8 | poco sensible a mejoras: alargar rectas |
| 30 | Nieve | La gran helada | 2 | 0.85 | 154 | 57% | 2.4 | 2.75 | 2.3% | Gecko 4/4/3 k=0.89 | 60.0 | 5 | Dart None, Hopper 5, Gecko 1, Rhino None | Dart 3.1, Hopper 2.9, Gecko 2.1, Rhino 4.1 | poco sensible a mejoras: alargar rectas |

Exigencia = velocidad punta del Dart / velocidad segura en la curva más cerrada (carril central).
Mejora necesaria = nivel uniforme (velocidad, aceleración y peso hasta 3) con el que un piloto «bueno» (entra al 92 % del límite, reacciona en 0,3 s) gana al rival por 1 s o más.
Efecto de las mejoras = cuánto más rápido es el coche al máximo que de serie en ese circuito. La pericia del rival se calcula para que la mejora necesaria sea la prevista.

Coste acumulado de una mejora por niveles: [60, 170, 350, 630, 1050] monedas (×3 mejoras por coche).
