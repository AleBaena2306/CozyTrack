# Cozy Track · informe de los bots de prueba

3240 carreras simuladas en 2.9 min reales · 12 jugadores por perfil · semilla 1234 · versión 0.0.30

Los bots juegan el juego real (misma física, IA, niveles, monedas, cofres y mejoras). Solo cambia quién pulsa: cada perfil tiene su precisión en las curvas, su tiempo de reacción, sus despistes y su forma de gastar.

## Alertas

- **Muro:** 3 bot(s) «Novato (6-8 años)» se atascan en el nivel 11 (25 intentos sin superarlo).
- **Muro:** 2 bot(s) «Novato (6-8 años)» se atascan en el nivel 22 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 17 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 9 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 12 (25 intentos sin superarlo).
- **Muro:** 3 bot(s) «Novato (6-8 años)» se atascan en el nivel 20 (25 intentos sin superarlo).
- **Muro:** 1 bot(s) «Novato (6-8 años)» se atascan en el nivel 14 (25 intentos sin superarlo).
- **Poca emoción:** solo el 7 % de las carreras de «Experto» en Garden acaban ajustadas (<1 s).
- **Cofres desperdiciados:** «Casual» pierde el 27 % de los cofres por tener las ranuras y la cola llenas.
- **Cofres desperdiciados:** «Experto» pierde el 34 % de los cofres por tener las ranuras y la cola llenas.

## 1. Progresión (30 niveles)

| Perfil | Acaban el juego | Atascados | Horas medianas hasta el final | Carreras medianas | Monedas ganadas/h | Cofres/h | Anuncios/h |
|---|---|---|---|---|---|---|---|
| Novato (6-8 años) | 0/12 | 12 | — | 109.5 | 87 | 5.7 | 2.6 |
| Casual | 12/12 | 0 | 1.5 | 61.5 | 281 | 21.8 | 5.7 |
| Experto | 12/12 | 0 | 0.6 | 31.0 | 589 | 45.6 | 12.1 |

Intentos medianos para superar cada nivel y minutos de juego acumulados al superarlo (mediana). «Ganadas» = % de intentos en que gana al rival.

| Nivel | Mundo | Novato (6-8 años): intentos · min · gana | Casual: intentos · min · gana | Experto: intentos · min · gana |
|---|---|---|---|---|
| 1 | Playroom | 2.0 · 1 · 48 % | 1.0 · 0 · 63 % | 1.0 · 0 · 86 % |
| 2 | Playroom | 2.5 · 4 · 25 % | 1.0 · 2 · 80 % | 1.0 · 1 · 100 % |
| 3 | Playroom | 2.0 · 7 · 55 % | 1.0 · 4 · 67 % | 1.0 · 2 · 100 % |
| 4 | Playroom | 3.0 · 14 · 26 % | 2.0 · 6 · 52 % | 1.0 · 4 · 100 % |
| 5 | Playroom | 3.0 · 21 · 27 % | 1.5 · 9 · 57 % | 1.0 · 5 · 80 % |
| 6 | Playroom | 3.5 · 27 · 17 % | 1.5 · 12 · 48 % | 1.0 · 6 · 92 % |
| 7 | Playroom | 3.5 · 34 · 19 % | 1.0 · 14 · 71 % | 1.0 · 8 · 92 % |
| 8 | Playroom | 6.0 · 43 · 16 % | 1.0 · 18 · 55 % | 1.0 · 9 · 92 % |
| 9 | Playroom | 7.5 · 61 · 9 % | 2.0 · 20 · 41 % | 1.0 · 11 · 92 % |
| 10 | Playroom | 7.0 · 75 · 12 % | 1.5 · 22 · 60 % | 1.0 · 12 · 100 % |
| 11 | Playroom | 5.0 · 73 · 7 % | 1.5 · 26 · 48 % | 1.0 · 14 · 92 % |
| 12 | Playroom | 5.0 · 82 · 11 % | 1.5 · 28 · 60 % | 1.0 · 16 · 92 % |
| 13 | Playroom | 6.0 · 107 · 12 % | 2.0 · 32 · 48 % | 1.0 · 17 · 92 % |
| 14 | Playroom | 8.0 · 106 · 8 % | 2.5 · 33 · 35 % | 1.0 · 18 · 92 % |
| 15 | Garden | 5.5 · 125 · 14 % | 1.0 · 35 · 67 % | 1.0 · 20 · 100 % |
| 16 | Garden | 4.0 · 142 · 12 % | 1.5 · 36 · 48 % | 1.0 · 21 · 100 % |
| 17 | Garden | 4.5 · 148 · 9 % | 1.0 · 39 · 67 % | 1.0 · 22 · 100 % |
| 18 | Garden | 4.0 · 155 · 17 % | 1.0 · 43 · 43 % | 1.0 · 23 · 100 % |
| 19 | Garden | 6.0 · 164 · 13 % | 2.0 · 47 · 43 % | 1.0 · 24 · 100 % |
| 20 | Garden | 25.0 · 200 · 2 % | 1.0 · 51 · 50 % | 1.0 · 26 · 100 % |
| 21 | Garden | 15.5 · 231 · 6 % | 1.0 · 52 · 67 % | 1.0 · 27 · 100 % |
| 22 | Garden | 25.0 · — · 0 % | 1.0 · 57 · 44 % | 1.0 · 28 · 100 % |
| 23 | Garden | — | 2.0 · 62 · 46 % | 1.0 · 29 · 92 % |
| 24 | Garden | — | 1.0 · 64 · 71 % | 1.0 · 31 · 100 % |
| 25 | Snow | — | 2.0 · 66 · 31 % | 1.0 · 32 · 100 % |
| 26 | Snow | — | 1.5 · 70 · 30 % | 1.0 · 33 · 92 % |
| 27 | Snow | — | 2.0 · 74 · 35 % | 1.0 · 34 · 100 % |
| 28 | Snow | — | 1.0 · 76 · 57 % | 1.0 · 35 · 86 % |
| 29 | Snow | — | 2.0 · 80 · 44 % | 1.0 · 37 · 100 % |
| 30 | Snow | — | 2.0 · 87 · 40 % | 1.0 · 38 · 100 % |

