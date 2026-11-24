"""Física de Cozy Track (v3) y simulador para equilibrar los coches.

Basado en:
- Monster, "Car Physics for Games": F_long = F_motor + F_drag + F_rr, F_drag = -Cd·v|v|, F_rr = -Crr·v,
  Crr ≈ 30·Cd, velocidad punta emergente (sin tope fijo), agarre máximo F = μ·carga.
- Tesis "Development of a car physics engine for games": frenada constante, colisiones por impulso
  J = -(1+e)·Vr / (1/m1 + 1/m2).
- Delbruck, Mueggler et al., "Human vs. Computer Slot Car Racing" (apéndice J): el slot car tiene imán
  que lo sujeta a los raíles, acelera y frena en ~0,3 s (aquí más lento, escala juguete) y la IA usa un
  perfil de acelerador por tramo derivado de la curvatura, con ralentización cuando va por delante.

Modelo de slot car (sin dirección: la guía sigue el carril):
- Longitudinal: a = (F_motor·acelerador - Cd·v² - Crr·v)/m ; al soltar, freno motor constante.
- Lateral: aceleración lateral pedida = v²/r ; disponible = μ·(g + F_imán/m).
  El imán es igual en todos: a un coche pesado le ayuda menos (el peso tiene coste en curva).
- Derrape: si q = pedida/disponible > 1, la cola sale (estado slip 0..1) a ritmo K·(q-1); si q < 1, vuelve.
  Mientras derrapa pierde velocidad (rozamiento lateral). slip ≥ 1 → se sale de la pista.
"""
import math, itertools

G = 9.8
CRR_RATIO = 30.0         # Crr = 30·Cd (Monster)
MAGNET = 4.0             # fuerza del imán (por unidad de masa de referencia)
SLIP_GROW = 14.0         # velocidad de salida de cola por unidad de exceso
SLIP_RECOVER = 3.0       # recuperación por segundo
SLIP_SCRUB = 7.0         # deceleración extra (m/s²) a slip = 1
MOTOR_BRAKE = 12.0       # deceleración al soltar (m/s²)
RESTITUTION = 0.3
CORNER_Q0 = 0.85         # resistencia en curva (deriva de los neumáticos): empieza en q = 0,85 ...
CORNER_Q_EQ = 0.97       # ... e iguala al motor en q = 0,97: a fondo dentro de la curva, el coche se estabiliza ahí
CORNER_CAP = 1.0         # la resistencia en curva como mucho iguala al motor: si entras pasado, hay que soltar

STATS = {"Dart": [5, 2, 3, 2], "Hopper": [3, 5, 2, 1], "Gecko": [2, 4, 5, 2], "Rhino": [3, 1, 4, 5]}


def car_params(st, P):
    speed, acc, grip, weight = st
    m = 0.8 + 0.15 * weight
    vtop = P["v0"] + P["kv"] * speed
    a0 = P["a0"] + P["ka"] * acc
    F = m * a0
    Cd = F / (vtop ** 2 + CRR_RATIO * vtop)
    mu = P["mu0"] + P["kmu"] * grip
    return {"m": m, "F": F, "Cd": Cd, "Crr": CRR_RATIO * Cd, "mu": mu, "vtop": vtop}


def alat(c):
    return c["mu"] * (G + MAGNET / c["m"])


def vsafe(c, r):
    return math.sqrt(alat(c) * r)


# circuito del prototipo
CH = ["C45R", "C45L", "C45L", "C45R"]
LAYOUT = ["SF", "S8", "C90L", *CH, "C90L", "S8", "S8", "C90L", *CH, "C90L"]


def segments(o):
    out = []
    for k in LAYOUT:
        if k[0] == "S":
            out.append((8.0, math.inf))
        else:
            A = math.radians(90 if k.startswith("C90") else 45)
            s = 1 if k.endswith("L") else -1
            r = 8 + s * o
            out.append((A * r, r))
    return out


