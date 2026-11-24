"""Los 4 coches de juguete de Cozy Track, cada uno destacando en 1-2 stats.

Uso: python gen_cars.py <carpeta_salida>

- Avance hacia -Y de Blender (+Z en Unity). Pivote del chasis a ras de pista, en el centro.
- Cada rueda es un objeto hijo (Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR) con el pivote en su eje,
  para poder girarlas en Unity.
"""
import sys, os, math, json
sys.path.insert(0, os.path.dirname(__file__))
import bpy
from mathutils import Vector, Euler
from common import *
import common
from gen_props import Builder

common.PALETTE.update({
    "Tyre":  (0.17, 0.17, 0.21),
    "Glass": (0.62, 0.86, 1.00),
    "Hub":   (0.92, 0.92, 0.95),
    "Light": (1.00, 0.93, 0.60),
    "Tail":  (0.95, 0.25, 0.30),
    "Decal": (0.98, 0.98, 0.98),
})

CARS = {
    # nombre: color de pintura, stats (velocidad, aceleración, agarre, peso) de 1 a 5, papel, color de detalles
    "Dart":   ("Coral", (5, 2, 3, 2), "Velocidad punta", "Snow"),
    "Hopper": ("Sun",   (3, 5, 2, 1), "Aceleración", "Snow"),
    "Gecko":  ("Mint",  (2, 4, 5, 2), "Agarre y aceleración", "Snow"),
    "Rhino":  ("Ocean", (3, 1, 4, 5), "Peso y agarre", "Snow"),
}

UV_TILE_M = 0.6   # un repetido de textura de skin = 0,6 m en los cuatro coches


def livery(name):
    """Registra los materiales de pintura y detalles del coche (CT_Paint_<Coche>, CT_Trim_<Coche>)."""
    paint, trim = CARS[name][0], CARS[name][3]
    common.PALETTE[f"Paint_{name}"] = common.PALETTE[paint]
    common.PALETTE[f"Trim_{name}"] = common.PALETTE[trim]
    return f"Paint_{name}", f"Trim_{name}"


def box_uv(ob, tile=UV_TILE_M):
    """UV por proyección de caja a escala del mundo: misma densidad de textura en todos los coches."""
    import bmesh
    me = ob.data
    bm = bmesh.new(); bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        sgn = 1 if n[ax] >= 0 else -1
        for l in f.loops:
            c = l.vert.co
            if ax == 0:
                u, v = -sgn * c.y, c.z
            elif ax == 1:
                u, v = sgn * c.x, c.z
            else:
                u, v = c.x, -sgn * c.y
            l[uv].uv = (u / tile, v / tile)
    bm.to_mesh(me); bm.free()


def tube(b, mat, path, r, sides=10):
    """Tubo redondo a lo largo de una polilínea (para barras y arcos)."""
    import bmesh as _bm
    def f():
        P = [Vector(p) for p in path]
        rings = []
        for i, p in enumerate(P):
            t = (P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]).normalized()
            n = t.cross(Vector((0, 1, 0)))
            if n.length < 1e-4:
                n = t.cross(Vector((1, 0, 0)))
            n.normalize()
            bn = t.cross(n).normalized()
            rings.append([b.bm.verts.new(p + (n * math.cos(2 * math.pi * k / sides) + bn * math.sin(2 * math.pi * k / sides)) * r)
                          for k in range(sides)])
        for i in range(len(rings) - 1):
            for k in range(sides):
                b.bm.faces.new((rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k]))
        b.bm.faces.new(list(reversed(rings[0])))
        b.bm.faces.new(rings[-1])
    return b._run(f, mat, True)


