"""Genera coches, objetos de carrera y decorados de Cozy Track.

Uso: python gen_props.py <carpeta_salida>
"""
import sys, os, math, json, random
sys.path.insert(0, os.path.dirname(__file__))
import bpy, bmesh
from mathutils import Vector, Matrix, Euler
from common import *


class Builder:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def _tag(self, before, mat, smooth):
        idx = self.mi(mat)
        new = [f for f in self.bm.faces if f not in before]
        for f in new:
            f.material_index = idx
            f.smooth = smooth
        return new

    def _run(self, fn, mat, smooth):
        before = set(self.bm.faces)
        fn()
        return self._tag(before, mat, smooth)

    def sphere(self, mat, loc=(0, 0, 0), r=0.5, scale=(1, 1, 1), seg=(24, 12), rot=(0, 0, 0), smooth=True):
        M = Matrix.LocRotScale(Vector(loc), Euler(rot), Vector(scale))
        return self._run(lambda: bmesh.ops.create_uvsphere(self.bm, u_segments=seg[0], v_segments=seg[1],
                         radius=r, matrix=M, calc_uvs=True), mat, smooth)

    def ico(self, mat, loc=(0, 0, 0), r=0.5, sub=1, scale=(1, 1, 1), rot=(0, 0, 0), smooth=False):
        M = Matrix.LocRotScale(Vector(loc), Euler(rot), Vector(scale))
        return self._run(lambda: bmesh.ops.create_icosphere(self.bm, subdivisions=sub, radius=r,
                         matrix=M, calc_uvs=True), mat, smooth)

    def cone(self, mat, loc=(0, 0, 0), r1=0.5, r2=0.0, depth=1.0, seg=16, rot=(0, 0, 0), scale=(1, 1, 1), smooth=False):
        M = Matrix.LocRotScale(Vector(loc), Euler(rot), Vector(scale))
        return self._run(lambda: bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=seg,
                         radius1=r1, radius2=r2, depth=depth, matrix=M, calc_uvs=True), mat, smooth)

    def cube(self, mat, loc=(0, 0, 0), size=(1, 1, 1), rot=(0, 0, 0), bevel=0.0, smooth=False):
        def f():
            ret = bmesh.ops.create_cube(self.bm, size=1.0, matrix=Matrix.LocRotScale(Vector(loc), Euler(rot), Vector(size)),
                                        calc_uvs=True)
            if bevel > 0:
                edges = list({e for v in ret["verts"] for e in v.link_edges})
                bmesh.ops.bevel(self.bm, geom=edges, offset=bevel, segments=3, affect="EDGES", profile=0.5)
        return self._run(f, mat, smooth)

    def hull(self, mat, pts, smooth=False):
        def f():
            vs = [self.bm.verts.new(Vector(p)) for p in pts]
            bmesh.ops.convex_hull(self.bm, input=vs)
        faces = self._run(f, mat, smooth)
        # el casco puede dejar triángulos coplanares: se disuelven
        bmesh.ops.dissolve_limit(self.bm, angle_limit=math.radians(1), verts=list({v for fc in faces for v in fc.verts if fc.is_valid}),
                                 edges=list({e for fc in faces if fc.is_valid for e in fc.edges}))
        return faces

    def prism_polygon(self, mat, pts2d, z0, z1, loc=(0, 0, 0), rot=(0, 0, 0), smooth=False):
        """Extruye un polígono 2D (en XY) entre z0 y z1."""
        M = Matrix.LocRotScale(Vector(loc), Euler(rot), Vector((1, 1, 1)))
        def f():
            bot = [self.bm.verts.new(M @ Vector((x, y, z0))) for x, y in pts2d]
            top = [self.bm.verts.new(M @ Vector((x, y, z1))) for x, y in pts2d]
            n = len(pts2d)
            self.bm.faces.new(list(reversed(bot)))
            self.bm.faces.new(top)
            for i in range(n):
                self.bm.faces.new((bot[i], bot[(i + 1) % n], top[(i + 1) % n], top[i]))
        return self._run(f, mat, smooth)

    def text(self, mat, body, loc, rot, size=0.5, depth=0.03):
        cu = bpy.data.curves.new("t", "FONT")
        cu.body = body
        cu.size = size
        cu.extrude = depth
        cu.align_x = "CENTER"
        cu.align_y = "CENTER"
        tob = bpy.data.objects.new("t", cu)
        bpy.context.scene.collection.objects.link(tob)
        dg = bpy.context.evaluated_depsgraph_get()
        me = bpy.data.meshes.new_from_object(tob.evaluated_get(dg))
        M = Matrix.LocRotScale(Vector(loc), Euler(rot), Vector((1, 1, 1)))
        me.transform(M)
        def f():
            self.bm.from_mesh(me)
        faces = self._run(f, mat, False)
        bpy.data.objects.remove(tob)
        bpy.data.meshes.remove(me)
        return faces

    def finish(self, uv=False, center_pivot=False, ground=False):
        bm = self.bm
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.normal_update()
        ob = new_object(self.name, bm, [material(m) for m in self.mats])
        me = ob.data
        # normales hacia fuera
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.mesh.normals_make_consistent(inside=False)
        if uv:
            bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.02)
        bpy.ops.object.mode_set(mode="OBJECT")
        zs = [v.co.z for v in me.vertices]
        if ground:
            me.transform(Matrix.Translation((0, 0, -min(zs))))
        if center_pivot:
            xs = [v.co.x for v in me.vertices]; ys = [v.co.y for v in me.vertices]
            c = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, (min(zs) + max(zs)) / 2))
            me.transform(Matrix.Translation(-c))
        return ob