def simulate(c, o, policy, laps=2, dt=1 / 60):
    S = segments(o)
    v = t = 0.0
    slip = 0.0
    crashes = 0
    peak_slip = 0.0
    for _ in range(laps):
        for i, (L, r) in enumerate(S):
            x = 0.0
            nr = S[(i + 1) % len(S)][1]
            while x < L:
                vs = vsafe(c, r) if r < math.inf else math.inf
                vsn = vsafe(c, nr) if nr < math.inf else math.inf
                if policy == "hold":
                    gas = True
                elif policy == "human":
                    # suelta antes de la curva (con 0,2 s de reacción) y mantiene pulsado dentro de ella
                    # (dentro de una curva también suelta si la siguiente es más cerrada: chicanes)
                    d = L - x - v * 0.2
                    gas = (v * v - vsn ** 2) < 2 * MOTOR_BRAKE * max(0.0, d) if vsn < vs or r == math.inf else True
                    if r < math.inf and v > vs:
                        gas = False
                else:  # piloto que suelta a tiempo y apura hasta el 98 %
                    gas = v < 0.98 * vs and (v * v - (0.98 * vsn) ** 2) < 2 * MOTOR_BRAKE * (L - x) * 0.9
                q0 = (v * v / r) / alat(c) if r < math.inf else 0
                corner = min(CORNER_CAP, max(0.0, (q0 - CORNER_Q0) / (CORNER_Q_EQ - CORNER_Q0))) * c["F"] if r < math.inf else 0
                a = ((c["F"] if gas else 0) - c["Cd"] * v * v - c["Crr"] * v - corner) / c["m"]
                if not gas:
                    a -= MOTOR_BRAKE
                q = (v * v / r) / alat(c) if r < math.inf else 0
                if q > 1:
                    slip += SLIP_GROW * (q - 1) * dt
                else:
                    slip = max(0.0, slip - SLIP_RECOVER * dt)
                a -= SLIP_SCRUB * slip
                peak_slip = max(peak_slip, slip)
                if slip >= 1:
                    crashes += 1
                    slip = 0
                    v = 0
                    t += 1.2
                v = max(0.0, v + a * dt)
                x += v * dt
                t += dt
    return crashes, t, peak_slip


def time_to(c, frac=0.9, dt=1 / 120):
    v = t = 0
    while v < frac * c["vtop"] and t < 20:
        v += (c["F"] - c["Cd"] * v * v - c["Crr"] * v) / c["m"] * dt
        t += dt
    return t


def evaluate(P, verbose=False):
    cars = {n: car_params(st, P) for n, st in STATS.items()}
    times, ok = {}, True
    rows = []
    for n, c in cars.items():
        hold = [simulate(c, o, "hold")[0] for o in (-0.6, 0.6)]
        smart = [simulate(c, o, "human") for o in (-1.8, -0.6, 0.6, 1.8)]
        if min(hold) == 0 or any(s[0] for s in smart):
            ok = False
        times[n] = sum(s[1] for s in smart[1:3]) / 2
        ratio_c = c["vtop"] / vsafe(c, 8)
        ratio_i = c["vtop"] / vsafe(c, 6.2)
        rows.append((n, c, hold, [round(s[1], 1) for s in smart], ratio_c, ratio_i, time_to(c)))
    if verbose:
        for n, c, hold, sm, rc, ri, t90 in rows:
            print(f"{n:7} m={c['m']:.2f} vtop={c['vtop']:.1f} a0={c['F']/c['m']:.1f} 0-90%={t90:.2f}s "
                  f"alat={alat(c):.1f} vsafe(r8)={vsafe(c,8):.1f} vtop/vsafe centro={rc:.2f} interior={ri:.2f} "
                  f"salidas sin soltar={hold} tiempos 2 vueltas (humano) por carril={sm}")
    ts = list(times.values())
    return ok, (max(ts) - min(ts)) / min(ts), times, rows


if __name__ == "__main__":
    best = []
    for v0, kv, a0, ka, mu0, kmu in itertools.product([10, 11], [0.5, 0.7, 0.9], [3, 4, 5], [1.0, 1.5, 2.0],
                                                      [0.9, 1.0, 1.1], [0.05, 0.08, 0.11]):
        P = dict(v0=v0, kv=kv, a0=a0, ka=ka, mu0=mu0, kmu=kmu)
        ok, spread, times, rows = evaluate(P)
        if not ok:
            continue
        # el más rápido de punta debe exigir soltar claramente, el de agarre poco
        rc = {r[0]: r[4] for r in rows}
        if not (rc["Dart"] > 1.12 and rc["Gecko"] > 0.97):
            continue
        best.append((spread, P, {k: round(v, 1) for k, v in times.items()}))
    best.sort(key=lambda b: b[0])
    for b in best[:5]:
        print(b)
    print()
    evaluate(best[0][1], verbose=True)