def attach_wheels(body, specs, style="Classic"):
    """specs: lista de (nombre, x, y, radio, ancho).

    Cada rueda es un vacío WheelSocket_XX escalado a (ancho, radio, radio) con un hijo Wheel_XX:
    la malla de rueda de tamaño unidad (ver gen_wheels.py). Para cambiar el diseño basta con
    cambiar la malla del hijo; para que gire, se rota el hijo en su eje X local.
    """
    import gen_wheels
    unit = gen_wheels.build(style, name=f"WheelMesh_{body.name}")
    for (n, x, y, r, w) in specs:
        sock = add_empty("WheelSocket_" + n.split("_")[1], body, (x, y, r), 0.0, 0.3)
        sock.scale = (w, r, r)
        wo = unit.copy()          # comparte la malla
        wo.name = f"{n}__{body.name}"
        wo["base_name"] = n
        bpy.context.scene.collection.objects.link(wo)
        wo.parent = sock
        wo.location = (0, 0, 0)
    bpy.data.objects.remove(unit)


def side_number(b, num, x, y, z, r=0.11, face=1):
    """Disco blanco con número en el lateral (face=1 mira a +X, -1 a -X)."""
    rot = (0, math.pi / 2, 0)
    b.cone("Decal", loc=(x + face * 0.006, y, z), r1=r, r2=r, depth=0.012, seg=24, rot=rot)
    b.text("Night", str(num), loc=(x + face * 0.014, y, z),
           rot=(math.pi / 2, 0, face * math.pi / 2), size=r * 1.4, depth=0.004)


def top_number(b, num, x, y, z, r=0.11):
    b.cone("Decal", loc=(x, y, z + 0.006), r1=r, r2=r, depth=0.012, seg=24)
    b.text("Night", str(num), loc=(x, y, z + 0.014), rot=(0, 0, math.pi), size=r * 1.4, depth=0.004)


def guide_blade(b, y):
    # sin guía visible: se veía como un palo suelto bajo el morro
    return


def make_dart():
    col, trim = livery("Dart")
    b = Builder("Car_Dart")
    W, z0 = 0.30, 0.10
    pts = []
    for sx in (-1, 1):
        pts += [(sx * 0.10, -0.74, z0 + 0.03), (sx * 0.10, -0.74, z0 + 0.10),
                (sx * 0.26, -0.30, z0), (sx * 0.26, -0.30, z0 + 0.17),
                (sx * W, 0.30, z0), (sx * W, 0.30, z0 + 0.26),
                (sx * 0.27, 0.58, z0 + 0.02), (sx * 0.27, 0.58, z0 + 0.24)]
    b.hull(col, pts)
    # carenado lateral
    for sx in (-1, 1):
        b.cube(col, loc=(sx * 0.31, 0.12, z0 + 0.09), size=(0.08, 0.5, 0.14), bevel=0.03)
    # cabina
    b.sphere("Glass", loc=(0, 0.02, z0 + 0.25), r=0.13, scale=(0.95, 1.7, 0.75), seg=(20, 10))
    # franja central
    b.hull(trim, [(sx * 0.045, y, z + 0.003) for sx in (-1, 1) for (y, z) in
                    ((-0.74, z0 + 0.10), (-0.30, z0 + 0.17), (-0.13, z0 + 0.205))] +
           [(sx * 0.045, y, z - 0.012) for sx in (-1, 1) for (y, z) in ((-0.74, z0 + 0.10), (-0.13, z0 + 0.205))])
    # alerones
    b.cube(trim, loc=(0, -0.70, z0 + 0.02), size=(0.62, 0.12, 0.025), bevel=0.008)
    for sx in (-1, 1):
        b.cube("Snow", loc=(sx * 0.22, 0.58, z0 + 0.34), size=(0.03, 0.06, 0.16))
    b.cube(col, loc=(0, 0.60, z0 + 0.43), size=(0.72, 0.16, 0.035), bevel=0.01)
    for sx in (-1, 1):
        b.cube(trim, loc=(sx * 0.37, 0.60, z0 + 0.41), size=(0.02, 0.2, 0.1))
    top_number(b, 5, 0, 0.36, z0 + 0.255, r=0.085)
    b.cube("Tail", loc=(0, 0.60, z0 + 0.12), size=(0.3, 0.02, 0.05))
    guide_blade(b, -0.6)
    ob = b.finish()
    box_uv(ob)
    attach_wheels(ob, [("Wheel_FL", -0.35, -0.42, 0.13, 0.12), ("Wheel_FR", 0.35, -0.42, 0.13, 0.12),
                       ("Wheel_RL", -0.40, 0.36, 0.17, 0.16), ("Wheel_RR", 0.40, 0.36, 0.17, 0.16)])
    return ob