## 2. Sensación de carrera (game feel)

Media por carrera de niveles. «Final ajustado» = menos de 1 s con el rival; «paliza» = más de 5 s de ventaja; «sin opción» = más de 5 s detrás.

| Perfil | Mundo | Carreras | Salidas de pista | Curvas perfectas | Derrapes salvados | Adelantamientos | Le adelantan | Cambios de líder | Golpes recibidos | Objetos usados | % derrapando | Final ajustado | Paliza | Sin opción | Salida perfecta | Salida en falso |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Novato (6-8 años) | Playroom | 923 | 0.15 | 6.97 | 0.26 | 4.8 | 6.2 | 3.9 | 0.69 | 2.6 | 0 % | 47 % | 0 % | 0 % | 28 % | 11 % |
| Novato (6-8 años) | Garden | 405 | 0.26 | 7.46 | 0.30 | 7.3 | 8.7 | 5.2 | 0.55 | 3.0 | 0 % | 30 % | 0 % | 0 % | 24 % | 7 % |
| Casual | Playroom | 313 | 0.13 | 12.19 | 0.26 | 4.2 | 4.8 | 4.8 | 1.09 | 2.8 | 0 % | 72 % | 0 % | 0 % | 34 % | 9 % |
| Casual | Garden | 229 | 0.15 | 14.34 | 0.41 | 5.3 | 5.8 | 7.5 | 1.20 | 3.0 | 0 % | 75 % | 0 % | 0 % | 32 % | 6 % |
| Casual | Snow | 191 | 0.34 | 0.20 | 0.27 | 4.4 | 5.1 | 6.4 | 1.05 | 3.0 | 0 % | 63 % | 0 % | 0 % | 30 % | 6 % |
| Experto | Playroom | 181 | 0.07 | 20.33 | 0.08 | 1.8 | 1.9 | 2.1 | 0.99 | 1.2 | 0 % | 33 % | 1 % | 0 % | 88 % | 0 % |
| Experto | Garden | 121 | 0.02 | 23.07 | 0.07 | 1.5 | 1.5 | 1.7 | 0.93 | 0.9 | 0 % | 7 % | 2 % | 0 % | 88 % | 0 % |
| Experto | Snow | 75 | 0.11 | 0.37 | 0.13 | 2.1 | 2.1 | 3.2 | 1.37 | 1.6 | 0 % | 24 % | 0 % | 0 % | 87 % | 0 % |

## 3. Economía y recompensas

