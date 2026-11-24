"""Exporta a un único GLB los modelos que usa el prototipo web de Cozy Track.

Uso: python gen_glb.py <archivo.glb>
Cada modelo queda como un nodo raíz con su nombre (Track_*, Car_*, Item_*, Prop_*), todos en el origen.
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import bpy
from common import *
import gen_track as T
import gen_cars as C
import gen_props as P


def main(path):
    reset_scene()
    lanes = lambda t: T.lane_offsets(4)
    width = lambda t: 4 * T.LANE_W
    defs = {
        "Track_StartFinish": (T.path_straight(8), {"checker": 4.0, "gate": True, "straight_step": 0.5}),
        "Track_Straight8": (T.path_straight(8), {"straight_step": 2.0}),
        "Track_Straight4": (T.path_straight(4), {"straight_step": 2.0}),
        "Track_Curve90_L8": (T.path_arc(8, 90, "L"), {"kerbs": True}),
        "Track_Curve45_L8": (T.path_arc(8, 45, "L"), {"kerbs": True}),
        "Track_Curve45_R8": (T.path_arc(8, 45, "R"), {"kerbs": True}),
    }
    for name, ((f, L), kw) in defs.items():
        T.build_piece(name, f, L, lanes, width, **kw)
    for name, fn in C.BUILDERS.items():
        fn()
    import gen_vehicles as V
    for name in V.VEH:
        V.build(name)
    keep_items = {"Item_Box", "Item_Coin", "Item_Shield", "Item_Projectile", "Item_BoostPad", "Item_Magnet"}
    for ob in P.make_items():
        if ob.name not in keep_items:
            bpy.data.objects.remove(ob)
    scen = P.make_scenery()
    keep_props = {"Prop_Garden_Tree", "Prop_Garden_Bush", "Prop_Garden_Flower", "Prop_Garden_Rock",
                  "Prop_Common_TrafficCone", "Prop_Common_Flag", "Prop_Snow_Pine", "Prop_Playroom_ToyBlock", "Prop_Snow_Snowman", "Prop_Snow_Pile", "Prop_Snow_IceCrystal", "Prop_Space_Planet", "Prop_Space_Rocket", "Prop_Space_Star", "Prop_Space_Asteroid", "Prop_Playroom_Pencil", "Prop_Playroom_Dice", "Prop_Playroom_BookStack"}
    for obs in scen.values():
        for ob in obs:
            if ob.name not in keep_props:
                bpy.data.objects.remove(ob)
    # fuera los vacíos de carril y conectores (el prototipo calcula los carriles por código)
    for ob in list(bpy.data.objects):
        if ob.type == "EMPTY" and (ob.name.startswith("Lane") or ob.name.startswith("Socket")):
            bpy.data.objects.remove(ob)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_yup=True,
                              export_apply=True, export_animations=False, export_cameras=False,
                              export_lights=False)
    print("GLB:", path, os.path.getsize(path) // 1024, "KB")


if __name__ == "__main__":
    main(sys.argv[1])