def make_hopper():
    col, trim = livery("Hopper")
    b = Builder("Car_Hopper")
    b.cube(col, loc=(0, 0, 0.32), size=(0.64, 0.98, 0.30), bevel=0.09)
    b.cube(col, loc=(0, 0.10, 0.55), size=(0.56, 0.58, 0.28), bevel=0.09)
    # ventanas
    b.cube("Glass", loc=(0, -0.191, 0.57), size=(0.44, 0.02, 0.17), bevel=0.02)
    b.cube("Glass", loc=(0, 0.391, 0.57), size=(0.42, 0.02, 0.16), bevel=0.02)
    for sx in (-1, 1):
        b.cube("Glass", loc=(sx * 0.281, 0.10, 0.57), size=(0.02, 0.44, 0.16), bevel=0.02)
    # parachoques y faros
    b.cube("Hub", loc=(0, -0.49, 0.22), size=(0.6, 0.06, 0.07), bevel=0.02)
    b.cube("Hub", loc=(0, 0.49, 0.22), size=(0.6, 0.06, 0.07), bevel=0.02)
    for sx in (-1, 1):
        b.sphere("Light", loc=(sx * 0.2, -0.47, 0.36), r=0.06, scale=(1, 0.5, 1), seg=(14, 7))
        b.cube("Tail", loc=(sx * 0.22, 0.491, 0.36), size=(0.1, 0.02, 0.06))
        b.cone("Hub", loc=(sx * 0.12, 0.52, 0.17), r1=0.03, r2=0.03, depth=0.1, seg=10, rot=(math.pi / 2, 0, 0))
    # techo con número y raya
    top_number(b, 7, 0, 0.12, 0.69, r=0.11)
    for sx in (-1, 1):
        b.cube(trim, loc=(sx * 0.12, -0.25, 0.47), size=(0.06, 0.48, 0.01))
    guide_blade(b, -0.40)
    ob = b.finish()
    box_uv(ob)
    attach_wheels(ob, [("Wheel_FL", -0.34, -0.30, 0.15, 0.14), ("Wheel_FR", 0.34, -0.30, 0.15, 0.14),
                       ("Wheel_RL", -0.35, 0.30, 0.19, 0.17), ("Wheel_RR", 0.35, 0.30, 0.19, 0.17)])
    return ob


def make_gecko():
    col, trim = livery("Gecko")
    b = Builder("Car_Gecko")
    # chasis plano y morro
    b.cube(col, loc=(0, 0.02, 0.22), size=(0.52, 1.02, 0.10), bevel=0.04)
    b.hull(col, [(sx * w, y, z) for sx in (-1, 1) for (w, y, z) in
                 ((0.26, -0.30, 0.17), (0.26, -0.30, 0.34), (0.16, -0.60, 0.18), (0.16, -0.60, 0.26))])
    # pontones laterales
    for sx in (-1, 1):
        b.cube(col, loc=(sx * 0.30, 0.05, 0.27), size=(0.12, 0.42, 0.12), bevel=0.04)
    # asiento
    b.cube("Night", loc=(0, 0.12, 0.31), size=(0.28, 0.26, 0.08), bevel=0.03)
    b.cube("Night", loc=(0, 0.25, 0.44), size=(0.28, 0.06, 0.26), rot=(-0.2, 0, 0), bevel=0.03)
    # volante
    b.cone("Night", loc=(0, -0.12, 0.42), r1=0.07, r2=0.07, depth=0.02, seg=14, rot=(1.1, 0, 0))
    # arco antivuelco
    R = 0.22
    path = [(R, 0.33, 0.24)] + [(R * math.cos(math.pi * i / 12), 0.33, 0.40 + R * math.sin(math.pi * i / 12))
                                for i in range(13)] + [(-R, 0.33, 0.24)]
    tube(b, trim, path, 0.026)
    # parachoques delantero tubular
    b.cube(trim, loc=(0, -0.66, 0.20), size=(0.5, 0.05, 0.05), bevel=0.02)
    b.cube("Night", loc=(0, 0.52, 0.27), size=(0.3, 0.06, 0.12), bevel=0.02)   # motor
    for sx in (-1, 1):
        b.cone("Hub", loc=(sx * 0.08, 0.57, 0.33), r1=0.025, r2=0.025, depth=0.08, seg=10, rot=(math.pi / 2.4, 0, 0))
    top_number(b, 3, 0, -0.42, 0.27, r=0.075)
    guide_blade(b, -0.52)
    ob = b.finish()
    box_uv(ob)
    attach_wheels(ob, [("Wheel_FL", -0.40, -0.36, 0.17, 0.22), ("Wheel_FR", 0.40, -0.36, 0.17, 0.22),
                       ("Wheel_RL", -0.42, 0.38, 0.19, 0.26), ("Wheel_RR", 0.42, 0.38, 0.19, 0.26)])
    return ob


