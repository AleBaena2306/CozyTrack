"""Diseños de rueda de Cozy Track.

Todas las ruedas son mallas de tamaño unidad: radio 1 y ancho 1, con el eje en X.
Cada coche las coloca dentro de un vacío WheelSocket_XX escalado a (ancho, radio, radio),
así que cualquier diseño sirve para cualquier coche.

Todas tienen las mismas tres ranuras de material, en el mismo orden, para poder cambiar
la malla en Unity sin tocar los materiales:
  0 CT_WheelTyre (neumático), 1 CT_WheelRim (llanta), 2 CT_WheelCap (tapacubos y tornillos)
"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from mathutils import Vector
import common
from gen_props import Builder, star2d

common.PALETTE.update({
    "WheelTyre": (0.17, 0.17, 0.21),
    "WheelRim":  (0.92, 0.92, 0.95),
    "WheelCap":  (0.17, 0.17, 0.21),
})

TYRE, RIM, CAP = "WheelTyre", "WheelRim", "WheelCap"
AX = (0, math.pi / 2, 0)  # gira los cilindros de bmesh (eje Z) al eje X
SIDES = (-1, 1)


def _start(name):
    b = Builder(name)
    for m in (TYRE, RIM, CAP):   # fija el orden de las ranuras
        b.mi(m)
    return b


def _tyre(b, r=1.0, w=1.0):
    b.cone(TYRE, r1=r, r2=r, depth=w, seg=28, rot=AX, smooth=True)


def _cap(b, r=0.18, out=0.09):
    for s in SIDES:
        b.cone(CAP, loc=(s * (0.5 + out), 0, 0), r1=r, r2=r * 0.8, depth=0.08, seg=16, rot=(0, s * math.pi / 2, 0))


def _disc(b, mat, r, out, depth=0.06, seg=24):
    for s in SIDES:
        b.cone(mat, loc=(s * (0.5 + out), 0, 0), r1=r, r2=r, depth=depth, seg=seg, rot=AX)


def _ring(b, mat, R, thick, out):
    from gen_cars import tube
    for s in SIDES:
        pts = [(s * (0.5 + out), R * math.cos(2 * math.pi * i / 32), R * math.sin(2 * math.pi * i / 32)) for i in range(33)]
        tube(b, mat, pts, thick, sides=8)


def classic(b):
    _tyre(b)
    _disc(b, RIM, 0.55, 0.02)
    _cap(b)


def spokes(b):
    _tyre(b)
    _ring(b, RIM, 0.56, 0.06, 0.03)
    for s in SIDES:
        for k in range(5):
            a = 2 * math.pi * k / 5
            mid = Vector((s * 0.54, 0.28 * math.cos(a), 0.28 * math.sin(a)))
            b.cube(RIM, loc=mid, size=(0.07, 0.10, 0.56), rot=(a - math.pi / 2, 0, 0))
    _cap(b, r=0.2)


def star(b):
    _tyre(b)
    for s in SIDES:
        b.prism_polygon(RIM, star2d(0.66, 0.30, 5), 0, 0.07, loc=(s * 0.5, 0, 0), rot=(0, s * math.pi / 2, 0))
    _cap(b, r=0.16, out=0.1)


def turbine(b):
    _tyre(b)
    _disc(b, RIM, 0.62, 0.01, depth=0.04)
    for s in SIDES:
        for k in range(8):
            a = 2 * math.pi * k / 8
            p = Vector((s * 0.54, 0.4 * math.cos(a), 0.4 * math.sin(a)))
            b.cube(CAP, loc=p, size=(0.06, 0.07, 0.36), rot=(a - math.pi / 2 + 0.6, 0, 0))
    _cap(b, r=0.17, out=0.1)


def racing(b):
    _tyre(b)
    _disc(b, RIM, 0.66, 0.015, depth=0.05)
    for s in SIDES:
        for k in range(6):
            a = 2 * math.pi * k / 6
            b.cone(CAP, loc=(s * 0.56, 0.4 * math.cos(a), 0.4 * math.sin(a)), r1=0.07, r2=0.07, depth=0.05, seg=10, rot=AX)
    _cap(b, r=0.15, out=0.07)


def knobby(b):
    _tyre(b, r=0.9)
    n = 14
    for k in range(n):
        a = 2 * math.pi * k / n
        for s in (-1, 1):
            off = 0.22 * s if k % 2 == 0 else -0.22 * s
            if s == 1:
                b.cube(TYRE, loc=(off, 0.93 * math.cos(a), 0.93 * math.sin(a)), size=(0.4, 0.2, 0.16),
                       rot=(a - math.pi / 2, 0, 0), bevel=0.03)
    _disc(b, RIM, 0.5, 0.02)
    for s in SIDES:
        for k in range(5):
            a = 2 * math.pi * k / 5 + 0.3
            b.cone(CAP, loc=(s * 0.56, 0.3 * math.cos(a), 0.3 * math.sin(a)), r1=0.05, r2=0.05, depth=0.05, seg=8, rot=AX)
    _cap(b, r=0.14)


def whitewall(b):
    _tyre(b)
    _disc(b, RIM, 0.8, 0.005, depth=0.02, seg=32)   # banda blanca
    _disc(b, CAP, 0.5, 0.02, depth=0.04, seg=24)    # tapacubos cromado
    _cap(b, r=0.14, out=0.06)


STYLES = {
    "Classic": classic, "Spokes": spokes, "Star": star, "Turbine": turbine,
    "Racing": racing, "Knobby": knobby, "Whitewall": whitewall,
}
STYLE_NAMES_ES = {
    "Classic": "Clásica", "Spokes": "Radios", "Star": "Estrella", "Turbine": "Turbina",
    "Racing": "Deportiva", "Knobby": "Todoterreno", "Whitewall": "Banda blanca",
}


def build(style, name=None):
    b = _start(name or f"Wheel_{style}")
    STYLES[style](b)
    ob = b.finish(center_pivot=False)
    return ob
