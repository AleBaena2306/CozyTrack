"""Utilidades comunes para generar los assets de Cozy Track con Blender (bpy).

Convenciones:
- Unidades en metros. Z arriba en Blender.
- Las piezas de pista empiezan en el origen y avanzan hacia -Y de Blender,
  que al exportar a FBX (forward -Z, up Y, transformación aplicada) es +Z en Unity.
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector

# Paleta "cozy / whimsical": colores saturados pero suaves.
PALETTE = {
    "Road":        (0.20, 0.22, 0.30),
    "RoadDark":    (0.10, 0.11, 0.16),
    "Rail":        (0.80, 0.78, 0.74),
    "Base":        (0.96, 0.80, 0.55),   # madera clara de juguete
    "BaseShade":   (0.85, 0.62, 0.40),
    "Border":      (0.98, 0.96, 0.92),
    "KerbRed":     (0.95, 0.30, 0.32),
    "KerbWhite":   (0.98, 0.96, 0.92),
    "CheckBlack":  (0.10, 0.10, 0.12),
    "CheckWhite":  (0.98, 0.98, 0.98),
    "Coral":       (1.00, 0.45, 0.40),
    "Tangerine":   (1.00, 0.62, 0.25),
    "Sun":         (1.00, 0.84, 0.30),
    "Mint":        (0.45, 0.88, 0.65),
    "Leaf":        (0.40, 0.75, 0.35),
    "LeafDark":    (0.22, 0.55, 0.30),
    "Sky":         (0.45, 0.75, 1.00),
    "Ocean":       (0.25, 0.50, 0.95),
    "Lilac":       (0.70, 0.55, 0.95),
    "Grape":       (0.45, 0.30, 0.75),
    "Bubblegum":   (1.00, 0.55, 0.80),
    "Snow":        (0.95, 0.97, 1.00),
    "Ice":         (0.70, 0.90, 1.00),
    "Bark":        (0.55, 0.35, 0.22),
    "Stone":       (0.62, 0.62, 0.68),
    "Night":       (0.12, 0.12, 0.25),
    "Gold":        (1.00, 0.78, 0.20),
    "Shield":      (0.45, 0.85, 1.00),
}


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def material(name, alpha=1.0, emission=0.0, rough=0.55):
    """Material por nombre de la paleta (se reutiliza si ya existe)."""
    mname = f"CT_{name}"
    if mname in bpy.data.materials:
        return bpy.data.materials[mname]
    m = bpy.data.materials.new(mname)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    col = PALETTE[name]
    lin = tuple(srgb_to_linear(c) for c in col) + (1.0,)
    bsdf.inputs["Base Color"].default_value = lin
    bsdf.inputs["Roughness"].default_value = rough
    if alpha < 1.0:
        bsdf.inputs["Alpha"].default_value = alpha
        try:
            m.surface_render_method = "BLENDED"
        except Exception:
            pass
    if emission > 0:
        bsdf.inputs["Emission Color"].default_value = lin
        bsdf.inputs["Emission Strength"].default_value = emission
    m.diffuse_color = lin[:3] + (alpha,)
    return m


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def new_object(name, bm, mats):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def add_empty(name, parent, loc, rot_z=0.0, size=0.25):
    e = bpy.data.objects.new(name, None)
    e["base_name"] = name
    e.empty_display_type = "ARROWS"
    e.empty_display_size = size
    e.location = loc
    e.rotation_euler = (0.0, 0.0, rot_z)
    bpy.context.scene.collection.objects.link(e)
    e.parent = parent
    return e


def select_hierarchy(root):
    bpy.ops.object.select_all(action="DESELECT")
    stack = [root]
    while stack:
        o = stack.pop()
        o.select_set(True)
        stack.extend(o.children)
    bpy.context.view_layer.objects.active = root


def _clean_names(root):
    """Da a los hijos de root su nombre limpio (sin .001) apartando antes los demás."""
    mine = {root}
    stack = list(root.children)
    while stack:
        o = stack.pop(); mine.add(o); stack.extend(o.children)
    for o in bpy.data.objects:
        if o not in mine and "base_name" in o:
            o.name = f"tmp_{o.as_pointer()}"
    for o in mine:
        if "base_name" in o:
            o.name = o["base_name"]


def export_fbx(root, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    _clean_names(root)
    select_hierarchy(root)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        path_mode="STRIP",
    )


def tri_count(ob):
    me = ob.data
    return sum(len(p.vertices) - 2 for p in me.polygons)


def move_hierarchy(root, offset):
    root.location = Vector(root.location) + Vector(offset)


def setup_preview(cam_loc, cam_target, ortho_scale=None, res=(1600, 900), samples=24):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = samples
    sc.cycles.use_denoising = False
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = False
    world = bpy.data.worlds.new("W")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.93, 0.90, 0.85, 1)
    bg.inputs["Strength"].default_value = 0.9
    sc.world = world
    sun = bpy.data.lights.new("Sun", "SUN")
    sun.energy = 3.5
    sun.angle = math.radians(8)
    so = bpy.data.objects.new("Sun", sun)
    so.rotation_euler = (math.radians(50), math.radians(10), math.radians(35))
    sc.collection.objects.link(so)
    cam = bpy.data.cameras.new("Cam")
    if ortho_scale:
        cam.type = "ORTHO"
        cam.ortho_scale = ortho_scale
    co = bpy.data.objects.new("Cam", cam)
    co.location = cam_loc
    d = Vector(cam_target) - Vector(cam_loc)
    co.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    sc.collection.objects.link(co)
    sc.camera = co
    # suelo
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=400)
    for v in bm.verts:
        v.co.z = -0.25
    g = new_object("PreviewFloor", bm, [material("Stone")])
    return co


def render(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def hide_tree(root, hide):
    """Oculta o muestra en el render un objeto y todos sus descendientes."""
    stack = [root]
    while stack:
        o = stack.pop()
        o.hide_render = hide
        stack.extend(o.children)