| Perfil | Monedas por carrera | Monedas gastadas en mejoras | Mejoras compradas al final | Vehículos al final | Cofres ganados | Cofres perdidos por ranuras llenas | Cofres abiertos | Cambios de coche |
|---|---|---|---|---|---|---|---|---|
| Novato (6-8 años) | 2.3 | 1283 | 13.2 | 10.5 | 16.8 | 0 % | 16.3 | 4.7 |
| Casual | 6.8 | 1220 | 13.3 | 9.3 | 31.3 | 27 % | 12.7 | 1.4 |
| Experto | 12.4 | 945 | 8.4 | 6.2 | 30.0 | 34 % | 9.8 | 0.0 |

## 4. Equilibrio de vehículos (contrarreloj de 2 vueltas, sin mejoras)

Diferencia de tiempo con la mediana de todos los coches (negativo = más rápido) y salidas de pista por carrera de un casual.

| Vehículo | Experto | Casual | Salidas (casual) |
|---|---|---|---|
| Skateboard | -2.8 % | -3.9 % | 0.00 |
| Skate | -3.4 % | -3.0 % | 0.00 |
| Gecko | -3.6 % | -2.1 % | 0.25 |
| Trike | -3.3 % | -2.0 % | 0.25 |
| Quad | -2.9 % | -1.9 % | 0.00 |
| GolfCart | -2.5 % | -1.5 % | 0.13 |
| EScooter | -1.1 % | -2.6 % | 0.00 |
| Stroller | -0.3 % | -2.5 % | 0.00 |
| Vespino | -1.0 % | -1.7 % | 0.00 |
| Forklift | -0.2 % | -0.9 % | 0.00 |
| Cart | -0.5 % | -0.5 % | 0.13 |
| Mower | -1.2 % | +0.5 % | 0.25 |
| Dart | +0.0 % | -0.5 % | 0.13 |
| Hopper | -0.8 % | +0.4 % | 0.25 |
| Taxi | +0.1 % | 0.0 % | 0.00 |
| HotDog | -0.3 % | +0.6 % | 0.00 |
| Moto | -0.3 % | +0.8 % | 0.13 |
| Tractor | +0.7 % | -0.2 % | 0.00 |
| Train | +0.5 % | +0.4 % | 0.25 |
| Rocket | +1.0 % | -0.1 % | 0.13 |
| Snowmobile | -0.7 % | +1.7 % | 0.38 |
| Wheelbarrow | -0.4 % | +1.4 % | 0.13 |
| Bumper | -0.1 % | +1.5 % | 0.13 |
| Ambulance | +1.0 % | +1.1 % | 0.25 |
| Bike | -0.1 % | +2.3 % | 0.38 |
| Monster | +1.0 % | +1.3 % | 0.13 |
| Bathtub | +1.6 % | +1.5 % | 0.13 |
| Rhino | +1.0 % | +2.5 % | 0.25 |
| Sled | +2.1 % | +1.5 % | 0.00 |
| IceCream | +1.7 % | +2.2 % | 0.00 |
| Tram | +2.3 % | +1.8 % | 0.00 |
| FireTruck | +2.4 % | +2.3 % | 0.00 |
| Camper | +1.8 % | +3.1 % | 0.13 |
| Digger | +2.0 % | +3.2 % | 0.13 |
| Bus | +3.2 % | +3.9 % | 0.13 |
| Hover | +2.2 % | +5.6 % | 0.25 |

## 5. Supervivencia

| Perfil | Carreras | Gana | Top 2 (cofre) | Eliminado en la vuelta 1 | Salidas de pista |
|---|---|---|---|---|---|
| Novato (6-8 años) | 30 | 10 % | 63 % | 20 % | 0.20 |
| Casual | 30 | 53 % | 93 % | 3 % | 0.13 |
| Experto | 30 | 100 % | 100 % | 0 % | 0.03 |

## 6. Errores y avisos del juego durante las pruebas

Ninguno.

---
Perfiles: **Novato** (curvas al 83 % del límite, reacciona en 0,38 s, suelta tarde en el 9 % de las curvas, apenas cambia de carril, usa los objetos al azar), **Casual** (88 %, 0,27 s, 4 %), **Experto** (95 %, 0,16 s, 1 %, busca el rebufo y usa los objetos en buen momento). Entre carreras abren cofres, ven anuncios para abrirlos antes (máx. 8 al día), compran mejoras y a veces juegan carreras libres cuando se atascan.
