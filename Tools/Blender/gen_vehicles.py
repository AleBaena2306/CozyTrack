"""Vehículos de Cozy Track: 32 vehículos de juguete genéricos (sin marcas reales), hechos por script en Blender.

Uso: python gen_vehicles.py <carpeta_salida> [--sheet]
Mismas convenciones que gen_cars.py: avance hacia -Y, pivote a ras de pista, ranuras CT_Paint_<V> y
CT_Trim_<V> para las skins, ruedas como WheelSocket_XX + Wheel_XX (intercambiables), UV a escala del mundo.
"""
import sys, os, math, json
sys.path.insert(0, os.path.dirname(__file__))
import bpy
from mathutils import Vector
from common import *
import common
import gen_cars as C
from gen_props import Builder, star2d

common.PALETTE.update({"Chrome": (0.86, 0.88, 0.92), "Seat": (0.25, 0.22, 0.32), "Water": (0.55, 0.82, 1.0),
                       "Flame": (1.0, 0.55, 0.15), "Wood": (0.78, 0.55, 0.32), "Cream": (1.0, 0.95, 0.85)})

VEH = {}   # nombre: (pintura, detalles, stats[vel, acel, agarre, peso], tipo, descripción)


def vehicle(name, paint, trim, stats, kind, desc):
    def deco(fn):
        VEH[name] = (paint, trim, stats, kind, desc, fn)
        return fn
    return deco


class K:
    """Atajos de modelado sobre Builder."""
    def __init__(self, name, paint, trim):
        common.PALETTE[f"Paint_{name}"] = common.PALETTE[paint]
        common.PALETTE[f"Trim_{name}"] = common.PALETTE[trim]
        self.P, self.T = f"Paint_{name}", f"Trim_{name}"
        self.b = Builder(f"Car_{name}")
        self.wheels = []
        self.style = "Classic"

    def box(self, m, x, y, z, sx, sy, sz, bev=0.03, rx=0.0):
        self.b.cube(m, loc=(x, y, z), size=(sx, sy, sz), bevel=bev, rot=(rx, 0, 0))

    def cyl(self, m, x, y, z, r, d, axis="z", seg=16, r2=None, smooth=True):
        rot = {"x": (0, math.pi / 2, 0), "y": (math.pi / 2, 0, 0), "z": (0, 0, 0)}[axis]
        self.b.cone(m, loc=(x, y, z), r1=r, r2=r if r2 is None else r2, depth=d, seg=seg, rot=rot, smooth=smooth)

    def sph(self, m, x, y, z, r, sc=(1, 1, 1), seg=(16, 8)):
        self.b.sphere(m, loc=(x, y, z), r=r, scale=sc, seg=seg)

    def tube(self, m, pts, r=0.02):
        C.tube(self.b, m, pts, r, sides=8)

    def wheel(self, n, x, y, r, w):
        self.wheels.append((f"Wheel_{n}", x, y, r, w))

    def wheels4(self, xf, yf, rf, wf, xr=None, yr=None, rr=None, wr=None):
        xr, yr, rr, wr = xr or xf, yr if yr is not None else -yf, rr or rf, wr or wf
        for n, x, y, r, w in (("FL", -xf, yf, rf, wf), ("FR", xf, yf, rf, wf), ("RL", -xr, yr, rr, wr), ("RR", xr, yr, rr, wr)):
            self.wheel(n, x, y, r, w)

    def finish(self):
        ob = self.b.finish()
        C.box_uv(ob)
        if self.wheels:
            C.attach_wheels(ob, self.wheels, style=self.style)
        ob["base_name"] = ob.name
        return ob


# ============ vehículos cotidianos ============
@vehicle("Cart", "Sky", "Coral", [2, 4, 3, 2], "Cotidiano", "Carrito de la compra")
def _(k):
    for i in range(6):                                   # cesta de rejilla
        y = -0.45 + i * 0.18
        k.tube(k.P, [(-0.3, y, 0.35), (-0.3, y, 0.75), (0.3, y, 0.75), (0.3, y, 0.35)], 0.018)
    for x in (-0.3, 0.3):
        k.tube(k.P, [(x, -0.47, 0.75), (x, 0.47, 0.75)], 0.02)
        k.tube(k.P, [(x, -0.47, 0.35), (x, 0.47, 0.35)], 0.02)
    k.box(k.P, 0, 0, 0.34, 0.62, 0.96, 0.03, 0.01)
    k.tube(k.T, [(-0.32, 0.5, 0.9), (0.32, 0.5, 0.9)], 0.035)          # asa
    for x in (-0.3, 0.3):
        k.tube(k.P, [(x, 0.47, 0.75), (x, 0.5, 0.9)], 0.018)
        k.tube("Chrome", [(x, -0.4, 0.34), (x, -0.4, 0.12)], 0.02)
        k.tube("Chrome", [(x, 0.4, 0.34), (x, 0.4, 0.12)], 0.02)
    k.wheels4(0.3, -0.4, 0.09, 0.06)