def make_rhino():
    col, trim = livery("Rhino")
    b = Builder("Car_Rhino")
    b.cube(col, loc=(0, 0, 0.40), size=(0.72, 1.24, 0.30), bevel=0.06)
    # cabina
    b.cube(col, loc=(0, -0.22, 0.73), size=(0.70, 0.52, 0.40), bevel=0.07)
    b.cube("Glass", loc=(0, -0.481, 0.76), size=(0.56, 0.02, 0.2), bevel=0.02)
    for sx in (-1, 1):
        b.cube("Glass", loc=(sx * 0.351, -0.24, 0.79), size=(0.02, 0.34, 0.16), bevel=0.02)
        side_number(b, 1, sx * 0.36, 0.25, 0.44, r=0.09, face=sx)
        b.cube(trim, loc=(sx * 0.362, -0.02, 0.33), size=(0.01, 1.12, 0.05))
    # caja trasera abierta
    for sx in (-1, 1):
        b.cube(col, loc=(sx * 0.33, 0.33, 0.63), size=(0.06, 0.56, 0.18), bevel=0.02)
    b.cube(col, loc=(0, 0.59, 0.63), size=(0.72, 0.06, 0.18), bevel=0.02)
    b.cube("Night", loc=(0, 0.33, 0.56), size=(0.6, 0.52, 0.02))
    # barras de techo y faros
    b.cube("Night", loc=(0, -0.22, 0.95), size=(0.6, 0.12, 0.04), bevel=0.01)
    for sx in (-1, -0.33, 0.33, 1):
        b.cube("Light", loc=(sx * 0.2, -0.26, 0.98), size=(0.08, 0.05, 0.05), bevel=0.01)
    # defensa frontal
    b.cube("Hub", loc=(0, -0.66, 0.36), size=(0.76, 0.08, 0.10), bevel=0.03)
    for sx in (-1, 1):
        b.cube("Hub", loc=(sx * 0.2, -0.66, 0.46), size=(0.06, 0.08, 0.16), bevel=0.02)
        b.cube("Light", loc=(sx * 0.25, -0.621, 0.47), size=(0.12, 0.02, 0.07), bevel=0.01)
        b.cube("Tail", loc=(sx * 0.27, 0.621, 0.44), size=(0.1, 0.02, 0.07))
    b.cube("Hub", loc=(0, 0.66, 0.30), size=(0.6, 0.06, 0.08), bevel=0.02)
    guide_blade(b, -0.55)
    ob = b.finish()
    box_uv(ob)
    attach_wheels(ob, [("Wheel_FL", -0.40, -0.38, 0.21, 0.2), ("Wheel_FR", 0.40, -0.38, 0.21, 0.2),
                       ("Wheel_RL", -0.40, 0.38, 0.21, 0.2), ("Wheel_RR", 0.40, 0.38, 0.21, 0.2)])
    return ob


