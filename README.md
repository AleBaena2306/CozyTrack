# Cozy Track · informe de los bots de prueba

3030 carreras simuladas en 2.5 min reales · 12 jugadores por perfil · semilla 1234 · versión 0.0.25

Los bots juegan el juego real (misma física, IA, niveles, monedas, cofres y mejoras). Solo cambia quién pulsa: cada perfil tiene su precisión en las curvas, su tiempo de reacción, sus despistes y su forma de gastar.

## Alertas

- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 8 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 12 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 7 (25 intentos sin superarlo).
- **Muro:** 3 bot(s) «Novato (6-8 años)» se atascan en el nivel 9 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 5 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 6 (25 intentos sin superarlo).
- **Muro:** 4 bot(s) «Novato (6-8 años)» se atascan en el nivel 10 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Casual» se atascan en el nivel 12 (25 intentos sin superarlo).
- **Pico de dificultad:** el nivel 7 pide 5.0 intentos (mediana) a un jugador casual.
- **Pico de dificultad:** el nivel 30 pide 4.0 intentos (mediana) a un jugador casual.
- **Poca emoción:** solo el 7 % de las carreras de «Experto» en Garden acaban ajustadas (<1 s).
- **Cofres desperdiciados:** «Casual» pierde el 42 % de los cofres por tener las 4 ranuras llenas.
- **Cofres desperdiciados:** «Experto» pierde el 54 % de los cofres por tener las 4 ranuras llenas.

## 1. Progresión (30 niveles)

| Perfil | Acaban el juego | Atascados | Horas medianas hasta el final | Carreras medianas | Monedas ganadas/h | Cofres/h | Anuncios/h |
|---|---|---|---|---|---|---|---|
| Novato (6-8 años) | 0/12 | 12 | — | 76.5 | 52 | 4.8 | 2.5 |
| Casual | 11/12 | 1 | 2.0 | 87.0 | 353 | 10.2 | 4.2 |
| Experto | 12/12 | 0 | 0.6 | 32.0 | 795 | 20.2 | 11.6 |

Intentos medianos para superar cada nivel y minutos de juego acumulados al superarlo (mediana). «Ganadas» = % de intentos en que gana al rival.

| Nivel | Mundo | Novato (6-8 años): intentos · min · gana | Casual: intentos · min · gana | Experto: intentos · min · gana |
|---|---|---|---|---|
| 1 | Playroom | 2.5 · 2 · 28 % | 1.0 · 0 · 60 % | 1.0 · 0 · 92 % |
| 2 | Playroom | 2.0 · 6 · 32 % | 1.0 · 2 · 40 % | 1.0 · 1 · 75 % |
| 3 | Playroom | 4.0 · 14 · 21 % | 2.5 · 5 · 43 % | 1.0 · 3 · 92 % |
| 4 | Playroom | 4.0 · 20 · 20 % | 2.0 · 8 · 43 % | 1.0 · 4 · 80 % |
| 5 | Playroom | 8.0 · 31 · 10 % | 1.5 · 11 · 50 % | 1.0 · 5 · 92 % |
| 6 | Playroom | 6.0 · 43 · 10 % | 2.0 · 15 · 38 % | 1.0 · 7 · 92 % |
| 7 | Playroom | 7.5 · 54 · 10 % | 5.0 · 26 · 21 % | 1.0 · 9 · 71 % |
| 8 | Playroom | 5.0 · 68 · 10 % | 2.0 · 30 · 32 % | 1.0 · 10 · 86 % |
| 9 | Playroom | 18.5 · 80 · 4 % | 2.0 · 35 · 29 % | 1.0 · 12 · 63 % |
| 10 | Playroom | 25.0 · 83 · 1 % | 3.5 · 42 · 24 % | 1.0 · 14 · 86 % |
| 11 | Playroom | 8.0 · 96 · 12 % | 1.5 · 52 · 32 % | 1.0 · 15 · 80 % |
| 12 | Playroom | 25.0 · — · 0 % | 3.0 · 52 · 17 % | 1.0 · 17 · 92 % |
| 13 | Playroom | — | 2.0 · 63 · 28 % | 1.0 · 18 · 100 % |
| 14 | Playroom | — | 3.0 · 68 · 21 % | 1.0 · 19 · 100 % |
| 15 | Garden | — | 1.0 · 73 · 58 % | 1.0 · 20 · 100 % |
| 16 | Garden | — | 2.0 · 81 · 48 % | 1.0 · 22 · 100 % |
| 17 | Garden | — | 1.0 · 82 · 73 % | 1.0 · 23 · 100 % |
| 18 | Garden | — | 1.0 · 84 · 55 % | 1.0 · 24 · 100 % |
| 19 | Garden | — | 1.0 · 89 · 58 % | 1.0 · 25 · 100 % |
| 20 | Garden | — | 2.0 · 97 · 34 % | 1.0 · 26 · 92 % |
| 21 | Garden | — | 1.0 · 98 · 50 % | 1.0 · 28 · 100 % |
| 22 | Garden | — | 2.0 · 101 · 48 % | 1.0 · 29 · 100 % |
| 23 | Garden | — | 1.0 · 103 · 65 % | 1.0 · 30 · 100 % |
| 24 | Garden | — | 1.0 · 105 · 52 % | 1.0 · 32 · 100 % |
| 25 | Snow | — | 2.0 · 107 · 41 % | 1.0 · 33 · 100 % |
| 26 | Snow | — | 1.0 · 109 · 38 % | 1.0 · 34 · 100 % |
| 27 | Snow | — | 2.0 · 110 · 46 % | 1.0 · 35 · 86 % |
| 28 | Snow | — | 1.0 · 111 · 55 % | 1.0 · 37 · 100 % |
| 29 | Snow | — | 2.0 · 113 · 35 % | 1.0 · 38 · 100 % |
| 30 | Snow | — | 4.0 · 117 · 24 % | 1.0 · 39 · 86 % |

