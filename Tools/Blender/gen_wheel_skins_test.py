"""Skins de rueda de Cozy Track: exporta las mallas de cada diseño y prueba combinaciones
de skin de carrocería × skin de rueda.

Una skin de rueda = diseño (malla Wheel_<Diseño>, tamaño unidad) + colores de neumático, llanta y tapacubos.
Uso: python gen_wheel_skins_test.py <carpeta_salida>
"""
import sys, os, json, math
sys.path.insert(0, os.path.dirname(__file__))
import bpy
from mathutils import Vector
from common import *
import common
import gen_cars as C
import gen_wheels as W
import gen_skins_test as ST

# id, nombre visible, diseño, neumático, llanta, tapacubos (nombres de la paleta)
WHEEL_SKINS = [
    ("classic",   "Clásica",      "Classic",   "WheelTyre", "WheelRim", "WheelCap"),
    ("gold",      "Radios oro",   "Spokes",    "WheelTyre", "Gold",     "Gold"),
    ("star",      "Estrella",     "Star",      "WheelTyre", "Sun",      "Coral"),
    ("turbine",   "Turbina",      "Turbine",   "WheelTyre", "Sky",      "Snow"),
    ("racing",    "Deportiva",    "Racing",    "WheelTyre", "Coral",    "Snow"),
    ("offroad",   "Todoterreno",  "Knobby",    "WheelTyre", "Night",    "Sun"),
    ("whitewall", "Banda blanca", "Whitewall", "WheelTyre", "Snow",     "Stone"),
    ("candy",     "Caramelo",     "Classic",   "Bubblegum", "Snow",     "Coral"),
]

BODY_FOR_ROW = {"Dart": ST.SKINS[0], "Hopper": ST.SKINS[2], "Gecko": ST.SKINS[6], "Rhino": ST.SKINS[4]}


def lin(rgb):
    return tuple(srgb_to_linear(c) for c in rgb) + (1.0,)


_mats = {}


def flat(col_name):
    if col_name not in _mats:
        m = bpy.data.materials.new(f"WS_{col_name}")
        m.use_nodes = True
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = lin(PALETTE[col_name])
        m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.45
        _mats[col_name] = m
    return _mats[col_name]


def style_meshes():
    """Construye una malla por diseño (compartida por todas las ruedas)."""
    meshes = {}
    for st in W.STYLES:
        ob = W.build(st, name=f"Wheel_{st}")
        meshes[st] = ob
    return meshes


def apply_wheel_skin(car_ob, skin, meshes):
    _, _, style, tyre, rim, cap = skin
    for sock in car_ob.children:
        for w in sock.children:
            if w.type != "MESH" or not w.name.startswith("Wheel_"):
                continue
            w.data = meshes[style].data
            for i, col in enumerate((tyre, rim, cap)):
                w.material_slots[i].link = "OBJECT"
                w.material_slots[i].material = flat(col)


def export_styles(out, meshes):
    for st, ob in meshes.items():
        ob["base_name"] = ob.name
        export_fbx(ob, os.path.join(out, "FBX", "Wheels", ob.name + ".fbx"))


def styles_sheet(out, meshes):
    cam = setup_preview((0, 3, 2), (0, 0, 0), res=(420, 420), samples=32)
    bpy.data.objects["PreviewFloor"].data.materials[0] = material("Border")
    bpy.data.objects["PreviewFloor"].location.z = 0.25 - 0.62
    cam.data.lens = 70
    cells = []
    for skin in WHEEL_SKINS[:7]:
        style = skin[2]
        for o in meshes.values():
            o.hide_render = True
        ob = meshes[style]
        ob.hide_render = False
        ob.location = (0, 0, 0)
        ob.scale = (0.5, 0.62, 0.62)
        for i, col in enumerate(skin[3:]):
            ob.material_slots[i].link = "OBJECT"
            ob.material_slots[i].material = flat(col)
        cam.location = Vector((3.2, -1.3, 0.8))
        cam.rotation_euler = (Vector((0, 0, 0)) - cam.location).to_track_quat("-Z", "Y").to_euler()
        p = os.path.join(out, "Previews", "wheels_tmp", f"style_{style}.png")
        render(p)
        cells.append((W.STYLE_NAMES_ES[style], p))
        ob.scale = (1, 1, 1)
        for i in range(3):
            ob.material_slots[i].link = "DATA"
    for o in meshes.values():
        o.hide_render = True
    return cells


