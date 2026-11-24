"""Herramienta de progresión de Cozy Track: niveles, mejoras, rivales y economía.

Uso: python progression.py <carpeta_salida>
Genera levels.json (datos de cada nivel para Unity), upgrades.json y progression.md (tablas).

Trazados: cada circuito se define por su MEDIA VUELTA (lista de piezas que gira 180° en total);
la vuelta completa es esa media vuelta repetida dos veces, y así el circuito cierra siempre.
Piezas: S8, S4 (rectas), C<ángulo><L|R><radio> (curvas), p. ej. C90L8, C45R5, C90L3.

Usa la misma física que el prototipo (ver physics_sim.py y proyectos/cozy-track-fisica.md).
"""
import sys, os, math, json
sys.path.insert(0, os.path.dirname(__file__))
import physics_sim as P

# ---------- física v4 (idéntica al prototipo) ----------
P.MAGNET, P.SLIP_GROW, P.SLIP_SCRUB = 2, 30, 1.5
BASE = dict(v0=11, kv=0.5, a0=7, ka=1.0, mu0=0.9, kmu=0.04)
CORNER_Q0, CORNER_Q_EQ = 0.85, 0.97
LANE_W = 1.2
HUMAN_SKILL = 0.92      # piloto «bueno»: entra a las curvas al 92 % del límite...
HUMAN_REACT = 0.3        # ...y reacciona en 0,3 s
CRASH_TIME = 2.0         # tiempo perdido por salida (vuelo + arrancar desde parado), medido en el prototipo ~1,2 s + aceleración

# ---------- mejoras: rendimiento decreciente ----------
UPG_SPEED = [0.04, 0.03, 0.025, 0.02, 0.015]     # +% velocidad punta por nivel
UPG_ACCEL = [0.06, 0.05, 0.04, 0.03, 0.02]       # +% aceleración de salida
UPG_WEIGHT = [0.05, 0.05, 0.04, 0.04, 0.03]      # +masa (más fuerza en choques, menos ayuda del imán)
UPG_COST = [60, 110, 180, 280, 420]               # monedas por nivel de cada mejora (igual en los 3 tipos)


def car(name, up=(0, 0, 0)):
    """Parámetros de un coche con niveles de mejora (velocidad, aceleración, peso)."""
    st = P.STATS[name]
    c = P.car_params(st, BASE)
    sp, ac, wt = up
    c["vtop"] *= 1 + sum(UPG_SPEED[:sp])
    a0 = c["F"] / c["m"] * (1 + sum(UPG_ACCEL[:ac]))
    c["m"] += sum(UPG_WEIGHT[:wt])
    c["F"] = c["m"] * a0
    c["Cd"] = c["F"] / (c["vtop"] ** 2 + P.CRR_RATIO * c["vtop"])
    c["Crr"] = P.CRR_RATIO * c["Cd"]
    return c


# ---------- trazados ----------
def parse(k):
    if k[0] == "S":
        return ("S", float(k[1:]))
    ang, side, R = int(k[1:3]), k[3], float(k[4:])
    return ("A", math.radians(ang), 1 if side == "L" else -1, R)


def check_half(half):
    turn = sum(math.degrees(p[1]) * p[2] for p in map(parse, half) if p[0] == "A")
    assert abs(turn - 180) < 1e-6, f"la media vuelta gira {turn}°, debe girar 180°"


def segments(layout, o):
    out = []
    for k in layout:
        p = parse(k)
        if p[0] == "S":
            out.append((p[1], math.inf))
        else:
            r = p[3] + p[2] * o
            out.append((p[1] * r, r))
    return out


LEVELS = [
    # n, mundo, nombre, media vuelta, carriles, factor de agarre de la superficie
    (1,  "Sala de juegos", "Primera vuelta",   ["S8", "S8", "C90L8", "S4", "C90L8"], 4, 1.0),
    (5,  "Sala de juegos", "Zigzag",           ["S8", "S8", "S8", "C90L8", "C45R8", "C45L8", "C90L8"], 4, 1.0),
    (10, "Sala de juegos", "La alfombra",      ["S8", "S8", "C90L8", "C45R8", "C45L8", "C45L8", "C45R8", "C90L8", "S8"], 4, 1.0),
    (15, "Jardín",         "Entre macetas",    ["S8", "S8", "S8", "C90L5", "S4", "C90R8", "C90L5", "C90L5", "S8"], 4, 1.0),
    (20, "Jardín",         "El seto",          ["S8", "S8", "S8", "C90L5", "C45R5", "C45L5", "C90L5", "C90R8", "C90L5", "S8"], 4, 1.0),
    (25, "Nieve",          "Horquillas",       ["S8", "S8", "S8", "C90L3", "C90L3", "S8", "S8", "C45R5", "C45L5"], 2, 0.85),
    (30, "Nieve",          "La gran helada",   ["S8", "S8", "S8", "S4", "C90L3", "C90L3", "C90R5", "C90L5", "C45L5", "C45R5", "S8", "S8"], 2, 0.85),
]