def star2d(r_out, r_in, n=5, rot=math.pi / 2):
    pts = []
    for i in range(2 * n):
        r = r_out if i % 2 == 0 else r_in
        a = rot + i * math.pi / n
        pts.append((r * math.cos(a), r * math.sin(a)))
    return pts


# ---------------- COCHES ----------------
CAR_INFO = {
    # forma: (color de base, agarre sugerido 0-1, peso sugerido 0-1, nota)
    "Sphere":       ("Coral",     0.45, 0.50, "Equilibrado, rueda en cualquier dirección"),
    "Cube":         ("Sky",       0.85, 0.75, "Mucho agarre, arranca lento"),
    "Capsule":      ("Mint",      0.60, 0.45, "Alargado, buena estabilidad en recta"),
    "Pyramid":      ("Sun",       0.75, 0.55, "Centro de gravedad bajo"),
    "Cylinder":     ("Tangerine", 0.55, 0.65, "Rueda gigante, punta de velocidad alta"),
    "Icosahedron":  ("Lilac",     0.50, 0.40, "Ligero y nervioso"),
    "Dodecahedron": ("Bubblegum", 0.65, 0.60, "Casi esfera, algo más de agarre"),
    "Octahedron":   ("Ocean",     0.70, 0.30, "El más ligero, sale despedido en los choques"),
}


