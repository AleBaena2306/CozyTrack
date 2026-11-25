# Cozy Track · informe de los bots de prueba

3215 carreras simuladas en 3.1 min reales · 12 jugadores por perfil · semilla 1234 · versión 0.0.24

Los bots juegan el juego real (misma física, IA, niveles, monedas, cofres y mejoras). Solo cambia quién pulsa: cada perfil tiene su precisión en las curvas, su tiempo de reacción, sus despistes y su forma de gastar.

## Alertas

- **Muro:** 2 bot(s) «Novato (6-8 años)» se atascan en el nivel 8 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 1 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 10 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 6 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 9 (25 intentos sin superarlo).
- **Muro:** 2 bot(s) «Novato (6-8 años)» se atascan en el nivel 12 (25 intentos sin superarlo).
- **Muro:** 2 bot(s) «Novato (6-8 años)» se atascan en el nivel 5 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 7 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 3 (25 intentos sin superarlo).
- **Pico de dificultad:** el nivel 7 pide 4.5 intentos (mediana) a un jugador casual.
- **Pico de dificultad:** el nivel 12 pide 4.0 intentos (mediana) a un jugador casual.
- **Pico de dificultad:** el nivel 14 pide 4.0 intentos (mediana) a un jugador casual.
- **Poca emoción:** solo el 8 % de las carreras de «Experto» en Garden acaban ajustadas (<1 s).
- **Cofres desperdiciados:** «Casual» pierde el 35 % de los cofres por tener las 4 ranuras llenas.
- **Cofres desperdiciados:** «Experto» pierde el 54 % de los cofres por tener las 4 ranuras llenas.

## 1. Progresión (30 niveles)

| Perfil | Acaban el juego | Atascados | Horas medianas hasta el final | Carreras medianas | Monedas ganadas/h | Cofres/h | Anuncios/h |
|---|---|---|---|---|---|---|---|
| Novato (6-8 años) | 0/12 | 12 | — | 67.0 | 33 | 4.0 | 2.1 |
| Casual | 12/12 | 0 | 2.4 | 100.5 | 281 | 9.3 | 3.5 |
| Experto | 12/12 | 0 | 0.7 | 32.5 | 797 | 20.3 | 11.8 |

Intentos medianos para superar cada nivel y minutos de juego acumulados al superarlo (mediana). «Ganadas» = % de intentos en que gana al rival.

| Nivel | Mundo | Novato (6-8 años): intentos · min · gana | Casual: intentos · min · gana | Experto: intentos · min · gana |
|---|---|---|---|---|
| 1 | Playroom | 4.5 · 3 · 13 % | 1.0 · 0 · 60 % | 1.0 · 0 · 100 % |
| 2 | Playroom | 2.0 · 8 · 38 % | 1.5 · 3 · 33 % | 1.0 · 1 · 86 % |
| 3 | Playroom | 3.0 · 18 · 12 % | 1.0 · 8 · 36 % | 1.0 · 3 · 80 % |
| 4 | Playroom | 5.0 · 23 · 17 % | 2.0 · 12 · 43 % | 1.0 · 4 · 100 % |
| 5 | Playroom | 9.0 · 34 · 7 % | 2.0 · 16 · 44 % | 1.0 · 5 · 100 % |
| 6 | Playroom | 12.0 · 57 · 7 % | 1.5 · 18 · 32 % | 1.0 · 7 · 75 % |
| 7 | Playroom | 9.0 · 70 · 8 % | 4.5 · 24 · 21 % | 1.0 · 8 · 92 % |
| 8 | Playroom | 8.0 · 77 · 6 % | 2.0 · 33 · 27 % | 1.0 · 10 · 86 % |
| 9 | Playroom | 13.5 · 84 · 6 % | 3.5 · 37 · 24 % | 1.0 · 11 · 75 % |
| 10 | Playroom | 10.0 · 97 · 5 % | 2.0 · 40 · 30 % | 1.0 · 13 · 80 % |
| 11 | Playroom | 10.5 · 117 · 10 % | 2.0 · 45 · 33 % | 1.0 · 15 · 86 % |
| 12 | Playroom | 25.0 · — · 0 % | 4.0 · 50 · 27 % | 1.0 · 17 · 75 % |
| 13 | Playroom | — | 3.0 · 66 · 14 % | 1.0 · 18 · 100 % |
| 14 | Playroom | — | 4.0 · 82 · 18 % | 1.0 · 20 · 92 % |
| 15 | Garden | — | 2.0 · 85 · 40 % | 1.0 · 21 · 100 % |
| 16 | Garden | — | 2.0 · 88 · 52 % | 1.0 · 22 · 92 % |
| 17 | Garden | — | 1.0 · 89 · 52 % | 1.0 · 23 · 100 % |
| 18 | Garden | — | 1.0 · 90 · 92 % | 1.0 · 25 · 100 % |
| 19 | Garden | — | 1.0 · 96 · 52 % | 1.0 · 26 · 100 % |
| 20 | Garden | — | 2.5 · 106 · 31 % | 1.0 · 27 · 100 % |
| 21 | Garden | — | 1.5 · 108 · 48 % | 1.0 · 28 · 92 % |
| 22 | Garden | — | 2.0 · 111 · 52 % | 1.0 · 30 · 100 % |
| 23 | Garden | — | 2.0 · 116 · 34 % | 1.0 · 31 · 100 % |
| 24 | Garden | — | 1.5 · 118 · 35 % | 1.0 · 32 · 100 % |
| 25 | Snow | — | 2.0 · 123 · 29 % | 1.0 · 34 · 100 % |
| 26 | Snow | — | 2.0 · 124 · 36 % | 1.0 · 35 · 100 % |
| 27 | Snow | — | 2.0 · 128 · 19 % | 1.0 · 36 · 100 % |
| 28 | Snow | — | 2.5 · 133 · 22 % | 1.0 · 37 · 92 % |
| 29 | Snow | — | 2.0 · 135 · 39 % | 1.0 · 38 · 100 % |
| 30 | Snow | — | 3.0 · 143 · 20 % | 1.0 · 39 · 92 % |