def target_upgrade(n):
    """Nivel de mejora que debería necesitar el jugador en el nivel n (curva de progresión)."""
    return min(5, round((n - 1) / 29 * 5))

def lane_offsets(n):
    return [(i - (n - 1) / 2) * LANE_W for i in range(n)]


# ---------- simulación ----------
def alat(c, grip):
    return P.alat(c) * grip


def run(c, segs, grip, policy, k=0.9, react=0.2, laps=3, dt=1 / 60):
    """policy: 'human' (suelta antes de las curvas y pisa dentro), 'hold' o 'ai' (pericia k)."""
    v = t = slip = 0.0
    crashes = 0
    n = len(segs)
    al = alat(c, grip)
    vs_of = lambda r: math.sqrt(al * r) if r < math.inf else math.inf
    for _ in range(laps):
        for i, (L, r) in enumerate(segs):
            x = 0.0
            vs = vs_of(r)
            while x < L:
                # curva siguiente más exigente en los próximos 45 m
                need_brake = False
                d = L - x
                for j in range(1, 7):
                    Lj, rj = segs[(i + j) % n]
                    if rj < math.inf:
                        f = k if policy == "ai" else HUMAN_SKILL
                        vt = vs_of(rj) * f
                        if v * v - vt * vt > 2 * P.MOTOR_BRAKE * max(0.0, d - v * react):
                            need_brake = True
                            break
                    d += Lj
                    if d > 45:
                        break
                if policy == "hold":
                    gas = True
                elif policy == "ai":
                    gas = not need_brake and v < c["vtop"] * (k + 0.02) and (r == math.inf or v < vs * k)
                else:
                    gas = not need_brake and not (r < math.inf and v > vs * HUMAN_SKILL ** 0.5)
                q = (v * v / r) / al if r < math.inf else 0
                corner = min(1.0, max(0.0, (q - CORNER_Q0) / (CORNER_Q_EQ - CORNER_Q0))) * c["F"] if r < math.inf else 0
                a = ((c["F"] if gas else 0) - c["Cd"] * v * v - c["Crr"] * v - corner) / c["m"]
                if not gas:
                    a -= P.MOTOR_BRAKE
                if q > 1:
                    slip += P.SLIP_GROW * (q - 1) * dt
                else:
                    slip = max(0.0, slip - P.SLIP_RECOVER * dt)
                a -= P.SLIP_SCRUB * slip
                if slip >= 1:
                    crashes += 1
                    slip = 0
                    v = 0
                    t += CRASH_TIME - 0.8
                v = max(0.0, v + a * dt)
                x += v * dt
                t += dt
    return t, crashes


def race_time(c, layout, lanes, grip, policy, **kw):
    """Media de los carriles centrales (en 2 carriles, los dos)."""
    offs = lane_offsets(lanes)
    use = offs[1:-1] if lanes > 2 else offs
    res = [run(c, segments(layout, o), grip, policy, **kw) for o in use]
    return sum(r[0] for r in res) / len(res), sum(r[1] for r in res)


def difficulty(layout, lanes, grip, carname="Dart"):
    """Exigencia: velocidad punta / velocidad segura en la curva más cerrada (carril central)."""
    c = car(carname)
    o = 0.0 if lanes % 2 else LANE_W / 2
    rmin = min(r for _, r in segments(layout, -o) + segments(layout, o))
    return c["vtop"] / math.sqrt(alat(c, grip) * rmin), rmin


# ---------- progresión ----------
RIVAL_CARS = ["Rhino", "Gecko", "Hopper", "Dart"]