def make_car(shape):
    col = CAR_INFO[shape][0]
    b = Builder(f"Car_{shape}")
    phi = (1 + 5 ** 0.5) / 2
    if shape == "Sphere":
        b.sphere(col, r=0.45, seg=(32, 16))
    elif shape == "Cube":
        b.cube(col, size=(0.8, 0.8, 0.8), bevel=0.1)
    elif shape == "Capsule":
        # cápsula tumbada a lo largo del avance (-Y)
        b.sphere(col, loc=(0, -0.22, 0), r=0.33, seg=(24, 12), rot=(math.pi / 2, 0, 0))
        b.sphere(col, loc=(0, 0.22, 0), r=0.33, seg=(24, 12), rot=(math.pi / 2, 0, 0))
        b.cone(col, r1=0.33, r2=0.33, depth=0.44, seg=24, rot=(math.pi / 2, 0, 0), smooth=True)
    elif shape == "Pyramid":
        h = 0.75
        b.hull(col, [(-0.45, -0.45, 0), (0.45, -0.45, 0), (0.45, 0.45, 0), (-0.45, 0.45, 0), (0, 0, h)])
    elif shape == "Cylinder":
        b.cone(col, r1=0.45, r2=0.45, depth=0.5, seg=32, rot=(0, math.pi / 2, 0), smooth=True)
        b.cone("Snow", loc=(0.255, 0, 0), r1=0.18, r2=0.18, depth=0.02, seg=24, rot=(0, math.pi / 2, 0))
        b.cone("Snow", loc=(-0.255, 0, 0), r1=0.18, r2=0.18, depth=0.02, seg=24, rot=(0, math.pi / 2, 0))
    elif shape == "Icosahedron":
        b.ico(col, r=0.48, sub=1)
    elif shape == "Dodecahedron":
        pts = []
        for x in (-1, 1):
            for y in (-1, 1):
                for z in (-1, 1):
                    pts.append((x, y, z))
        for a in (-1, 1):
            for c in (-1, 1):
                pts += [(0, a / phi, c * phi), (a / phi, c * phi, 0), (a * phi, 0, c / phi)]
        s = 0.45 / math.sqrt(3)
        b.hull(col, [(x * s, y * s, z * s) for x, y, z in pts])
    elif shape == "Octahedron":
        r = 0.5
        b.hull(col, [(r, 0, 0), (-r, 0, 0), (0, r, 0), (0, -r, 0), (0, 0, r), (0, 0, -r)])
    ob = b.finish(uv=True, center_pivot=True)
    me = ob.data
    zs = [v.co.z for v in me.vertices]
    return ob, {"base_color": col, "suggested_grip": CAR_INFO[shape][1], "suggested_weight": CAR_INFO[shape][2],
                "note": CAR_INFO[shape][3], "center_height_m": round(-min(zs), 3)}


# ---------------- OBJETOS ----------------
def make_items():
    out = []
    b = Builder("Item_Box")
    b.cube("Sun", size=(0.8, 0.8, 0.8), bevel=0.08)
    for i in range(4):
        a = i * math.pi / 2
        n = Vector((math.sin(a), -math.cos(a), 0))
        b.text("Coral", "?", loc=n * 0.405, rot=(math.pi / 2, 0, a), size=0.55, depth=0.02)
    out.append(b.finish(center_pivot=True))

    b = Builder("Item_Coin")
    b.cone("Gold", r1=0.3, r2=0.3, depth=0.08, seg=32, rot=(math.pi / 2, 0, 0), smooth=False)
    for s in (-1, 1):
        b.prism_polygon("Sun", star2d(0.17, 0.075), 0, 0.02, loc=(0, s * 0.04, 0), rot=(s * -math.pi / 2, 0, 0))
    out.append(b.finish(center_pivot=True))

    b = Builder("Item_Shield")
    b.ico("Shield", r=0.75, sub=3, smooth=True)
    out.append(b.finish(center_pivot=True))
    bpy.data.materials["CT_Shield"].node_tree.nodes["Principled BSDF"].inputs["Alpha"].default_value = 0.35

    b = Builder("Item_Projectile")
    b.sphere("Coral", r=0.22, seg=(16, 8))
    dirs = [Vector(v).normalized() for v in
            [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1),
             (1, 1, 1), (-1, 1, 1), (1, -1, 1), (-1, -1, 1), (1, 1, -1), (-1, 1, -1), (1, -1, -1), (-1, -1, -1)]]
    for d in dirs:
        q = d.to_track_quat("Z", "Y")
        b.cone("Sun", loc=d * 0.27, r1=0.07, r2=0.0, depth=0.16, seg=8, rot=q.to_euler())
    out.append(b.finish(center_pivot=True))

    b = Builder("Item_Magnet")
    # imán de herradura: medio toro + dos puntas
    segs = 16
    for i in range(segs):
        a0 = math.pi * i / segs
        a = math.pi * (i + 0.5) / segs
        b.cube("Coral", loc=(0.28 * math.cos(a), 0, 0.28 * math.sin(a)), size=(0.12, 0.16, 0.06),
               rot=(0, -a + math.pi / 2, 0))
    for sx in (-1, 1):
        b.cube("Coral", loc=(sx * 0.28, 0, -0.12), size=(0.14, 0.16, 0.24))
        b.cube("Snow", loc=(sx * 0.28, 0, -0.30), size=(0.14, 0.16, 0.12))
    out.append(b.finish(center_pivot=True))

    b = Builder("Item_BoostPad")
    b.cube("Tangerine", loc=(0, 0, 0.01), size=(1.0, 1.6, 0.02))
    for k in range(3):
        y = 0.45 - k * 0.45
        pts = [(-0.35, y + 0.15), (0, y - 0.15), (0.35, y + 0.15), (0.35, y + 0.0), (0, y - 0.3), (-0.35, y + 0.0)]
        pts = [(x, yy) for x, yy in pts]
        b.prism_polygon("Sun", pts, 0.02, 0.035)
    ob = b.finish(ground=True)
    out.append(ob)
    return out