@vehicle("EScooter", "Mint", "Night", [3, 5, 2, 1], "Cotidiano", "Patinete eléctrico")
def _(k):
    k.box(k.P, 0, 0.02, 0.14, 0.18, 0.95, 0.06, 0.03)
    k.tube(k.T, [(0, -0.45, 0.16), (0, -0.52, 0.95)], 0.03)
    k.tube(k.T, [(-0.25, -0.52, 0.95), (0.25, -0.52, 0.95)], 0.025)
    for x in (-0.27, 0.27):
        k.cyl(k.P, x, -0.52, 0.95, 0.035, 0.08, "x")
    k.box("Light", 0, -0.53, 0.8, 0.08, 0.04, 0.05, 0.01)
    k.wheel("F", 0, -0.48, 0.11, 0.07)
    k.wheel("R", 0, 0.48, 0.11, 0.07)


@vehicle("Skate", "Bubblegum", "Sun", [3, 4, 4, 1], "Cotidiano", "Patín de cuatro ruedas gigante")
def _(k):
    k.box(k.P, 0, 0.05, 0.42, 0.42, 0.5, 0.5, 0.12)                    # bota
    k.box(k.P, 0, -0.2, 0.28, 0.42, 0.55, 0.22, 0.1)
    k.box(k.T, 0, -0.05, 0.15, 0.46, 1.0, 0.06, 0.02)                   # suela
    k.cyl(k.T, 0, -0.52, 0.13, 0.06, 0.08, "y")                          # freno
    for z in (0.35, 0.45, 0.55):
        k.box("Snow", 0, -0.2 if z < 0.4 else -0.1, z, 0.36, 0.02, 0.02, 0.005)
    k.wheels4(0.17, -0.32, 0.09, 0.08)


@vehicle("Skateboard", "Tangerine", "Night", [4, 4, 3, 1], "Cotidiano", "Monopatín")
def _(k):
    k.box(k.P, 0, 0, 0.16, 0.36, 1.1, 0.04, 0.05)
    k.box(k.P, 0, -0.58, 0.19, 0.36, 0.12, 0.04, 0.05, rx=0.35)
    k.box(k.P, 0, 0.58, 0.19, 0.36, 0.12, 0.04, 0.05, rx=-0.35)
    k.box(k.T, 0, 0, 0.183, 0.3, 0.9, 0.005, 0.0)
    for y in (-0.36, 0.36):
        k.box("Chrome", 0, y, 0.11, 0.3, 0.06, 0.04, 0.01)
    k.wheels4(0.15, -0.36, 0.06, 0.06)


@vehicle("Bike", "Coral", "Snow", [4, 3, 2, 1], "Cotidiano", "Bicicleta")
def _(k):
    z = 0.32
    k.tube(k.P, [(0, -0.42, z), (0, -0.05, 0.36), (0, 0.4, z)], 0.025)
    k.tube(k.P, [(0, -0.05, 0.36), (0, 0.05, 0.62), (0, -0.32, 0.62), (0, -0.42, z)], 0.025)
    k.tube(k.P, [(0, 0.05, 0.62), (0, 0.4, z)], 0.025)
    k.box(k.T, 0, 0.08, 0.68, 0.12, 0.22, 0.05, 0.02)                  # sillín
    k.tube("Chrome", [(0, -0.32, 0.62), (0, -0.36, 0.76)], 0.02)
    k.tube("Chrome", [(-0.22, -0.36, 0.76), (0.22, -0.36, 0.76)], 0.02)
    k.box(k.T, 0, -0.45, 0.62, 0.22, 0.14, 0.12, 0.03)                  # cesta delantera
    k.wheel("F", 0, -0.42, 0.3, 0.05)
    k.wheel("R", 0, 0.42, 0.3, 0.05)


@vehicle("Trike", "Sun", "Coral", [2, 4, 4, 1], "Cotidiano", "Triciclo")
def _(k):
    k.tube(k.P, [(0, -0.3, 0.3), (0, 0.1, 0.38), (0, 0.3, 0.2)], 0.035)
    k.tube(k.P, [(-0.28, 0.3, 0.2), (0.28, 0.3, 0.2)], 0.03)
    k.box(k.T, 0, 0.12, 0.45, 0.2, 0.2, 0.06, 0.03)
    k.tube(k.P, [(0, -0.3, 0.3), (0, -0.33, 0.6)], 0.03)
    k.tube(k.T, [(-0.2, -0.33, 0.6), (0.2, -0.33, 0.6)], 0.025)
    k.wheel("F", 0, -0.32, 0.24, 0.07)
    k.wheel("RL", -0.28, 0.3, 0.14, 0.07)
    k.wheel("RR", 0.28, 0.3, 0.14, 0.07)


@vehicle("Stroller", "Lilac", "Snow", [2, 3, 4, 2], "Cotidiano", "Carrito de bebé")
def _(k):
    k.sph(k.P, 0, 0.05, 0.55, 0.36, sc=(0.85, 1.25, 0.65), seg=(20, 10))
    k.box("Seat", 0, -0.1, 0.62, 0.5, 0.5, 0.14, 0.05)
    k.b.sphere(k.T, loc=(0, 0.28, 0.72), r=0.34, scale=(0.9, 0.7, 0.7), seg=(16, 8))   # capota
    k.tube("Chrome", [(-0.26, 0.42, 0.55), (-0.26, 0.6, 0.95), (0.26, 0.6, 0.95), (0.26, 0.42, 0.55)], 0.02)
    for x in (-0.26, 0.26):
        k.tube("Chrome", [(x, -0.3, 0.35), (x, -0.3, 0.15)], 0.018)
        k.tube("Chrome", [(x, 0.3, 0.35), (x, 0.3, 0.15)], 0.018)
    k.wheels4(0.28, -0.3, 0.13, 0.05)


