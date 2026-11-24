"""Genera las piezas modulares de pista de Cozy Track.

Uso: python gen_track.py <carpeta_salida>

Cada pieza:
- Empieza en el origen y avanza hacia -Y de Blender (+Z en Unity).
- Lleva hijos vacíos (empties): Socket_Start, Socket_End (su eje adelante apunta
  a la dirección de salida) y LaneK_NN (puntos del centro de cada carril cada ~1 m,
  carril 0 = el de la izquierda en el sentido de la marcha).
"""
import sys, os, math, json
sys.path.insert(0, os.path.dirname(__file__))
import bpy, bmesh
from mathutils import Vector
from common import *

LANE_W = 1.2        # ancho de carril
BORDER_W = 0.3      # bordillo a cada lado
BORDER_H = 0.12     # altura del bordillo
BASE_D = 0.2        # grosor de la base bajo la pista
KERB_SEG = 0.8      # longitud de cada tramo rojo/blanco de los pianos
UP = Vector((0, 0, 1))


def smooth(t):
    return t * t * (3 - 2 * t)


def lane_offsets(n):
    return [(i - (n - 1) / 2) * LANE_W for i in range(n)]


# ---------- trayectorias ----------
def path_straight(L, rise=0.0):
    def f(t):
        z = rise * smooth(t)
        return Vector((0, -L * t, z)), Vector((0, -1, 0))
    return f, L


def path_arc(R, deg, side):
    A = math.radians(deg)
    sx = 1 if side == "L" else -1

    def f(t):
        a = A * t
        p = Vector((sx * (R - R * math.cos(a)), -R * math.sin(a), 0))
        tan = Vector((sx * math.sin(a), -math.cos(a), 0))
        return p, tan
    return f, R * A


