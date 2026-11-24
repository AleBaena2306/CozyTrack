"""Prueba de skins de Cozy Track: los 4 coches con varias skins y una vista de juego.

Una skin solo cambia dos ranuras de material de cada coche:
- CT_Paint_<Coche>: color liso o textura de patrón (UV a escala del mundo, 0,6 m por repetición).
- CT_Trim_<Coche>: color de los detalles (franjas, alerón, arco, defensa).
Cristales, ruedas, faros y números no cambian.

Uso: python gen_skins_test.py <carpeta_salida>
"""
import sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
import bpy
from mathutils import Vector, Matrix
from common import *
import common
import gen_cars as C

# nombre visible, textura del patrón (o None), color liso de pintura (o None = el de fábrica), color de detalles
SKINS = [
    ("Fábrica",    None,              None,        None),
    ("Rayas",      "Stripes_Coral",   None,        "Snow"),
    ("Lunares",    "Dots_Sky",        None,        "Snow"),
    ("Cuadros",    "Checker_Race",    None,        "Coral"),
    ("Estrellas",  "Stars_Night",     None,        "Sun"),
    ("Rayos",      "Bolt_Electric",   None,        "Sun"),
    ("Medianoche", None,              "Night",     "Sun"),
    ("Chicle",     None,              "Bubblegum", "Snow"),
]


def lin(rgb):
    return tuple(srgb_to_linear(c) for c in rgb) + (1.0,)


def skin_paint(skin, car, out):
    name, tex, solid, _ = skin
    m = bpy.data.materials.new(f"SkinPaint_{name}_{car}")
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.5
    if tex:
        img = bpy.data.images.load(os.path.join(out, "Textures", "Skins", f"Skin_{tex}.png"), check_existing=True)
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = img
        n.interpolation = "Smart"
        nt.links.new(n.outputs["Color"], bsdf.inputs["Base Color"])
    else:
        col = PALETTE[solid] if solid else PALETTE[C.CARS[car][0]]
        bsdf.inputs["Base Color"].default_value = lin(col)
    return m


def skin_trim(skin, car):
    name, _, _, trim = skin
    m = bpy.data.materials.new(f"SkinTrim_{name}_{car}")
    m.use_nodes = True
    col = PALETTE[trim] if trim else PALETTE[C.CARS[car][3]]
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = lin(col)
    m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.5
    return m


def apply_skin(car_ob, car, skin, out):
    for i, slot in enumerate(car_ob.data.materials):
        if slot.name.startswith(f"CT_Paint_{car}"):
            car_ob.data.materials[i] = skin_paint(skin, car, out)
        elif slot.name.startswith(f"CT_Trim_{car}"):
            car_ob.data.materials[i] = skin_trim(skin, car)


def build_cars():
    cars = {}
    for name, fn in C.BUILDERS.items():
        ob = fn()
        cars[name] = (ob, list(ob.data.materials))
    return cars


def restore(cars, name):
    ob, mats = cars[name]
    for i, m in enumerate(mats):
        ob.data.materials[i] = m


def grid(out):
    reset_scene()
    cars = build_cars()
    cam = setup_preview((0, 3, 2), (0, 0, 0), res=(520, 420), samples=28)
    bpy.data.objects["PreviewFloor"].data.materials[0] = material("Border")
    bpy.data.objects["PreviewFloor"].location.z = 0.25
    cam.data.lens = 58
    cells = {}
    for name, (ob, _) in cars.items():
        for other, (o2, _) in cars.items():
            hide = other != name
            hide_tree(o2, hide)
        c = Vector(ob.location) + Vector((0, 0, 0.32))
        cam.location = c + Vector((2.3, -2.9, 1.8))
        cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
        for skin in SKINS:
            restore(cars, name)
            apply_skin(ob, name, skin, out)
            p = os.path.join(out, "Previews", "skins_test", f"{name}_{skin[0]}.png")
            render(p)
            cells[(name, skin[0])] = p
    return cells


def sheet(out, cells):
    from PIL import Image, ImageDraw, ImageFont
    F = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
    fh = ImageFont.truetype(F, 26)
    cw, ch = 300, 242
    left, top = 150, 60
    names = list(C.CARS)
    img = Image.new("RGB", (left + cw * len(SKINS) + 20, top + ch * len(names) + 20), (236, 231, 222))
    d = ImageDraw.Draw(img)
    for j, s in enumerate(SKINS):
        tw = d.textlength(s[0], font=fh)
        d.text((left + j * cw + (cw - tw) / 2, 18), s[0], font=fh, fill=(45, 45, 60))
    for i, n in enumerate(names):
        d.text((20, top + i * ch + ch / 2 - 14), n, font=fh, fill=(45, 45, 60))
        for j, s in enumerate(SKINS):
            cell = Image.open(cells[(n, s[0])]).convert("RGB").resize((cw - 8, ch - 8))
            img.paste(cell, (left + j * cw + 4, top + i * ch + 4))
    p = os.path.join(out, "Previews", "skins_on_cars.png")
    img.save(p)
    return p


def ingame(out):
    reset_scene()
    from gen_showcase import piece
    world = Matrix.Identity(4)
    pieces = []
    for kind in ["StartFinish", "Straight8", "Curve90_L", "Curve45_R"]:
        ob = piece(kind)
        ob.matrix_world = world
        bpy.context.view_layer.update()
        end = [c for c in ob.children if c.name.startswith("Socket_End")][0]
        world = world @ end.matrix_local
        for c in list(ob.children):
            if c.type == "EMPTY":
                c.hide_render = True
        pieces.append(ob)
    bpy.context.view_layer.update()
    picks = {"Dart": SKINS[3], "Hopper": SKINS[2], "Gecko": SKINS[5], "Rhino": SKINS[4]}
    for k, (name, fn) in enumerate(C.BUILDERS.items()):
        car = fn()
        apply_skin(car, name, picks[name], out)
        lane = [c for c in pieces[1].children if c.name.startswith(f"Lane{k}_0{1 + (k % 2) * 2}")][0]
        car.location = lane.matrix_world.translation
    cam = setup_preview((0, -2, 9.5), (0.6, -17, 0), res=(1080, 1920), samples=40)
    cam.data.lens = 30
    bpy.data.objects["PreviewFloor"].data.materials[0] = material("Mint")
    render(os.path.join(out, "Previews", "skins_ingame.png"))


if __name__ == "__main__":
    out = sys.argv[1]
    cells = grid(out)
    print(sheet(out, cells))
    ingame(out)
    with open(os.path.join(out, "skins_test.json"), "w", encoding="utf-8") as fh:
        json.dump([{"name": s[0], "pattern": s[1], "paint_color": s[2], "trim_color": s[3]} for s in SKINS],
                  fh, indent=2, ensure_ascii=False)