@vehicle("Wheelbarrow", "Leaf", "Wood", [1, 3, 4, 3], "Cotidiano", "Carretilla")
def _(k):
    k.b.hull(k.P, [(sx * w, y, z) for sx in (-1, 1) for (w, y, z) in
                   ((0.18, -0.25, 0.25), (0.34, -0.45, 0.6), (0.2, 0.25, 0.25), (0.34, 0.35, 0.6))])
    k.box("Night", 0, -0.02, 0.59, 0.6, 0.72, 0.02, 0.0)
    for x in (-0.18, 0.18):
        k.tube(k.T, [(x * 0.3, -0.45, 0.2), (x, 0.2, 0.3), (x * 1.5, 0.75, 0.55)], 0.03)
        k.tube(k.T, [(x, 0.25, 0.28), (x, 0.3, 0.05)], 0.025)
    k.wheel("F", 0, -0.45, 0.2, 0.09)


@vehicle("Bathtub", "Snow", "Gold", [3, 2, 3, 4], "Cotidiano", "Bañera con ruedas")
def _(k):
    k.sph(k.P, 0, 0, 0.42, 0.45, sc=(0.85, 1.45, 0.55), seg=(24, 12))
    k.box(k.P, 0, 0, 0.62, 0.78, 1.32, 0.12, 0.1)
    k.box("Water", 0, 0, 0.67, 0.66, 1.18, 0.04, 0.06)
    for i, (x, y) in enumerate([(-0.15, -0.2), (0.12, 0.1), (-0.05, 0.35)]):
        k.sph("Snow", x, y, 0.72, 0.08 + 0.02 * i, sc=(1, 1, 0.7), seg=(10, 6))   # espuma
    k.tube(k.T, [(0, 0.6, 0.62), (0, 0.62, 0.85), (0, 0.5, 0.92)], 0.03)        # grifo
    for x in (-0.3, 0.3):
        for y in (-0.5, 0.5):
            k.cyl(k.T, x, y, 0.18, 0.04, 0.2, "z", seg=8)
    k.wheels4(0.34, -0.5, 0.12, 0.08)


# ============ vehículos de calle ============
@vehicle("Taxi", "Sun", "Night", [3, 3, 3, 3], "Calle", "Taxi")
def _(k):
    k.box(k.P, 0, 0, 0.32, 0.7, 1.3, 0.3, 0.08)
    k.box(k.P, 0, 0.08, 0.57, 0.62, 0.68, 0.26, 0.08)
    k.box("Glass", 0, -0.27, 0.58, 0.52, 0.02, 0.18, 0.02)
    for x in (-0.311, 0.311):
        k.box("Glass", x, 0.08, 0.59, 0.02, 0.52, 0.15, 0.02)
    k.box(k.T, 0, 0.08, 0.74, 0.3, 0.14, 0.09, 0.03)                    # cartel del techo
    for x in (-0.351, 0.351):
        for i in range(5):
            k.box(k.T if i % 2 == 0 else k.P, x, -0.4 + i * 0.2, 0.3, 0.01, 0.2, 0.06, 0.0)
    for x in (-0.22, 0.22):
        k.sph("Light", x, -0.65, 0.34, 0.05, sc=(1, 0.5, 1), seg=(10, 6))
        k.box("Tail", x, 0.651, 0.34, 0.1, 0.02, 0.05, 0.0)
    k.wheels4(0.35, -0.42, 0.15, 0.13)


@vehicle("Bus", "Coral", "Cream", [2, 1, 3, 5], "Calle", "Autobús")
def _(k):
    k.box(k.P, 0, 0, 0.6, 0.78, 1.8, 0.85, 0.08)
    k.box(k.T, 0, 0, 0.55, 0.8, 1.82, 0.08, 0.01)
    for x in (-0.391, 0.391):
        for i in range(5):
            k.box("Glass", x, -0.65 + i * 0.32, 0.82, 0.02, 0.24, 0.2, 0.02)
    k.box("Glass", 0, -0.901, 0.8, 0.62, 0.02, 0.3, 0.02)
    k.box("Night", 0, -0.902, 1.0, 0.5, 0.02, 0.08, 0.01)
    for x in (-0.25, 0.25):
        k.sph("Light", x, -0.9, 0.35, 0.05, sc=(1, 0.5, 1), seg=(10, 6))
    k.wheels4(0.37, -0.6, 0.17, 0.14)


@vehicle("IceCream", "Bubblegum", "Cream", [2, 2, 3, 4], "Calle", "Furgoneta de helados")
def _(k):
    k.box(k.P, 0, 0.05, 0.55, 0.76, 1.35, 0.75, 0.1)
    k.box(k.P, 0, -0.68, 0.42, 0.7, 0.3, 0.45, 0.08)
    k.box("Glass", 0, -0.66, 0.72, 0.6, 0.02, 0.2, 0.02)
    k.box("Night", 0.381, 0.08, 0.68, 0.02, 0.6, 0.3, 0.02)            # ventanilla
    for i in range(6):
        k.box(k.T if i % 2 == 0 else "Bubblegum", 0.385, -0.17 + i * 0.1, 0.88, 0.04, 0.1, 0.06, 0.01)  # toldo
    k.cyl("Wood", 0, 0.1, 1.05, 0.12, 0.25, "z", r2=0.0, smooth=False)  # cucurucho
    k.sph(k.T, 0, 0.1, 1.2, 0.13, seg=(12, 6))
    k.sph("Coral", 0, 0.1, 1.31, 0.05, seg=(8, 4))
    k.wheels4(0.36, -0.45, 0.16, 0.13)