BUILDERS = {"Dart": make_dart, "Hopper": make_hopper, "Gecko": make_gecko, "Rhino": make_rhino}


def stat_sheet(out, renders):
    from PIL import Image, ImageDraw, ImageFont
    F = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
    f_name = ImageFont.truetype(F, 46)
    f_role = ImageFont.truetype(F.replace("-Bold", ""), 24)
    f_stat = ImageFont.truetype(F, 22)
    cw, ch = 520, 860
    sheet = Image.new("RGB", (cw * 4 + 50, ch + 40), (236, 231, 222))
    d = ImageDraw.Draw(sheet)
    labels = ["Velocidad", "Aceleración", "Agarre", "Peso"]
    for k, name in enumerate(CARS):
        x0 = 25 + k * cw
        col = tuple(int(c * 255) for c in PALETTE[CARS[name][0]])
        d.rounded_rectangle([x0 + 10, 20, x0 + cw - 10, ch + 20], radius=28, fill=(250, 247, 241))
        img = Image.open(renders[name]).convert("RGB").resize((cw - 40, cw - 40))
        sheet.paste(img, (x0 + 20, 30))
        y = cw - 10
        d.text((x0 + 36, y), name, font=f_name, fill=(40, 40, 55))
        dark = tuple(int(c * 0.72) for c in col)
        d.text((x0 + 36, y + 58), CARS[name][2], font=f_role, fill=dark)
        for i, (lab, v) in enumerate(zip(labels, CARS[name][1])):
            yy = y + 110 + i * 52
            d.text((x0 + 36, yy), lab, font=f_stat, fill=(70, 70, 85))
            for p in range(5):
                px = x0 + 220 + p * 52
                d.rounded_rectangle([px, yy + 2, px + 44, yy + 26], radius=8,
                                    fill=col if p < v else (222, 216, 206))
    path = os.path.join(out, "Previews", "cars_stats.png")
    sheet.save(path)
    return path


def main(out):
    reset_scene()
    cars = {}
    meta = {}
    for name, fn in BUILDERS.items():
        ob = fn()
        ob["base_name"] = ob.name
        export_fbx(ob, os.path.join(out, "FBX", "Cars", ob.name + ".fbx"))
        tris = tri_count(ob) + sum(tri_count(g) for c in ob.children for g in c.children if g.type == "MESH")
        xs = [v.co.x for v in ob.data.vertices]; ys = [v.co.y for v in ob.data.vertices]
        meta[ob.name] = {"role": CARS[name][2],
                         "stats_1to5": dict(zip(["speed", "acceleration", "grip", "weight"], CARS[name][1])),
                         "triangles": tris,
                         "length_m": round(max(ys) - min(ys), 2)}
        cars[name] = ob
    with open(os.path.join(out, "cars.json"), "w", encoding="utf-8") as fh:
        json.dump(meta, fh, indent=2, ensure_ascii=False)
    os.makedirs(os.path.join(out, "Blend"), exist_ok=True)
    for i, ob in enumerate(cars.values()):
        ob.location = (i * 2.0, 0, 0)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, "Blend", "CT_Cars.blend"))

    # render individual 3/4 de cada coche
    cam = setup_preview((0, 3, 2), (0, 0, 0), res=(900, 900), samples=48)
    bpy.data.objects["PreviewFloor"].data.materials[0] = material("Border")
    bpy.data.objects["PreviewFloor"].location.z = 0.25
    cam.data.lens = 60
    renders = {}
    for name, ob in cars.items():
        for o in cars.values():
            hide = o is not ob
            hide_tree(o, hide)
        c = Vector(ob.location) + Vector((0, 0, 0.3))
        cam.location = c + Vector((2.6, -3.1, 1.9))
        cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
        p = os.path.join(out, "Previews", f"car_{name}.png")
        render(p)
        renders[name] = p
    print(json.dumps(meta, indent=1, ensure_ascii=False))
    return stat_sheet(out, renders)


if __name__ == "__main__":
    main(sys.argv[1])