# ---------- construcción ----------
def build_piece(name, path, length, lanes_fn, width_fn, kerbs=False, checker=None, gate=False, straight_step=None):
    """lanes_fn(t) -> lista de desplazamientos laterales; width_fn(t) -> ancho de la calzada."""
    step = 0.25 if (kerbs or straight_step is None) else straight_step
    n = max(2, int(math.ceil(length / step)))
    samples = []
    for i in range(n + 1):
        t = i / n
        p, tan = path(t)
        tan = tan.normalized()
        right = tan.cross(UP).normalized()
        samples.append((t, p, tan, right, t * length))

    mats = ["Base", "Border", "Road", "KerbRed", "KerbWhite", "RoadDark", "Rail",
            "CheckBlack", "CheckWhite", "Sun", "Coral"]
    mi = {m: i for i, m in enumerate(mats)}
    bm = bmesh.new()

    def P(s, o, z):
        _, p, _, r, _ = s
        return p + r * o + UP * z

    # perfil cerrado (o, z, material de la cara que empieza en este punto)
    def profile(t):
        rw = width_fn(t) / 2.0
        W = rw + BORDER_W
        return [
            (-W, -BASE_D, "Base"),          # fondo
            (W, -BASE_D, "Base"),           # pared exterior derecha
            (W, BORDER_H, "BORDER"),        # techo bordillo derecho
            (rw, BORDER_H, "BORDER"),       # pared interior derecha
            (rw, 0.0, "Road"),              # calzada
            (-rw, 0.0, "BORDER"),           # pared interior izquierda
            (-rw, BORDER_H, "BORDER"),      # techo bordillo izquierdo
            (-W, BORDER_H, "Base"),         # pared exterior izquierda
        ]

    rows = []
    for s in samples:
        rows.append([bm.verts.new(P(s, o, z)) for (o, z, _) in profile(s[0])])
    prof0 = profile(0)
    m = len(prof0)
    for i in range(n):
        mid_s = (samples[i][4] + samples[i + 1][4]) / 2
        kerb_mat = "KerbRed" if int(mid_s // KERB_SEG) % 2 == 0 else "KerbWhite"
        for j in range(m):
            a, b = rows[i][j], rows[i][(j + 1) % m]
            c, d = rows[i + 1][(j + 1) % m], rows[i + 1][j]
            f = bm.faces.new((a, d, c, b))
            mat = prof0[j][2]
            if mat == "BORDER":
                mat = kerb_mat if kerbs else "Border"
            f.material_index = mi[mat]
    # tapas
    fs = bm.faces.new(list(rows[0]))
    fe = bm.faces.new(list(reversed(rows[-1])))
    fs.material_index = fe.material_index = mi["Base"]
    bm.normal_update()
    # orientación: la cara del fondo debe mirar hacia abajo
    test = [f for f in bm.faces if f.material_index == mi["Base"]][0]
    bottom_faces = [f for f in bm.faces if abs(f.calc_center_median().z + BASE_D) < 1e-4 and len(f.verts) == 4]
    if bottom_faces and bottom_faces[0].normal.z > 0:
        for f in bm.faces:
            f.normal_flip()

    # ranura y raíles de cada carril (calcomanías)
    def strip(o_fn, half, z, mat, s0=0.0, s1=None):
        prev = None
        for s in samples:
            if s[4] < s0 - 1e-6 or (s1 is not None and s[4] > s1 + 1e-6):
                continue
            o = o_fn(s[0])
            row = (bm.verts.new(P(s, o - half, z)), bm.verts.new(P(s, o + half, z)))
            if prev:
                f = bm.faces.new((prev[0], row[0], row[1], prev[1]))
                if f.normal.z < 0:
                    f.normal_flip()
                f.material_index = mi[mat]
            prev = row

    nl = len(lanes_fn(0))
    for k in range(nl):
        of = (lambda k: (lambda t: lanes_fn(t)[k]))(k)
        strip(of, 0.045, 0.006, "RoadDark")
        strip(lambda t, of=of: of(t) - 0.075, 0.012, 0.006, "Rail")
        strip(lambda t, of=of: of(t) + 0.075, 0.012, 0.006, "Rail")

    # línea de meta a cuadros
    if checker is not None:
        s_mid = checker
        rw = width_fn(s_mid / length) / 2
        cols = int(round(2 * rw / 0.3))
        cw = 2 * rw / cols
        for row in range(2):
            for c in range(cols):
                s0 = s_mid - 0.3 + row * 0.3
                s1 = s0 + 0.3
                # interpolación en recta: posición directa
                o0 = -rw + c * cw
                o1 = o0 + cw
                vs = [bm.verts.new(Vector((-o, -s, 0.003))) for (o, s) in
                      ((o0, s0), (o1, s0), (o1, s1), (o0, s1))]
                # nota: en recta right = -X, por eso -o
                f = bm.faces.new(vs)
                if f.normal.z < 0:
                    f.normal_flip()
                f.material_index = mi["CheckBlack" if (row + c) % 2 == 0 else "CheckWhite"]

    ob = new_object(name, bm, [material(x) for x in mats])
    me = ob.data
    for p in me.polygons:
        p.use_smooth = False

    children = []
    if gate:
        children.append(build_gate(ob, width_fn(0.5) / 2 + BORDER_W / 2, checker))

    # empties
    add_empty("Socket_Start", ob, (0, 0, 0), 0.0, 0.6)
    _, pe, te, _, _ = samples[-1]
    yaw = math.atan2(te.x, -te.y)
    add_empty("Socket_End", ob, pe, yaw, 0.6)
    pts = max(2, int(round(length)) + 1)
    lane_data = []
    for k in range(nl):
        lp = []
        for q in range(pts):
            t = q / (pts - 1)
            p, tan = path(t)
            r = tan.normalized().cross(UP).normalized()
            loc = p + r * lanes_fn(t)[k]
            add_empty(f"Lane{k}_{q:02d}", ob, loc, 0.0, 0.15)
            lp.append([round(loc.x, 4), round(loc.y, 4), round(loc.z, 4)])
        lane_data.append(lp)
    return ob, {
        "length_centerline_m": round(length, 3),
        "lanes_start": len(lanes_fn(0)),
        "end_position_blender": [round(pe.x, 4), round(pe.y, 4), round(pe.z, 4)],
        "end_yaw_deg": round(math.degrees(yaw), 2),
        "kerbs": kerbs,
    }


def build_gate(parent, half_span, s_mid):
    """Arco de salida/meta: dos postes y un travesaño con banderines."""
    bm = bmesh.new()
    mats = ["Coral", "Sun", "CheckBlack", "CheckWhite"]
    y = -s_mid
    for sx in (-1, 1):
        bmesh.ops.create_cube(bm, size=1.0, matrix=__import__("mathutils").Matrix.LocRotScale(
            Vector((sx * half_span, y, 1.3)), None, Vector((0.25, 0.25, 2.6))))
    bmesh.ops.create_cube(bm, size=1.0, matrix=__import__("mathutils").Matrix.LocRotScale(
        Vector((0, y, 2.75)), None, Vector((2 * half_span + 0.25, 0.3, 0.5))))
    for f in bm.faces:
        f.material_index = 0
    # cuadros del travesaño (cara frontal y trasera)
    ncols = int((2 * half_span) / 0.25)
    for side in (-1, 1):
        for r in range(2):
            for c in range(ncols):
                x0 = -half_span + c * 0.25
                z0 = 2.52 + r * 0.23
                yy = y + side * 0.152
                vs = [bm.verts.new(Vector(v)) for v in
                      ((x0, yy, z0), (x0 + 0.25, yy, z0), (x0 + 0.25, yy, z0 + 0.23), (x0, yy, z0 + 0.23))]
                f = bm.faces.new(vs if side < 0 else list(reversed(vs)))
                f.material_index = 2 if (r + c) % 2 == 0 else 3
    ob = new_object(parent.name + "_Gate", bm, [material(m) for m in mats])
    ob.parent = parent
    return ob


def make_all(out):
    reset_scene()
    meta = {}
    pieces = []
    for nl in (2, 4):
        rw = nl * LANE_W
        lanes = lambda t, nl=nl: lane_offsets(nl)
        width = lambda t, rw=rw: rw
        tag = f"{nl}L"
        defs = [
            (f"Track_{tag}_Straight8", path_straight(8), {"straight_step": 2.0}),
            (f"Track_{tag}_Straight4", path_straight(4), {"straight_step": 2.0}),
            (f"Track_{tag}_Curve45_L", path_arc(8, 45, "L"), {"kerbs": True}),
            (f"Track_{tag}_Curve45_R", path_arc(8, 45, "R"), {"kerbs": True}),
            (f"Track_{tag}_Curve90_L", path_arc(8, 90, "L"), {"kerbs": True}),
            (f"Track_{tag}_Curve90_R", path_arc(8, 90, "R"), {"kerbs": True}),
            (f"Track_{tag}_Curve90Tight_L", path_arc(5, 90, "L"), {"kerbs": True}),
            (f"Track_{tag}_Curve90Tight_R", path_arc(5, 90, "R"), {"kerbs": True}),
            (f"Track_{tag}_RampUp", path_straight(8, 1.5), {}),
            (f"Track_{tag}_StartFinish", path_straight(8), {"checker": 4.0, "gate": True, "straight_step": 0.5}),
        ]
        for name, (f, L), kw in defs:
            ob, info = build_piece(name, f, L, lanes, width, **kw)
            pieces.append(ob)
            meta[name] = info

    # transición de 2 a 4 carriles
    def lanes_split(t):
        e = smooth(min(1.0, max(0.0, (t - 0.1) / 0.8)))
        return [-0.6 - 1.2 * e, -0.6, 0.6, 0.6 + 1.2 * e]
    ob, info = build_piece("Track_Split_2to4", *path_straight(10),
                           lanes_split, lambda t: 2 * LANE_W + 2 * LANE_W * smooth(t))
    info["lanes_end"] = 4
    info["note"] = "Carriles 1 y 2 siguen rectos; 0 y 3 se abren. Girada 180 grados sirve de 4 a 2."
    pieces.append(ob)
    meta[ob.name] = info

    # exportar y contar triángulos
    for ob in pieces:
        export_fbx(ob, os.path.join(out, "FBX", "Track", ob.name + ".fbx"))
        tris = tri_count(ob) + sum(tri_count(c) for c in ob.children if c.type == "MESH")
        meta[ob.name]["triangles"] = tris

    # disposición para el .blend y la vista previa
    for i, ob in enumerate(pieces):
        col, row = i % 7, i // 7
        ob.location = (col * 22, -row * 22, 0)
    os.makedirs(os.path.join(out, "Blend"), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, "Blend", "CT_Track.blend"))
    with open(os.path.join(out, "track_pieces.json"), "w", encoding="utf-8") as fh:
        json.dump(meta, fh, indent=2, ensure_ascii=False)

    setup_preview((66, -20, 120), (66, -21, 0), ortho_scale=160)
    render(os.path.join(out, "Previews", "track_pieces.png"))


if __name__ == "__main__":
    make_all(sys.argv[1])