@vehicle("FireTruck", "Coral", "Chrome", [3, 2, 3, 5], "Calle", "Camión de bomberos")
def _(k):
    k.box(k.P, 0, 0.1, 0.45, 0.78, 1.7, 0.45, 0.06)
    k.box(k.P, 0, -0.58, 0.75, 0.74, 0.5, 0.35, 0.08)
    k.box("Glass", 0, -0.831, 0.78, 0.6, 0.02, 0.2, 0.02)
    k.box(k.T, 0, 0.3, 0.75, 0.6, 1.0, 0.12, 0.03)
    for i in range(7):                                                   # escalera
        k.box("Snow", 0, -0.15 + i * 0.15, 0.86, 0.5, 0.03, 0.03, 0.005)
    for x in (-0.25, 0.25):
        k.tube("Snow", [(x, -0.25, 0.86), (x, 0.8, 0.86)], 0.02)
    k.box("Sky", -0.2, -0.58, 0.96, 0.14, 0.12, 0.08, 0.02)
    k.box("Coral", 0.2, -0.58, 0.96, 0.14, 0.12, 0.08, 0.02)
    k.wheels4(0.37, -0.55, 0.17, 0.14)


@vehicle("Ambulance", "Snow", "Coral", [3, 3, 3, 4], "Calle", "Ambulancia")
def _(k):
    k.box(k.P, 0, 0.12, 0.58, 0.76, 1.15, 0.78, 0.08)
    k.box(k.P, 0, -0.6, 0.45, 0.72, 0.4, 0.5, 0.08)
    k.box("Glass", 0, -0.75, 0.72, 0.6, 0.12, 0.2, 0.02, rx=0.5)
    for x in (-0.381, 0.381):
        k.box(k.T, x, 0.12, 0.62, 0.02, 0.36, 0.12, 0.0)                 # cruz
        k.box(k.T, x, 0.12, 0.62, 0.02, 0.12, 0.36, 0.0)
        k.box(k.T, x, 0.12, 0.3, 0.02, 1.1, 0.06, 0.0)
    k.box("Sky", -0.18, -0.35, 1.0, 0.16, 0.12, 0.07, 0.02)
    k.box("Coral", 0.18, -0.35, 1.0, 0.16, 0.12, 0.07, 0.02)
    k.wheels4(0.36, -0.48, 0.16, 0.13)


@vehicle("Camper", "Mint", "Cream", [2, 2, 3, 4], "Calle", "Furgoneta camper")
def _(k):
    k.box(k.P, 0, 0, 0.42, 0.76, 1.4, 0.4, 0.12)
    k.box(k.T, 0, 0, 0.78, 0.74, 1.36, 0.36, 0.12)
    k.box("Glass", 0, -0.68, 0.78, 0.6, 0.04, 0.22, 0.04)
    for x in (-0.371, 0.371):
        for y in (-0.25, 0.15, 0.45):
            k.box("Glass", x, y, 0.8, 0.02, 0.22, 0.16, 0.03)
    k.sph("Chrome", 0, -0.705, 0.48, 0.09, sc=(1, 0.3, 1), seg=(12, 6))
    k.box(k.P, 0, 0.05, 1.0, 0.5, 0.7, 0.06, 0.03)                     # techo elevable
    k.wheels4(0.36, -0.45, 0.16, 0.13)


@vehicle("GolfCart", "Snow", "Leaf", [2, 4, 4, 2], "Calle", "Carrito de golf")
def _(k):
    k.box(k.P, 0, 0, 0.28, 0.7, 1.05, 0.2, 0.06)
    k.box("Seat", 0, 0.12, 0.45, 0.62, 0.3, 0.14, 0.05)
    k.box("Seat", 0, 0.27, 0.6, 0.62, 0.06, 0.3, 0.03)
    for x in (-0.32, 0.32):
        k.tube("Chrome", [(x, -0.3, 0.36), (x, -0.32, 0.95)], 0.02)
        k.tube("Chrome", [(x, 0.32, 0.36), (x, 0.32, 0.95)], 0.02)
    k.box(k.T, 0, 0, 0.98, 0.76, 0.78, 0.05, 0.03)
    k.box("Glass", 0, -0.32, 0.65, 0.6, 0.02, 0.4, 0.02)
    k.cyl("Wood", 0.2, 0.48, 0.55, 0.06, 0.5, "z", seg=10)             # bolsa de palos
    k.wheels4(0.32, -0.36, 0.12, 0.1)


# ============ vehículos de trabajo ============
@vehicle("Tractor", "Leaf", "Sun", [1, 2, 5, 5], "Trabajo", "Tractor")
def _(k):
    k.box(k.P, 0, -0.25, 0.5, 0.4, 0.75, 0.38, 0.06)
    k.box(k.P, 0, 0.3, 0.8, 0.6, 0.5, 0.7, 0.06)
    k.box("Glass", 0, 0.05, 0.92, 0.5, 0.02, 0.35, 0.02)
    for x in (-0.301, 0.301):
        k.box("Glass", x, 0.3, 0.92, 0.02, 0.38, 0.35, 0.02)
    k.box(k.T, 0, 0.3, 1.17, 0.66, 0.56, 0.05, 0.02)
    k.cyl("Night", 0.12, -0.45, 0.88, 0.04, 0.35, "z", seg=8)
    k.wheels4(0.28, -0.48, 0.18, 0.12, xr=0.36, yr=0.35, rr=0.35, wr=0.2)