## 2. Sensación de carrera (game feel)

Media por carrera de niveles. «Final ajustado» = menos de 1 s con el rival; «paliza» = más de 5 s de ventaja; «sin opción» = más de 5 s detrás.

| Perfil | Mundo | Carreras | Salidas de pista | Curvas perfectas | Derrapes salvados | Adelantamientos | Le adelantan | Cambios de líder | Golpes recibidos | Objetos usados | % derrapando | Final ajustado | Paliza | Sin opción | Salida perfecta | Salida en falso |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Novato (6-8 años) | Playroom | 841 | 0.17 | 2.21 | 0.20 | 4.8 | 6.1 | 3.2 | 0.67 | 2.8 | 0 % | 38 % | 0 % | 0 % | 28 % | 10 % |
| Casual | Playroom | 543 | 0.12 | 4.42 | 0.22 | 3.9 | 4.7 | 3.8 | 1.11 | 2.7 | 0 % | 62 % | 0 % | 0 % | 33 % | 8 % |
| Casual | Garden | 211 | 0.20 | 5.27 | 0.53 | 5.1 | 5.6 | 7.5 | 1.35 | 3.0 | 0 % | 68 % | 0 % | 0 % | 35 % | 9 % |
| Casual | Snow | 177 | 0.33 | 0.07 | 0.23 | 4.8 | 5.4 | 6.9 | 1.02 | 3.0 | 0 % | 71 % | 0 % | 0 % | 34 % | 10 % |
| Experto | Playroom | 199 | 0.06 | 7.02 | 0.09 | 2.7 | 2.8 | 3.3 | 1.31 | 1.6 | 0 % | 52 % | 1 % | 0 % | 87 % | 0 % |
| Experto | Garden | 121 | 0.05 | 8.26 | 0.17 | 2.0 | 2.0 | 2.3 | 1.28 | 1.0 | 0 % | 7 % | 1 % | 0 % | 88 % | 0 % |
| Experto | Snow | 76 | 0.04 | 0.33 | 0.25 | 2.2 | 2.3 | 3.5 | 1.57 | 1.5 | 0 % | 30 % | 0 % | 0 % | 87 % | 0 % |

## 3. Economía y recompensas

