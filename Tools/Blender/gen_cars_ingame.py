"""Vista de juego: los 4 coches de prueba en la parrilla con la cámara vertical del juego."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import bpy
from mathutils import Matrix, Vector
from common import *
import gen_cars as C
from gen_showcase import piece


def main(out):
    reset_scene()
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
    # parrilla escalonada: dos coches detrás de la línea y dos un poco más atrás
    for k, (name, fn) in enumerate(C.BUILDERS.items()):
        car = fn()
        lane = [c for c in pieces[1].children if c.name.startswith(f"Lane{k}_0{1 + (k % 2) * 2}")][0]
        car.location = lane.matrix_world.translation
    cam = setup_preview((0, -2, 9.5), (0.6, -17, 0), res=(1080, 1920), samples=40)
    cam.data.lens = 30
    bpy.data.objects["PreviewFloor"].data.materials[0] = material("Mint")
    render(os.path.join(out, "Previews", "cars_ingame.png"))


if __name__ == "__main__":
    main(sys.argv[1])