@vehicle("Digger", "Sun", "Night", [1, 2, 4, 5], "Trabajo", "Excavadora")
def _(k):
    k.box(k.T, 0, 0.05, 0.18, 0.78, 1.25, 0.22, 0.08)                   # orugas
    for x in (-0.36, 0.36):
        k.cyl("Stone", x, -0.55, 0.18, 0.12, 0.08, "x")
        k.cyl("Stone", x, 0.55, 0.18, 0.12, 0.08, "x")
    k.box(k.P, 0, 0.1, 0.48, 0.66, 0.8, 0.38, 0.06)
    k.box(k.P, 0.12, -0.05, 0.85, 0.38, 0.4, 0.4, 0.05)
    k.box("Glass", 0.12, -0.251, 0.88, 0.3, 0.02, 0.28, 0.02)
    k.tube(k.P, [(-0.15, -0.2, 0.6), (-0.15, -0.55, 1.05), (-0.15, -0.85, 0.55)], 0.06)   # brazo
    k.box("Stone", -0.15, -0.9, 0.35, 0.3, 0.2, 0.22, 0.04)            # cazo


@vehicle("Forklift", "Tangerine", "Night", [1, 4, 4, 4], "Trabajo", "Carretilla elevadora")
def _(k):
    k.box(k.P, 0, 0.1, 0.35, 0.64, 0.85, 0.35, 0.06)
    k.box("Seat", 0, 0.2, 0.6, 0.36, 0.3, 0.18, 0.04)
    for x in (-0.28, 0.28):
        k.tube(k.T, [(x, -0.05, 0.5), (x, -0.05, 1.05)], 0.025)
        k.tube(k.T, [(x, 0.4, 0.5), (x, 0.4, 1.05)], 0.025)
        k.box(k.T, x * 0.8, -0.55, 0.12, 0.08, 0.6, 0.03, 0.01)         # horquillas
    k.box(k.T, 0, 0.18, 1.07, 0.62, 0.5, 0.04, 0.02)
    k.box(k.T, 0, -0.32, 0.55, 0.55, 0.04, 0.85, 0.02)                  # mástil
    k.box("Wood", 0, -0.55, 0.22, 0.5, 0.5, 0.14, 0.01)                 # palé
    k.wheels4(0.3, -0.25, 0.13, 0.1, yr=0.42)


@vehicle("Mower", "Coral", "Night", [2, 3, 4, 3], "Trabajo", "Cortacésped con asiento")
def _(k):
    k.box(k.P, 0, -0.15, 0.32, 0.62, 0.7, 0.25, 0.1)
    k.box(k.T, 0, 0.05, 0.16, 0.8, 0.6, 0.1, 0.04)                     # carcasa de cuchillas
    k.box("Seat", 0, 0.25, 0.5, 0.42, 0.32, 0.12, 0.05)
    k.box("Seat", 0, 0.42, 0.66, 0.42, 0.06, 0.28, 0.03)
    k.tube(k.T, [(0, -0.05, 0.45), (0, -0.12, 0.72)], 0.025)
    k.cyl(k.T, 0, -0.12, 0.72, 0.1, 0.03, "z", seg=14)
    k.box("Leaf", 0.36, 0.25, 0.45, 0.2, 0.3, 0.22, 0.04)              # bolsa de césped
    k.wheels4(0.3, -0.38, 0.11, 0.08, xr=0.33, yr=0.33, rr=0.17, wr=0.12)


# ============ motor y velocidad ============
@vehicle("Moto", "Grape", "Chrome", [5, 4, 2, 2], "Motor", "Moto de carreras")
def _(k):
    k.b.hull(k.P, [(sx * w, y, z) for sx in (-1, 1) for (w, y, z) in
                   ((0.08, -0.5, 0.35), (0.12, -0.35, 0.55), (0.16, 0.0, 0.5), (0.14, 0.3, 0.45), (0.08, 0.5, 0.45), (0.12, 0.0, 0.28))])
    k.box("Seat", 0, 0.15, 0.52, 0.2, 0.32, 0.06, 0.02)
    k.box("Glass", 0, -0.4, 0.62, 0.18, 0.12, 0.12, 0.02, rx=-0.5)
    k.tube("Chrome", [(-0.18, -0.32, 0.6), (0.18, -0.32, 0.6)], 0.018)
    k.tube("Chrome", [(0.1, 0.1, 0.3), (0.12, 0.55, 0.35)], 0.035)      # escape
    k.wheel("F", 0, -0.5, 0.2, 0.1)
    k.wheel("R", 0, 0.45, 0.2, 0.12)