## 2. Sensación de carrera (game feel)

Media por carrera de niveles. «Final ajustado» = menos de 1 s con el rival; «paliza» = más de 5 s de ventaja; «sin opción» = más de 5 s detrás.

| Perfil | Mundo | Carreras | Salidas de pista | Curvas perfectas | Derrapes salvados | Adelantamientos | Le adelantan | Cambios de líder | Golpes recibidos | Objetos usados | % derrapando | Final ajustado | Paliza | Sin opción | Salida perfecta | Salida en falso |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Novato (6-8 años) | Playroom | 790 | 0.00 | 1.89 | 0.11 | 4.7 | 6.2 | 3.3 | 0.49 | 2.8 | 0 % | 36 % | 0 % | 0 % | 26 % | 9 % |
| Casual | Playroom | 602 | 0.00 | 3.91 | 0.17 | 3.9 | 4.7 | 3.7 | 1.00 | 2.7 | 0 % | 62 % | 0 % | 0 % | 35 % | 10 % |
| Casual | Garden | 268 | 0.00 | 4.54 | 0.22 | 5.6 | 6.2 | 7.5 | 0.98 | 3.0 | 0 % | 75 % | 0 % | 0 % | 36 % | 6 % |
| Casual | Snow | 282 | 0.00 | 0.22 | 0.22 | 4.3 | 5.1 | 5.2 | 0.82 | 3.0 | 0 % | 62 % | 0 % | 0 % | 35 % | 9 % |
| Experto | Playroom | 194 | 0.00 | 7.00 | 0.12 | 2.4 | 2.5 | 2.8 | 1.30 | 1.5 | 0 % | 49 % | 0 % | 0 % | 87 % | 0 % |
| Experto | Garden | 122 | 0.00 | 8.39 | 0.10 | 1.9 | 1.9 | 2.4 | 1.39 | 1.0 | 0 % | 8 % | 1 % | 0 % | 84 % | 0 % |
| Experto | Snow | 74 | 0.00 | 0.38 | 0.18 | 1.9 | 1.9 | 2.8 | 1.34 | 1.3 | 0 % | 23 % | 0 % | 0 % | 81 % | 0 % |

## 3. Economía y recompensas