# ---------------- DECORADOS ----------------
def rng_blob(b, mat, loc, r, seed, sub=2, amt=0.15, scale=(1, 1, 1), smooth=False):
    random.seed(seed)
    faces = b.ico(mat, loc=loc, r=r, sub=sub, scale=scale, smooth=smooth)
    verts = {v for f in faces for v in f.verts}
    c = Vector(loc)
    cache = {}
    for v in verts:
        key = tuple(round(x, 4) for x in v.co)
        if key not in cache:
            cache[key] = 1 + random.uniform(-amt, amt)
        v.co = c + (v.co - c) * cache[key]


def make_scenery():
    S = {}
    # Sala de juegos
    b = Builder("Prop_Playroom_ToyBlock")
    b.cube("Ocean", loc=(0, 0, 0.5), size=(1, 1, 1), bevel=0.08)
    for i, (col, a) in enumerate([("Sun", 0), ("Coral", math.pi / 2), ("Mint", math.pi), ("Lilac", -math.pi / 2)]):
        n = Vector((math.sin(a), -math.cos(a), 0))
        b.prism_polygon(col, star2d(0.3, 0.13), 0, 0.03, loc=n * 0.5 + Vector((0, 0, 0.5)), rot=(math.pi / 2, 0, a))
    S["Playroom"] = [b.finish(ground=True)]
    b = Builder("Prop_Playroom_Pencil")
    hexp = [(0.18 * math.cos(math.pi / 3 * i), 0.18 * math.sin(math.pi / 3 * i)) for i in range(6)]
    b.prism_polygon("Sun", hexp, 0, 2.4, rot=(0, math.pi / 2, 0))
    b.cone("Base", loc=(2.55, 0, 0), r1=0.18, r2=0.04, depth=0.3, seg=6, rot=(0, math.pi / 2, 0))
    b.cone("Night", loc=(2.73, 0, 0), r1=0.04, r2=0.0, depth=0.06, seg=6, rot=(0, math.pi / 2, 0))
    b.cone("Stone", loc=(-0.08, 0, 0), r1=0.185, r2=0.185, depth=0.16, seg=12, rot=(0, math.pi / 2, 0))
    b.cone("Bubblegum", loc=(-0.26, 0, 0), r1=0.18, r2=0.18, depth=0.2, seg=12, rot=(0, math.pi / 2, 0), smooth=True)
    S["Playroom"].append(b.finish(ground=True, center_pivot=False))
    b = Builder("Prop_Playroom_Dice")
    b.cube("Snow", loc=(0, 0, 0.4), size=(0.8, 0.8, 0.8), bevel=0.1)
    pips = {(0, 0, 1): [(0, 0)], (0, 0, -1): [(-1, -1), (1, 1), (-1, 1), (1, -1), (0, 0), (0, 0)],
            (1, 0, 0): [(-1, -1), (1, 1)], (-1, 0, 0): [(-1, -1), (0, 0), (1, 1)],
            (0, 1, 0): [(-1, -1), (1, 1), (-1, 1), (1, -1)], (0, -1, 0): [(-1, -1), (1, 1), (-1, 1), (1, -1), (0, 0)]}
    for n, ps in pips.items():
        n = Vector(n)
        u = Vector((0, 1, 0)) if abs(n.z) > 0 else Vector((0, 0, 1))
        v = n.cross(u)
        for (pu, pv) in set(ps):
            p = Vector((0, 0, 0.4)) + n * 0.39 + u * pu * 0.2 + v * pv * 0.2
            b.sphere("Night", loc=p, r=0.07, seg=(10, 6))
    S["Playroom"].append(b.finish(ground=True))
    b = Builder("Prop_Playroom_BookStack")
    z = 0
    for i, (col, w, h, rz) in enumerate([("Coral", 1.6, 0.3, 0.05), ("Mint", 1.4, 0.25, -0.12), ("Lilac", 1.5, 0.35, 0.2)]):
        b.cube(col, loc=(0, 0, z + h / 2), size=(w, w * 0.7, h), rot=(0, 0, rz), bevel=0.02)
        b.cube("Snow", loc=(0.03, 0, z + h / 2), size=(w - 0.04, w * 0.7 + 0.01, h - 0.06), rot=(0, 0, rz))
        z += h
    S["Playroom"].append(b.finish(ground=True))

    # Jardín
    b = Builder("Prop_Garden_Tree")
    b.cone("Bark", loc=(0, 0, 0.6), r1=0.18, r2=0.13, depth=1.2, seg=8)
    rng_blob(b, "Leaf", (0, 0, 1.6), 0.75, 1, sub=2, amt=0.08)
    rng_blob(b, "Leaf", (0.45, 0.15, 1.3), 0.5, 2, sub=2, amt=0.08)
    rng_blob(b, "LeafDark", (-0.4, -0.2, 1.35), 0.5, 3, sub=2, amt=0.08)
    S["Garden"] = [b.finish(ground=True)]
    b = Builder("Prop_Garden_Bush")
    for i, (x, y, r) in enumerate([(0, 0, 0.5), (0.45, 0.1, 0.38), (-0.42, 0.05, 0.36)]):
        rng_blob(b, "Leaf" if i == 0 else "LeafDark", (x, y, r * 0.8), r, 10 + i, sub=2, amt=0.08)
    S["Garden"].append(b.finish(ground=True))
    b = Builder("Prop_Garden_Flower")
    b.cone("LeafDark", loc=(0, 0, 0.45), r1=0.04, r2=0.04, depth=0.9, seg=6)
    for i in range(6):
        a = i * math.pi / 3
        b.sphere("Bubblegum", loc=(0.17 * math.cos(a), 0.17 * math.sin(a), 0.95), r=0.12, scale=(1.2, 0.8, 0.35),
                 rot=(0, 0, a), seg=(12, 6))
    b.sphere("Sun", loc=(0, 0, 0.97), r=0.1, seg=(12, 6))
    b.sphere("Leaf", loc=(0.12, 0, 0.4), r=0.1, scale=(1.6, 0.6, 0.25), rot=(0, -0.5, 0), seg=(10, 6))
    S["Garden"].append(b.finish(ground=True))
    b = Builder("Prop_Garden_Rock")
    rng_blob(b, "Stone", (0, 0, 0.3), 0.5, 20, sub=1, amt=0.18, scale=(1.2, 1, 0.7))
    S["Garden"].append(b.finish(ground=True))

    # Nieve
    b = Builder("Prop_Snow_Pine")
    b.cone("Bark", loc=(0, 0, 0.2), r1=0.12, r2=0.12, depth=0.4, seg=8)
    for i, (z, r) in enumerate([(0.75, 0.75), (1.25, 0.58), (1.7, 0.4)]):
        b.cone("LeafDark", loc=(0, 0, z), r1=r, r2=0.0, depth=0.8, seg=10)
        b.cone("Snow", loc=(0, 0, z + 0.32), r1=r * 0.32, r2=0.0, depth=0.18, seg=10)
    S["Snow"] = [b.finish(ground=True)]
    b = Builder("Prop_Snow_Snowman")
    for z, r in [(0.4, 0.45), (1.0, 0.33), (1.45, 0.24)]:
        b.sphere("Snow", loc=(0, 0, z), r=r, seg=(20, 10))
    b.cone("Tangerine", loc=(0, -0.32, 1.45), r1=0.05, r2=0.0, depth=0.25, seg=8, rot=(math.pi / 2, 0, 0))
    for sx in (-1, 1):
        b.sphere("Night", loc=(sx * 0.09, -0.21, 1.53), r=0.035, seg=(8, 4))
    for z in (0.95, 1.08):
        b.sphere("Night", loc=(0, -0.32, z), r=0.035, seg=(8, 4))
    b.cone("Coral", loc=(0, 0, 1.25), r1=0.3, r2=0.28, depth=0.1, seg=16)
    S["Snow"].append(b.finish(ground=True))
    b = Builder("Prop_Snow_Pile")
    for i, (x, y, r) in enumerate([(0, 0, 0.6), (0.6, 0.2, 0.4), (-0.5, -0.1, 0.45)]):
        b.sphere("Snow", loc=(x, y, 0), r=r, scale=(1, 1, 0.55), seg=(16, 8))
    S["Snow"].append(b.finish(ground=True))
    b = Builder("Prop_Snow_IceCrystal")
    for i, (rx, rz, h) in enumerate([(0, 0, 1.4), (0.35, 0.8, 1.0), (-0.3, 2.2, 0.9), (0.25, 4.0, 0.8)]):
        r = 0.18
        pts = [(r, 0, 0), (-r, 0, 0), (0, r, 0), (0, -r, 0), (0, 0, h / 2), (0, 0, -h / 2)]
        M = Matrix.LocRotScale(Vector((0, 0, h / 2 * 0.9)), Euler((rx, 0, rz)), Vector((1, 1, 1)))
        b.hull("Ice", [M @ Vector(p) for p in pts])
    S["Snow"].append(b.finish(ground=True))

    # Espacio
    b = Builder("Prop_Space_Planet")
    b.sphere("Lilac", loc=(0, 0, 0.8), r=0.7, seg=(28, 14))
    b.cone("Sun", loc=(0, 0, 0.8), r1=1.15, r2=1.15, depth=0.04, seg=40, rot=(0.35, 0.2, 0))
    S["Space"] = [b.finish(ground=True)]
    b = Builder("Prop_Space_Rocket")
    b.cone("Snow", loc=(0, 0, 0.9), r1=0.3, r2=0.3, depth=1.2, seg=16, smooth=True)
    b.cone("Coral", loc=(0, 0, 1.75), r1=0.3, r2=0.0, depth=0.5, seg=16, smooth=True)
    b.cone("Sky", loc=(0, -0.29, 1.1), r1=0.11, r2=0.11, depth=0.04, seg=16, rot=(math.pi / 2, 0, 0))
    for i in range(3):
        a = i * 2 * math.pi / 3
        b.prism_polygon("Coral", [(0, 0.3), (0, 0.85), (0.4, 0.3), (0.4, 0.0)], -0.03, 0.03,
                        loc=(0.25 * math.cos(a), 0.25 * math.sin(a), 0), rot=(math.pi / 2, 0, a))
    b.cone("Tangerine", loc=(0, 0, 0.22), r1=0.18, r2=0.24, depth=0.15, seg=12)
    S["Space"].append(b.finish(ground=True))
    b = Builder("Prop_Space_Star")
    b.prism_polygon("Sun", star2d(0.6, 0.27), -0.1, 0.1, loc=(0, 0, 0.6), rot=(math.pi / 2, 0, 0))
    S["Space"].append(b.finish(ground=True))
    b = Builder("Prop_Space_Asteroid")
    rng_blob(b, "Grape", (0, 0, 0.5), 0.55, 40, sub=1, amt=0.22)
    for i, (p, r) in enumerate([((0.25, -0.35, 0.65), 0.12), ((-0.2, -0.38, 0.35), 0.09)]):
        b.sphere("Night", loc=p, r=r, scale=(1, 0.4, 1), seg=(10, 5))
    S["Space"].append(b.finish(ground=True))

    # Comunes
    b = Builder("Prop_Common_TrafficCone")
    b.cube("Tangerine", loc=(0, 0, 0.03), size=(0.6, 0.6, 0.06), bevel=0.02)
    b.cone("Tangerine", loc=(0, 0, 0.4), r1=0.22, r2=0.05, depth=0.7, seg=16, smooth=True)
    b.cone("Snow", loc=(0, 0, 0.42), r1=0.165, r2=0.135, depth=0.12, seg=16, scale=(1.03, 1.03, 1), smooth=True)
    S["Common"] = [b.finish(ground=True)]
    b = Builder("Prop_Common_Flag")
    b.cone("Stone", loc=(0, 0, 0.9), r1=0.04, r2=0.04, depth=1.8, seg=8)
    b.sphere("Sun", loc=(0, 0, 1.82), r=0.07, seg=(10, 5))
    for r in range(3):
        for c in range(4):
            b.cube("CheckBlack" if (r + c) % 2 == 0 else "CheckWhite",
                   loc=(0.05 + 0.15 * c + 0.075, 0, 1.7 - 0.15 * r - 0.075), size=(0.15, 0.02, 0.15))
    S["Common"].append(b.finish(ground=True))
    return S