@vehicle("Vespino", "Sky", "Cream", [3, 4, 3, 2], "Motor", "Moto scooter clásica")
def _(k):
    k.sph(k.P, 0, 0.25, 0.38, 0.24, sc=(0.85, 1.3, 0.8), seg=(16, 8))
    k.box(k.P, 0, -0.05, 0.22, 0.26, 0.5, 0.06, 0.03)
    k.box(k.P, 0, -0.32, 0.45, 0.28, 0.1, 0.5, 0.06)                     # escudo delantero
    k.box(k.T, 0, 0.2, 0.62, 0.22, 0.38, 0.08, 0.04)
    k.tube("Chrome", [(0, -0.33, 0.7), (0, -0.36, 0.85)], 0.025)
    k.tube("Chrome", [(-0.22, -0.36, 0.85), (0.22, -0.36, 0.85)], 0.02)
    k.sph("Light", 0, -0.39, 0.84, 0.06, sc=(1, 0.5, 1), seg=(10, 5))
    k.wheel("F", 0, -0.38, 0.13, 0.08)
    k.wheel("R", 0, 0.4, 0.13, 0.08)


@vehicle("Quad", "Tangerine", "Night", [3, 4, 4, 2], "Motor", "Quad")
def _(k):
    k.box(k.P, 0, -0.32, 0.45, 0.62, 0.35, 0.12, 0.06)
    k.box(k.P, 0, 0.35, 0.45, 0.62, 0.35, 0.12, 0.06)
    k.box(k.T, 0, 0, 0.38, 0.3, 0.75, 0.2, 0.05)
    k.box("Seat", 0, 0.1, 0.55, 0.26, 0.4, 0.1, 0.04)
    k.tube("Chrome", [(0, -0.2, 0.5), (0, -0.25, 0.7)], 0.025)
    k.tube(k.T, [(-0.28, -0.25, 0.7), (0.28, -0.25, 0.7)], 0.025)
    k.wheels4(0.34, -0.36, 0.17, 0.16)
    k.style = "Knobby"


@vehicle("Monster", "Lilac", "Sun", [3, 3, 3, 5], "Motor", "Monstruo de ruedas gigantes")
def _(k):
    k.box(k.T, 0, 0, 0.5, 0.42, 1.0, 0.12, 0.03)
    k.box(k.P, 0, 0, 0.78, 0.78, 1.15, 0.28, 0.08)
    k.box(k.P, 0, 0.1, 1.02, 0.66, 0.6, 0.25, 0.08)
    k.box("Glass", 0, -0.2, 1.03, 0.56, 0.02, 0.17, 0.02)
    for x in (-0.391, 0.391):
        k.b.prism_polygon(k.T, star2d(0.12, 0.05), 0, 0.01, loc=(x, 0.25, 0.78), rot=(0, (1 if x > 0 else -1) * math.pi / 2, 0))
    k.wheels4(0.42, -0.42, 0.32, 0.26)
    k.style = "Knobby"


@vehicle("Rocket", "Night", "Flame", [5, 3, 2, 3], "Motor", "Coche cohete")
def _(k):
    k.b.hull(k.P, [(sx * w, y, z) for sx in (-1, 1) for (w, y, z) in
                   ((0.05, -0.8, 0.22), (0.2, -0.35, 0.38), (0.3, 0.3, 0.42), (0.3, 0.55, 0.3), (0.25, 0.0, 0.15), (0.05, -0.8, 0.15))])
    k.sph("Glass", 0, -0.05, 0.46, 0.14, sc=(0.9, 1.6, 0.7), seg=(16, 8))
    k.cyl("Stone", 0, 0.6, 0.32, 0.15, 0.2, "y", seg=16)
    k.cyl(k.T, 0, 0.72, 0.32, 0.12, 0.1, "y", seg=16, r2=0.02)           # llama
    for x in (-0.3, 0.3):
        k.b.prism_polygon(k.T, [(0, 0.2), (0, 0.5), (0.25, 0.55), (0.2, 0.25)], -0.015, 0.015,
                          loc=(x, 0.05, 0.38), rot=(math.pi / 2, 0, math.pi / 2 if x > 0 else -math.pi / 2))
    k.wheels4(0.28, -0.45, 0.12, 0.1, xr=0.32, rr=0.15, wr=0.13)


@vehicle("Bumper", "Sky", "Sun", [2, 4, 3, 4], "Motor", "Coche de choque")
def _(k):
    k.cyl("Night", 0, 0, 0.16, 0.55, 0.16, "z", seg=28)                 # parachoques de goma
    k.b.hull(k.P, [(sx * w, y, z) for sx in (-1, 1) for (w, y, z) in
                   ((0.35, -0.45, 0.22), (0.42, 0.0, 0.22), (0.35, 0.42, 0.22), (0.3, -0.35, 0.55), (0.38, 0.2, 0.6), (0.3, 0.42, 0.6))])
    k.box("Seat", 0, 0.12, 0.55, 0.38, 0.28, 0.1, 0.04)
    k.tube("Chrome", [(0, -0.15, 0.5), (0, -0.2, 0.7)], 0.02)
    k.cyl(k.T, 0, -0.2, 0.7, 0.09, 0.025, "z", seg=14)
    k.tube(k.T, [(0, 0.45, 0.55), (0, 0.48, 1.3)], 0.02)                 # pértiga
    k.wheels4(0.25, -0.25, 0.08, 0.06)