def rival_for(n):
    """Rival a batir del nivel n (1..30): coche, mejoras y pericia."""
    name = RIVAL_CARS[(n - 1) // 3 % 4]
    lvl = min(5, (n - 1) // 6 + (1 if n % 6 >= 3 else 0))     # de 0 a 5 a lo largo de los 30 niveles
    k = round(0.84 + 0.06 * (n - 1) / 29, 3)                  # pericia de 0,84 a 0,90
    return name, (lvl, lvl, min(lvl, 3)), k


def main(out):
    os.makedirs(out, exist_ok=True)
    players = list(P.STATS)
    levels_json, rows = [], []
    for (n, world, title, half, lanes, grip) in LEVELS:
        check_half(half)
        layout = half + half
        lap_len = sum(L for L, _ in segments(layout, 0))
        straight = sum(L for L, r in segments(layout, 0) if r == math.inf) / lap_len
        diff, rmin = difficulty(layout, lanes, grip)
        rname, rup, _ = rival_for(n)
        # tiempos del piloto «bueno» (media de los 4 coches) según su nivel de mejora uniforme
        t_up = []
        for u in range(6):
            ts = [race_time(car(p, (u, u, min(u, 3))), layout, lanes, grip, "human", react=HUMAN_REACT)[0] for p in players]
            t_up.append(sum(ts) / len(ts))
        u_t = target_upgrade(n)
        # tiempo objetivo del rival: con la mejora prevista se gana por algo más de 1 s; con una menos, no
        hi = t_up[u_t] + 1.0
        lo = t_up[u_t - 1] + 1.0 if u_t > 0 else hi + 3.0
        target_t = (hi + lo) / 2
        sens = (t_up[0] - t_up[5]) / t_up[0]
        # pericia del rival por bisección
        a, b = 0.6, 0.99
        for _ in range(25):
            m = (a + b) / 2
            rt, _ = race_time(car(rname, rup), layout, lanes, grip, "ai", k=m, react=0.15)
            if rt > target_t: a = m
            else: b = m
        rk = round((a + b) / 2, 3)
        rt, _ = race_time(car(rname, rup), layout, lanes, grip, "ai", k=rk, react=0.15)
        rec = {}
        for pname in players:
            rec[pname] = None
            for u in range(6):
                ht, _ = race_time(car(pname, (u, u, min(u, 3))), layout, lanes, grip, "human", react=HUMAN_REACT)
                if ht <= rt - 1.0:
                    rec[pname] = u
                    break
        # coche al máximo conduciendo mal (2 salidas extra) contra el rival
        bad = {p: round(race_time(car(p, (5, 5, 3)), layout, lanes, grip, "human", react=HUMAN_REACT)[0] + 2 * CRASH_TIME - rt, 1) for p in players}
        warn = "poco sensible a mejoras: alargar rectas" if sens < 0.04 else ""
        levels_json.append({
            "level": n, "world": world, "name": title, "lanes": lanes, "surfaceGrip": grip,
            "halfLap": half, "lap": layout, "laps": 3, "lapLength_m": round(lap_len, 1),
            "straightShare": round(straight, 2), "tightestRadius_m": round(rmin, 2), "difficulty": round(diff, 2),
            "upgradeSensitivity": round(sens, 3),
            "rival": {"car": rname, "upgrades": {"speed": rup[0], "acceleration": rup[1], "weight": rup[2]},
                      "skill": rk, "raceTime_s": round(rt, 1)},
            "targetUpgradeLevel": u_t, "recommendedPerCar": rec,
        })
        rows.append((n, world, title, lanes, grip, round(lap_len), f"{straight:.0%}", round(rmin, 1), round(diff, 2),
                     f"{sens:.1%}", f"{rname} {rup[0]}/{rup[1]}/{rup[2]} k={rk}", round(rt, 1), u_t, rec, bad, warn))
        print(f"nivel {n:2} {title:15} rectas {straight:.0%} dific {diff:.2f} sensib {sens:.1%} rival {rname} k={rk} t={rt:.1f} objetivo {u_t} recomendado {rec} al máximo con 2 salidas: {bad} {warn}")

    # economía: monedas para llegar a la mejora recomendada
    cum = [0]
    for cst in UPG_COST:
        cum.append(cum[-1] + cst)
    with open(os.path.join(out, "levels.json"), "w", encoding="utf-8") as fh:
        json.dump(levels_json, fh, indent=2, ensure_ascii=False)
    with open(os.path.join(out, "upgrades.json"), "w", encoding="utf-8") as fh:
        json.dump({"speed": UPG_SPEED, "acceleration": UPG_ACCEL, "weight": UPG_WEIGHT, "costPerLevel": UPG_COST,
                   "cumulativeCostOneStat": cum, "base": BASE, "magnet": P.MAGNET,
                   "cornerQ0": CORNER_Q0, "cornerQeq": CORNER_Q_EQ, "slipGrow": P.SLIP_GROW,
                   "slipScrub": P.SLIP_SCRUB, "slipRecover": P.SLIP_RECOVER, "motorBrake": P.MOTOR_BRAKE},
                  fh, indent=2)
    md = ["# Cozy Track · progresión (generado por tools/progression.py)", "",
          "| Nivel | Mundo | Nombre | Carriles | Agarre pista | Vuelta (m) | Rectas | Curva más cerrada (m) | Exigencia | Efecto de las mejoras | Rival (mejoras v/a/p, pericia) | Tiempo rival (s) | Mejora prevista | Mejora necesaria por coche | Coche al máximo con 2 salidas (s respecto al rival; >0 pierde) | Aviso |",
          "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|"]
    for r in rows:
        md.append("| " + " | ".join(str(x) if not isinstance(x, dict) else ", ".join(f"{k} {v}" for k, v in x.items()) for x in r) + " |")
    md += ["", "Exigencia = velocidad punta del Dart / velocidad segura en la curva más cerrada (carril central).",
           "Mejora necesaria = nivel uniforme (velocidad, aceleración y peso hasta 3) con el que un piloto «bueno» (entra al 92 % del límite, reacciona en 0,3 s) gana al rival por 1 s o más.",
           "Efecto de las mejoras = cuánto más rápido es el coche al máximo que de serie en ese circuito. La pericia del rival se calcula para que la mejora necesaria sea la prevista.",
           "", f"Coste acumulado de una mejora por niveles: {cum[1:]} monedas (×3 mejoras por coche)."]
    with open(os.path.join(out, "progression.md"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(md) + "\n")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "progression_out")