| Perfil | Monedas por carrera | Monedas gastadas en mejoras | Mejoras compradas al final | Vehículos al final | Cofres ganados | Cofres perdidos por ranuras llenas | Cofres abiertos | Cambios de coche |
|---|---|---|---|---|---|---|---|---|
| Novato (6-8 años) | 0.8 | 468 | 3.9 | 4.8 | 7.1 | 0 % | 7.0 | 0.8 |
| Casual | 6.6 | 1826 | 20.6 | 15.4 | 23.1 | 35 % | 19.3 | 3.0 |
| Experto | 16.7 | 1073 | 9.1 | 7.0 | 13.8 | 54 % | 9.8 | 0.0 |

## 4. Equilibrio de vehículos (contrarreloj de 2 vueltas, sin mejoras)

Diferencia de tiempo con la mediana de todos los coches (negativo = más rápido) y salidas de pista por carrera de un casual.

| Vehículo | Experto | Casual | Salidas (casual) |
|---|---|---|---|
| Skate | -3.7 % | -4.4 % | 0.00 |
| Gecko | -3.2 % | -3.0 % | 0.00 |
| Trike | -2.9 % | -2.5 % | 0.00 |
| Skateboard | -2.2 % | -2.8 % | 0.00 |
| Stroller | -1.6 % | -2.4 % | 0.00 |
| Quad | -2.4 % | -1.5 % | 0.00 |
| GolfCart | -1.0 % | -2.4 % | 0.00 |
| EScooter | -0.9 % | -1.5 % | 0.00 |
| Hopper | -1.2 % | -1.0 % | 0.00 |
| Snowmobile | -0.8 % | -1.0 % | 0.00 |
| Mower | -0.8 % | -0.9 % | 0.00 |
| Vespino | -0.5 % | -1.0 % | 0.00 |
| Cart | -0.6 % | -0.7 % | 0.00 |
| Tram | +0.3 % | -1.2 % | 0.00 |
| Moto | -0.4 % | -0.6 % | 0.00 |
| Wheelbarrow | -0.4 % | -0.5 % | 0.00 |
| Tractor | 0.0 % | -0.9 % | 0.00 |
| Forklift | -0.5 % | -0.1 % | 0.00 |
| Taxi | -0.2 % | +0.7 % | 0.00 |
| Bumper | +0.6 % | +0.4 % | 0.00 |
| Bike | +0.0 % | +0.9 % | 0.00 |
| Dart | +0.0 % | +0.9 % | 0.00 |
| Rocket | +1.0 % | +0.2 % | 0.00 |
| Train | +0.1 % | +1.7 % | 0.00 |
| Ambulance | +0.8 % | +1.1 % | 0.00 |
| HotDog | -0.1 % | +2.3 % | 0.00 |
| FireTruck | +1.7 % | +1.0 % | 0.00 |
| IceCream | +2.1 % | +1.1 % | 0.00 |
| Digger | +1.8 % | +1.5 % | 0.00 |
| Monster | +0.9 % | +2.6 % | 0.00 |
| Bathtub | +1.5 % | +2.5 % | 0.00 |
| Rhino | +1.4 % | +2.7 % | 0.00 |
| Sled | +2.3 % | +1.9 % | 0.00 |
| Camper | +1.8 % | +2.7 % | 0.00 |
| Hover | +2.5 % | +2.0 % | 0.00 |
| Bus | +3.0 % | +2.7 % | 0.00 |

## 5. Supervivencia

| Perfil | Carreras | Gana | Top 2 (cofre) | Eliminado en la vuelta 1 | Salidas de pista |
|---|---|---|---|---|---|
| Novato (6-8 años) | 30 | 10 % | 50 % | 23 % | 0.03 |
| Casual | 30 | 57 % | 90 % | 3 % | 0.00 |
| Experto | 30 | 87 % | 100 % | 0 % | 0.00 |

## 6. Errores y avisos del juego durante las pruebas

Ninguno.

---
Perfiles: **Novato** (curvas al 80 % del límite, reacciona en 0,38 s, se le va la mano en el 12 % de las curvas, apenas cambia de carril, usa los objetos al azar), **Casual** (87 %, 0,27 s, 5 %), **Experto** (95 %, 0,16 s, 1 %, busca el rebufo y usa los objetos en buen momento). Entre carreras abren cofres, ven anuncios para abrirlos antes (máx. 8 al día), compran mejoras y a veces juegan carreras libres cuando se atascan.