# ============ nieve, agua y raíles ============
@vehicle("Sled", "Coral", "Wood", [4, 3, 1, 2], "Nieve", "Trineo")
def _(k):
    for x in (-0.25, 0.25):
        k.tube(k.T, [(x, 0.5, 0.06), (x, -0.4, 0.06), (x, -0.55, 0.16), (x, -0.5, 0.28)], 0.03)
        k.tube(k.T, [(x, -0.3, 0.06), (x, -0.3, 0.2)], 0.02)
        k.tube(k.T, [(x, 0.3, 0.06), (x, 0.3, 0.2)], 0.02)
    for i in range(5):
        k.box(k.P, 0, -0.35 + i * 0.18, 0.22, 0.62, 0.15, 0.04, 0.015)
    k.box("Snow", 0, 0.05, 0.27, 0.45, 0.4, 0.06, 0.03)


@vehicle("Snowmobile", "Sky", "Night", [4, 4, 3, 3], "Nieve", "Moto de nieve")
def _(k):
    for x in (-0.28, 0.28):
        k.box("Chrome", x, -0.45, 0.06, 0.08, 0.5, 0.03, 0.015)
    k.box(k.T, 0, 0.25, 0.15, 0.4, 0.7, 0.2, 0.06)                      # oruga
    k.b.hull(k.P, [(sx * w, y, z) for sx in (-1, 1) for (w, y, z) in
                   ((0.18, -0.6, 0.2), (0.3, -0.3, 0.3), (0.3, -0.25, 0.55), (0.25, 0.2, 0.42), (0.22, 0.6, 0.4), (0.22, 0.6, 0.25))])
    k.box("Seat", 0, 0.25, 0.48, 0.3, 0.55, 0.1, 0.04)
    k.box("Glass", 0, -0.3, 0.68, 0.38, 0.12, 0.18, 0.02, rx=-0.4)
    k.tube("Chrome", [(-0.25, -0.15, 0.65), (0.25, -0.15, 0.65)], 0.02)


@vehicle("Hover", "Tangerine", "Night", [4, 3, 1, 2], "Agua", "Aerodeslizador")
def _(k):
    k.cyl("Night", 0, 0, 0.16, 0.5, 0.22, "z", seg=28, r2=0.46)         # falda
    k.b.hull(k.P, [(sx * w, y, z) for sx in (-1, 1) for (w, y, z) in
                   ((0.3, -0.45, 0.28), (0.4, 0.0, 0.28), (0.3, 0.45, 0.28), (0.25, -0.3, 0.5), (0.3, 0.15, 0.5))])
    k.sph("Glass", 0, -0.1, 0.52, 0.18, sc=(1, 1.3, 0.6), seg=(14, 7))
    k.cyl(k.T, 0, 0.45, 0.62, 0.26, 0.08, "y", seg=20)                  # hélice carenada
    k.cyl("Stone", 0, 0.45, 0.62, 0.22, 0.09, "y", seg=20)
    k.box(k.T, 0, 0.45, 0.62, 0.04, 0.02, 0.4, 0.01)


@vehicle("Train", "Ocean", "Gold", [3, 2, 4, 5], "Raíles", "Locomotora de juguete")
def _(k):
    k.cyl(k.P, 0, -0.15, 0.45, 0.25, 0.85, "y", seg=20)                  # caldera
    k.box(k.P, 0, 0.45, 0.55, 0.66, 0.45, 0.65, 0.05)                    # cabina
    k.box(k.T, 0, 0.45, 0.9, 0.74, 0.55, 0.06, 0.02)
    k.box("Glass", 0, 0.221, 0.7, 0.4, 0.02, 0.2, 0.02)
    k.cyl("Night", 0, -0.4, 0.8, 0.08, 0.25, "z", seg=12, r2=0.12)       # chimenea
    k.cyl(k.T, 0, -0.6, 0.45, 0.2, 0.04, "y", seg=20)
    k.box(k.T, 0, -0.7, 0.18, 0.6, 0.12, 0.12, 0.02)
    k.box("Night", 0, 0.05, 0.22, 0.6, 1.3, 0.1, 0.02)
    k.wheels4(0.3, -0.4, 0.14, 0.08, yr=0.45, rr=0.2)


@vehicle("Tram", "Sun", "Snow", [3, 2, 4, 5], "Raíles", "Tranvía")
def _(k):
    k.box(k.P, 0, 0, 0.62, 0.74, 1.8, 0.8, 0.1)
    k.box(k.T, 0, 0, 0.75, 0.76, 1.82, 0.32, 0.04)
    for x in (-0.38, 0.38):
        for i in range(6):
            k.box("Glass", x, -0.72 + i * 0.29, 0.78, 0.02, 0.22, 0.2, 0.02)
    k.box("Glass", 0, -0.901, 0.8, 0.56, 0.02, 0.26, 0.02)
    k.box("Night", 0, 0, 1.05, 0.3, 0.5, 0.05, 0.01)                     # pantógrafo
    k.tube("Night", [(0, -0.15, 1.08), (0, 0.0, 1.3), (0, 0.15, 1.08)], 0.015)
    k.wheels4(0.32, -0.6, 0.13, 0.08)