def combos(out, meshes):
    cars = {}
    for name, fn in C.BUILDERS.items():
        ob = fn()
        ST.apply_skin(ob, name, BODY_FOR_ROW[name], out)
        cars[name] = ob
    cam = bpy.context.scene.camera
    cam.data.lens = 55
    cells = {}
    for name, ob in cars.items():
        for o in cars.values():
            hide_tree(o, o is not ob)
        c = Vector(ob.location) + Vector((0, 0, 0.28))
        cam.location = c + Vector((3.0, -2.0, 1.25))
        cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
        for skin in WHEEL_SKINS:
            apply_wheel_skin(ob, skin, meshes)
            p = os.path.join(out, "Previews", "wheels_tmp", f"{name}_{skin[0]}.png")
            render(p)
            cells[(name, skin[0])] = p
    return cells


def compose(out, style_cells, combo_cells):
    from PIL import Image, ImageDraw, ImageFont
    F = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
    fh = ImageFont.truetype(F, 24)
    fs = ImageFont.truetype(F.replace("-Bold", ""), 18)
    # hoja de diseños
    cw = 260
    img = Image.new("RGB", (cw * len(style_cells) + 20, int((cw - 10) * 0.88) + 70), (236, 231, 222))
    d = ImageDraw.Draw(img)
    for k, (label, p) in enumerate(style_cells):
        im = Image.open(p).convert("RGB")
        im = im.crop((0, int(im.height * 0.12), im.width, im.height))
        cell = im.resize((cw - 10, int((cw - 10) * im.height / im.width)))
        img.paste(cell, (10 + k * cw, 10))
        tw = d.textlength(label, font=fh)
        d.text((10 + k * cw + (cw - 10 - tw) / 2, int((cw - 10) * 0.88) + 22), label, font=fh, fill=(45, 45, 60))
    img.save(os.path.join(out, "Previews", "wheel_styles.png"))
    # rejilla de combinaciones
    cw, ch = 280, 226
    left, top = 260, 64
    names = list(C.CARS)
    img = Image.new("RGB", (left + cw * len(WHEEL_SKINS) + 20, top + ch * len(names) + 20), (236, 231, 222))
    d = ImageDraw.Draw(img)
    for j, s in enumerate(WHEEL_SKINS):
        tw = d.textlength(s[1], font=fh)
        d.text((left + j * cw + (cw - tw) / 2, 20), s[1], font=fh, fill=(45, 45, 60))
    for i, n in enumerate(names):
        d.text((20, top + i * ch + ch / 2 - 26), n, font=fh, fill=(45, 45, 60))
        d.text((20, top + i * ch + ch / 2 + 4), "carrocería: " + BODY_FOR_ROW[n][0].lower(), font=fs, fill=(110, 105, 100))
        for j, s in enumerate(WHEEL_SKINS):
            cell = Image.open(combo_cells[(n, s[0])]).convert("RGB").resize((cw - 8, ch - 8))
            img.paste(cell, (left + j * cw + 4, top + i * ch + 4))
    img.save(os.path.join(out, "Previews", "wheel_skins_on_cars.png"))


def write_json(out):
    def hx(c):
        return "#%02X%02X%02X" % tuple(round(x * 255) for x in c)
    rows = [{"id": i, "name": n, "mesh": f"Wheel_{st}", "tyre": hx(PALETTE[t]), "rim": hx(PALETTE[r]),
             "cap": hx(PALETTE[c])} for (i, n, st, t, r, c) in WHEEL_SKINS]
    os.makedirs(os.path.join(out, "Unity"), exist_ok=True)
    with open(os.path.join(out, "Unity", "wheel_skins.json"), "w", encoding="utf-8") as fh:
        json.dump({"wheelSkins": rows}, fh, indent=2, ensure_ascii=False)


def main(out):
    reset_scene()
    meshes = style_meshes()
    export_styles(out, meshes)
    style_cells = styles_sheet(out, meshes)
    combo_cells = combos(out, meshes)
    compose(out, style_cells, combo_cells)
    write_json(out)
    import shutil
    shutil.rmtree(os.path.join(out, "Previews", "wheels_tmp"), ignore_errors=True)


if __name__ == "__main__":
    main(sys.argv[1])