def main(out):
    reset_scene()
    meta = {"cars": {}, "items": {}, "scenery": {}}

    cars = []
    for shape in []:  # las formas geométricas se sustituyeron por los coches de gen_cars.py
        ob, info = make_car(shape)
        export_fbx(ob, os.path.join(out, "FBX", "Cars", ob.name + ".fbx"))
        info["triangles"] = tri_count(ob)
        meta["cars"][ob.name] = info
        cars.append(ob)
    items = make_items()
    for ob in items:
        export_fbx(ob, os.path.join(out, "FBX", "Items", ob.name + ".fbx"))
        meta["items"][ob.name] = {"triangles": tri_count(ob)}
    scen = make_scenery()
    for theme, obs in scen.items():
        for ob in obs:
            export_fbx(ob, os.path.join(out, "FBX", "Scenery", theme, ob.name + ".fbx"))
            meta["scenery"][ob.name] = {"theme": theme, "triangles": tri_count(ob)}

    # disposición para la vista previa
    for i, ob in enumerate(cars):
        ob.location = (i * 1.4, 0, meta["cars"][ob.name]["center_height_m"])
    for i, ob in enumerate(items):
        ob.location = (i * 2.0 + 0.5, -2.2, 0.0 if ob.name == "Item_BoostPad" else 0.55)
    row = -5.0
    for theme, obs in scen.items():
        x = 0
        for ob in obs:
            ob.location = (x + (1.2 if ob.name == "Prop_Playroom_Pencil" else 0), row, 0)
            x += 3.6 if ob.name == "Prop_Playroom_Pencil" else 2.8
        row -= 3.2
    os.makedirs(os.path.join(out, "Blend"), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, "Blend", "CT_Props.blend"))
    with open(os.path.join(out, "props.json"), "w", encoding="utf-8") as fh:
        json.dump(meta, fh, indent=2, ensure_ascii=False)
    setup_preview((7, -30, 16), (7, -8.0, 0.3), ortho_scale=24)
    render(os.path.join(out, "Previews", "props.png"))


if __name__ == "__main__":
    main(sys.argv[1])