@vehicle("HotDog", "Sun", "Coral", [3, 3, 3, 3], "Cotidiano", "Coche salchicha")
def _(k):
    k.sph("Wood", 0, 0, 0.42, 0.32, sc=(1.1, 2.2, 0.6), seg=(20, 10))     # pan
    k.cyl(k.T, 0, 0, 0.55, 0.13, 1.55, "y", seg=16)                       # salchicha
    k.sph(k.T, 0, -0.78, 0.55, 0.13, seg=(12, 6))
    k.sph(k.T, 0, 0.78, 0.55, 0.13, seg=(12, 6))
    for i in range(7):
        y = -0.6 + i * 0.2
        k.box(k.P, 0, y, 0.69, 0.05, 0.12, 0.02, 0.01)                    # mostaza en zigzag
    k.box("Seat", 0, 0.1, 0.62, 0.3, 0.3, 0.08, 0.03)
    k.wheels4(0.32, -0.5, 0.13, 0.1)


EXISTING = {  # coches que ya existen en gen_cars.py
    "Dart": ("Coral", "Snow", [5, 2, 3, 2], "Motor", "Coche de fórmula"),
    "Hopper": ("Sun", "Snow", [3, 5, 2, 1], "Calle", "Utilitario pequeño"),
    "Gecko": ("Mint", "Snow", [2, 4, 5, 2], "Motor", "Buggy"),
    "Rhino": ("Ocean", "Snow", [3, 1, 4, 5], "Trabajo", "Todoterreno"),
}


def build(name):
    paint, trim, stats, kind, desc, fn = VEH[name]
    k = K(name, paint, trim)
    fn(k)
    return k.finish()


def main(out, sheet=False):
    reset_scene()
    meta = {}
    obs = {}
    for name in VEH:
        ob = build(name)
        export_fbx(ob, os.path.join(out, "FBX", "Vehicles", ob.name + ".fbx"))
        paint, trim, stats, kind, desc, _ = VEH[name]
        tris = tri_count(ob) + sum(tri_count(g) for c in ob.children for g in c.children if g.type == "MESH")
        xs = [v.co.x for v in ob.data.vertices]; ys = [v.co.y for v in ob.data.vertices]; zs = [v.co.z for v in ob.data.vertices]
        meta[name] = {"name_es": desc, "category": kind, "stats": dict(zip(["speed", "acceleration", "grip", "weight"], stats)),
                      "paint": paint, "trim": trim, "wheels": len([1 for c in ob.children if c.name.startswith("WheelSocket")]),
                      "triangles": tris, "size_m": [round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(max(zs) - min(zs), 2)]}
        obs[name] = ob
    for n, (paint, trim, stats, kind, desc) in EXISTING.items():
        meta[n] = {"name_es": desc, "category": kind, "stats": dict(zip(["speed", "acceleration", "grip", "weight"], stats)),
                   "paint": paint, "trim": trim, "file": "FBX/Cars/Car_%s.fbx" % n}
    with open(os.path.join(out, "vehicles.json"), "w", encoding="utf-8") as fh:
        json.dump(meta, fh, indent=2, ensure_ascii=False)
    for i, ob in enumerate(obs.values()):
        ob.location = ((i % 8) * 2.2, -(i // 8) * 2.4, 0)
    os.makedirs(os.path.join(out, "Blend"), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, "Blend", "CT_Vehicles.blend"))
    print(json.dumps({k: (v["triangles"] if "triangles" in v else "-") for k, v in meta.items()}))
    if sheet:
        render_sheet(out, obs)


def render_sheet(out, obs):
    from PIL import Image, ImageDraw, ImageFont
    cam = setup_preview((0, 3, 2), (0, 0, 0), res=(360, 300), samples=20)
    bpy.data.objects["PreviewFloor"].data.materials[0] = material("Border")
    bpy.data.objects["PreviewFloor"].location.z = 0.25
    cam.data.lens = 50
    cells = []
    for name, ob in obs.items():
        for o in obs.values():
            hide_tree(o, o is not ob)
        size = max(1.0, VEH_SIZE(ob))
        c = Vector(ob.location) + Vector((0, 0, 0.35 * size))
        cam.location = c + Vector((2.6, -3.0, 2.0)) * size
        cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
        p = os.path.join(out, "Previews", "veh_tmp", f"{name}.png")
        render(p)
        cells.append((name, VEH[name][4], p))
    F = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
    fb = ImageFont.truetype(F, 20); fs = ImageFont.truetype(F.replace("-Bold", ""), 15)
    cw, ch, cols = 300, 290, 8
    rows = (len(cells) + cols - 1) // cols
    img = Image.new("RGB", (cw * cols + 20, ch * rows + 20), (236, 231, 222))
    d = ImageDraw.Draw(img)
    for k, (n, desc, p) in enumerate(cells):
        x, y = 10 + (k % cols) * cw, 10 + (k // cols) * ch
        im = Image.open(p).convert("RGB").resize((cw - 10, int((cw - 10) * 300 / 360)))
        img.paste(im, (x, y))
        d.text((x + 6, y + 245), n, font=fb, fill=(45, 45, 60))
        d.text((x + 6, y + 268), desc, font=fs, fill=(100, 95, 110))
    img.save(os.path.join(out, "Previews", "vehicles.png"))
    import shutil
    shutil.rmtree(os.path.join(out, "Previews", "veh_tmp"), ignore_errors=True)


def VEH_SIZE(ob):
    ys = [v.co.y for v in ob.data.vertices]; zs = [v.co.z for v in ob.data.vertices]
    return max((max(ys) - min(ys)) / 1.4, (max(zs) - min(zs)) / 1.1)


if __name__ == "__main__":
    main(sys.argv[1], "--sheet" in sys.argv)