| Perfil | Monedas por carrera | Monedas gastadas en mejoras | Mejoras compradas al final | Vehículos al final | Cofres ganados | Cofres perdidos por ranuras llenas | Cofres abiertos | Cambios de coche |
|---|---|---|---|---|---|---|---|---|
| Novato (6-8 años) | 1.3 | 582 | 5.0 | 5.1 | 8.7 | 0 % | 8.6 | 1.4 |
| Casual | 8.3 | 1644 | 16.8 | 11.8 | 20.5 | 42 % | 16.7 | 2.1 |
| Experto | 16.6 | 1073 | 9.1 | 6.8 | 13.9 | 54 % | 9.9 | 0.0 |

## 4. Equilibrio de vehículos (contrarreloj de 2 vueltas, sin mejoras)

Diferencia de tiempo con la mediana de todos los coches (negativo = más rápido) y salidas de pista por carrera de un casual.

| Vehículo | Experto | Casual | Salidas (casual) |
|---|---|---|---|
| Skate | -3.2 % | -3.8 % | 0.13 |
| Gecko | -2.8 % | -3.8 % | 0.13 |
| Quad | -2.3 % | -3.3 % | 0.13 |
| Skateboard | -2.4 % | -3.0 % | 0.00 |
| Trike | -2.8 % | -2.3 % | 0.25 |
| GolfCart | -1.5 % | -3.2 % | 0.00 |
| Hopper | -1.2 % | -1.3 % | 0.00 |
| EScooter | -0.8 % | -1.3 % | 0.25 |
| Cart | -0.9 % | -1.1 % | 0.00 |
| Vespino | -1.1 % | -0.8 % | 0.13 |
| Stroller | -1.2 % | -0.7 % | 0.00 |
| Snowmobile | -0.9 % | -1.0 % | 0.13 |
| Wheelbarrow | -0.5 % | -1.2 % | 0.25 |
| Mower | -0.5 % | -0.3 % | 0.25 |
| HotDog | +0.3 % | +0.0 % | 0.13 |
| Bike | +1.1 % | -0.7 % | 0.00 |
| Forklift | -0.4 % | +1.2 % | 0.00 |
| Dart | -0.1 % | +0.9 % | 0.25 |
| Bumper | +1.0 % | +0.1 % | 0.00 |
| Taxi | +0.1 % | +1.3 % | 0.38 |
| Tram | +1.3 % | +0.1 % | 0.13 |
| Tractor | +0.4 % | +1.2 % | 0.00 |
| Moto | -0.6 % | +2.2 % | 0.13 |
| Monster | +1.4 % | +1.4 % | 0.13 |
| Train | +0.6 % | +2.2 % | 0.38 |
| Digger | +1.6 % | +1.4 % | 0.00 |
| Rocket | +1.2 % | +1.9 % | 0.13 |
| Ambulance | +2.4 % | +1.2 % | 0.25 |
| Bathtub | +1.8 % | +1.8 % | 0.13 |
| FireTruck | +4.3 % | +0.3 % | 0.13 |
| Camper | +2.0 % | +2.7 % | 0.13 |
| Hover | +2.3 % | +2.7 % | 0.38 |
| Rhino | +1.4 % | +3.6 % | 0.38 |
| Sled | +1.8 % | +3.4 % | 0.00 |
| IceCream | +3.7 % | +2.5 % | 0.25 |
| Bus | +3.5 % | +4.4 % | 0.00 |

## 5. Supervivencia

| Perfil | Carreras | Gana | Top 2 (cofre) | Eliminado en la vuelta 1 | Salidas de pista |
|---|---|---|---|---|---|
| Novato (6-8 años) | 30 | 13 % | 63 % | 13 % | 0.30 |
| Casual | 30 | 57 % | 83 % | 3 % | 0.10 |
| Experto | 30 | 93 % | 100 % | 0 % | 0.07 |

## 6. Errores y avisos del juego durante las pruebas

Ninguno.

---
Perfiles: **Novato** (curvas al 83 % del límite, reacciona en 0,38 s, suelta tarde en el 9 % de las curvas, apenas cambia de carril, usa los objetos al azar), **Casual** (88 %, 0,27 s, 4 %), **Experto** (95 %, 0,16 s, 1 %, busca el rebufo y usa los objetos en buen momento). Entre carreras abren cofres, ven anuncios para abrirlos antes (máx. 8 al día), compran mejoras y a veces juegan carreras libres cuando se atascan.
