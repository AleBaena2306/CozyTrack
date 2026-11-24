"""Monta un circuito de prueba encadenando piezas por sus Socket_End y lo renderiza
con cámara de juego (vertical, cenital inclinada). Sirve para comprobar que las
piezas encajan y para ver el conjunto.

Uso: python gen_showcase.py <carpeta_salida>
"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
import bpy
from mathutils import Matrix, Vector
from common import *
import gen_track as T
import gen_props as P


def piece(kind, nl=4):
    lanes = lambda t: T.lane_offsets(nl)
    width = lambda t: nl * T.LANE_W
    defs = {
        "Straight8": (T.path_straight(8), {"straight_step": 2.0}),
        "Straight4": (T.path_straight(4), {"straight_step": 2.0}),
        "Curve90_L": (T.path_arc(8, 90, "L"), {"kerbs": True}),
        "Curve90_R": (T.path_arc(8, 90, "R"), {"kerbs": True}),
        "Curve45_L": (T.path_arc(8, 45, "L"), {"kerbs": True}),
        "Curve45_R": (T.path_arc(8, 45, "R"), {"kerbs": True}),
        "Tight_L": (T.path_arc(5, 90, "L"), {"kerbs": True}),
        "StartFinish": (T.path_straight(8), {"checker": 4.0, "gate": True, "straight_step": 0.5}),
    }
    (f, L), kw = defs[kind]
    ob, _ = T.build_piece(f"{kind}", f, L, lanes, width, **kw)
    return ob


def skin_material(name, path):
    m = bpy.data.materials.new(f"Skin_{name}")
    m.use_nodes = True
    nt = m.node_tree
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(path)
    nt.links.new(tex.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
    nt.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.5
    return m


def main(out):
    reset_scene()
    layout = ["StartFinish", "Straight8", "Curve90_L", "Curve45_L", "Curve45_R", "Tight_L",
              "Straight4", "Curve90_L", "Curve90_R", "Curve90_L"]
    world = Matrix.Identity(4)
    pieces = []
    for kind in layout:
        ob = piece(kind)
        ob.matrix_world = world
        bpy.context.view_layer.update()
        end = [c for c in ob.children if c.name.startswith("Socket_End")][0]
        world = world @ end.matrix_local
        pieces.append(ob)
        for c in list(ob.children):
            if c.type == "EMPTY":
                c.hide_render = True
    gap = world.translation.length
    print("Hueco final del circuito de prueba (m):", round(gap, 3))

    # coches en la parrilla, con skins
    import gen_cars as C
    import gen_skins_test as ST
    start = pieces[0]
    picks = [ST.SKINS[0], ST.SKINS[1], ST.SKINS[2], ST.SKINS[6]]
    bpy.context.view_layer.update()
    for k, (name, fn) in enumerate(C.BUILDERS.items()):
        car = fn()
        ST.apply_skin(car, name, picks[k], out)
        lane = [c for c in start.children if c.name.startswith(f"Lane{k}_01")][0]
        car.location = lane.matrix_world.translation
    # objetos y decorados
    items = P.make_items()
    pos = {"Item_Box": (3.0, -30.0, 0.55), "Item_Coin": (1.8, -26.0, 0.45), "Item_BoostPad": (-0.6, -20.0, 0.0)}
    for ob in items:
        if ob.name in pos:
            ob.location = pos[ob.name]
        else:
            bpy.data.objects.remove(ob)
    scen = P.make_scenery()
    spots = [(-6, -4), (7, -10), (-6, -16), (9, -2), (-7, -26), (15, -24), (24, -18), (20, -32), (-5, -34), (28, -6)]
    k = 0
    for theme in ("Garden", "Common"):
        for ob in scen[theme]:
            if k < len(spots):
                ob.location = (spots[k][0], spots[k][1], 0)
                k += 1
    for theme in ("Playroom", "Snow", "Space"):
        for ob in scen[theme]:
            bpy.data.objects.remove(ob)

    cam = setup_preview((0, 9, 16), (2, -18, 0), res=(1080, 1920), samples=32)
    cam.data.lens = 26
    bpy.data.objects["PreviewFloor"].data.materials[0] = material("Mint")
    render(os.path.join(out, "Previews", "showcase_circuit.png"))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, "Blend", "CT_Showcase.blend"))


if __name__ == "__main__":
    main(sys.argv[1])
