# Shared building blocks for the building scripts, on top of the vehicle kit (meshes, atlas, export).
# Units: metres, Z up, street side = -Y, ground at z = 0, pivot at the bottom centre of the lot.
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "vehicles", "common"))
from vehicle_kit import (ACCENT_MASK, BODY_MASK, FRAME_MASK, LOD1_RATIO, Kit, bake_atlas,  # noqa: E402
                         export_fbx, faces_outside_their_cell, joined_copy, material, reset_scene,
                         textured_material, triangles)

CELL = 8.0


class BuildingPalette:
    """Walls (R), steel structure (G) and trims (B) are recoloured by the game; the rest is fixed."""

    def __init__(self):
        self.wall = material("Wall", (0.92, 0.91, 0.88), BODY_MASK, gloss=0.2)
        self.steel = material("Steel", (0.80, 0.81, 0.80), FRAME_MASK, metallic=0.6, coat=0.2, gloss=0.5)
        self.trim = material("Trim", (0.32, 0.36, 0.34), ACCENT_MASK, metallic=0.3, gloss=0.4)
        self.timber = material("Timber", (0.70, 0.54, 0.36), gloss=0.15)
        self.concrete = material("Concrete", (0.66, 0.65, 0.62), gloss=0.1)
        self.paving = material("Paving", (0.52, 0.51, 0.49), gloss=0.1)
        self.dark = material("Dark", (0.10, 0.10, 0.11), gloss=0.2)
        self.glass = material("Glass", (0.30, 0.40, 0.45), coat=0.8, gloss=0.9)
        self.solar = material("Solar", (0.10, 0.14, 0.28), metallic=0.4, coat=0.9, gloss=0.9)
        self.sedum = material("Sedum", (0.42, 0.55, 0.28), gloss=0.05)
        self.leaves = material("Leaves", (0.26, 0.46, 0.22), gloss=0.05)
        self.bark = material("Bark", (0.36, 0.27, 0.20), gloss=0.05)
        self.cardboard = material("Cardboard", (0.78, 0.64, 0.44), gloss=0.1)
        self.pallet = material("Pallet", (0.74, 0.62, 0.45), gloss=0.1)
        self.safety = material("Safety", (0.92, 0.75, 0.15), gloss=0.3)
        self.light = material("Light", (1.0, 0.95, 0.80), gloss=0.8)
        self.mannequin = material("Mannequin", (0.62, 0.66, 0.70))

    def swatches(self):
        """Exported swatches, in atlas cell order (the mannequin is preview only)."""
        return [self.wall, self.steel, self.trim, self.timber, self.concrete, self.paving, self.dark,
                self.glass, self.solar, self.sedum, self.leaves, self.bark, self.cardboard, self.pallet,
                self.safety, self.light]


class BuildingKit(Kit):
    """Vehicle kit plus the shapes buildings need."""

    def slab(self, name, x0, x1, y0, y1, z0, z1, mat, export=True):
        """Axis-aligned box given by its bounds, easier to read than centre + size for buildings."""
        return self.box(name, (x1 - x0, y1 - y0, z1 - z0), ((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2),
                        mat, export=export)

    def tilted_panel(self, name, centre, size, tilt, mat):
        """Flat panel tilted around X (solar panels facing -Y when tilt > 0). Built at the origin:
        Kit.box applies its transform, so the mesh is centred there before rotating and moving it."""
        o = self.box(name, size, (0, 0, 0), mat)
        o.rotation_euler = (tilt, 0, 0)
        o.location = centre
        return o

    def tree(self, name, x, y, z, height, crown, p):
        self.tube(name + "Trunk", [(x, y, z), (x, y, z + height * 0.55)], 0.12, p.bark, sides=6)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=crown, location=(x, y, z + height * 0.7))
        bpy.context.object.name = name + "Crown"
        bpy.context.object.scale = (1, 1, 0.85)
        return self._keep(bpy.context.object, p.leaves)

    def text(self, name, body, size, location, mat, depth=0.05):
        """Raised letters facing -Y (the street)."""
        bpy.ops.object.text_add(location=location, rotation=(math.pi / 2, 0, 0))
        o = bpy.context.object
        o.name = name
        o.data.body = body
        o.data.size = size
        o.data.extrude = depth
        o.data.align_x = "CENTER"
        o.data.resolution_u = 2
        bpy.ops.object.convert(target="MESH")
        o = bpy.context.object
        return self._keep(o, mat)

    def mannequin_standing(self, x, y, p):
        """Grey 1.75 m figure for previews only."""
        self.tube("ScaleLegs", [(x, y, 0), (x, y, 0.9)], 0.12, p.mannequin, sides=6, export=False)
        self.tube("ScaleTorso", [(x, y, 0.9), (x, y, 1.5)], 0.18, p.mannequin, sides=6, export=False)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=0.12, location=(x, y, 1.65))
        self._keep(bpy.context.object, p.mannequin, export=False)


def import_preview_vehicle(path, location, rotation_z=0.0):
    """Imports a vehicle FBX for scale in the previews; never exported with the building."""
    before = set(bpy.data.objects)
    # The FBX holds game-space meshes (Y up, centimetres, see vehicle_kit.export_fbx): undo both here.
    bpy.ops.import_scene.fbx(filepath=path, axis_forward="-Z", axis_up="Y", global_scale=0.01)
    for o in set(bpy.data.objects) - before:
        if o.parent is None:
            o.location = location
            # Y up in the file: stand it back up on Blender's Z
            o.rotation_euler = (math.pi / 2, 0, rotation_z)
        if o.type == "MESH":
            o.data.materials.clear()
            o.data.materials.append(material("PreviewVehicle", (0.85, 0.85, 0.82)))


VIEWS = {"34": (1.0, -1.1, 0.75), "back": (-1.0, 1.1, 0.8), "top": (0.0, -0.05, 1.6)}


def setup_render(street_y=-26.0):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    shading = scene.display.shading
    shading.light = "STUDIO"
    shading.color_type = "MATERIAL"
    shading.show_shadows = True
    shading.show_cavity = True
    shading.cavity_type = "BOTH"
    shading.show_object_outline = True
    scene.render.resolution_x, scene.render.resolution_y = 1200, 800
    scene.world = bpy.data.worlds.new("World")
    bpy.ops.mesh.primitive_plane_add(size=240, location=(0, 0, -0.01))
    bpy.context.object.data.materials.append(material("Ground", (0.50, 0.55, 0.50)))
    # Street in front of the lot, for orientation only
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, street_y, 0.0))
    street = bpy.context.object
    street.scale = (120, 10, 1)
    street.data.materials.append(material("Street", (0.25, 0.25, 0.27)))


def render_view(path, target, offset, distance):
    scene = bpy.context.scene
    target = Vector(target)
    bpy.ops.object.camera_add(location=target + Vector(offset) * distance)
    camera = bpy.context.object
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 35
    scene.camera = camera
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def build(out_dir, name, make, target, distance, decorate=None, street_y=-26.0):
    """Building version of vehicle_kit.build: previews, atlas, LOD0 + LOD1 FBX, control mask preview."""
    reset_scene()
    palette = BuildingPalette()
    kit = BuildingKit()
    make(kit, palette)
    if decorate:
        decorate(kit, palette)

    setup_render(street_y)
    for view, offset in VIEWS.items():
        render_view(os.path.join(out_dir, f"{name}_preview_{view}.png"), target, offset, distance)

    lod0 = joined_copy(kit.exported, name)
    # Decimate before the atlas: a collapse across two colours would otherwise leave faces straddling
    # two atlas cells. Both levels then get their own projection into the same, identical textures.
    lod1 = lod0.copy()
    lod1.data = lod0.data.copy()
    lod1.name = lod1.data.name = f"{name}_LOD1"
    bpy.context.collection.objects.link(lod1)
    decimate = lod1.modifiers.new("decimate", "DECIMATE")
    decimate.ratio = LOD1_RATIO
    bpy.context.view_layer.objects.active = lod1
    bpy.ops.object.modifier_apply(modifier="decimate")
    control_mask = bake_atlas(lod0, palette.swatches(), name, out_dir)
    bake_atlas(lod1, palette.swatches(), name, out_dir)
    # Same material as LOD0 (the second bake made a duplicate named "<name>.001")
    lod1.data.materials.clear()
    lod1.data.materials.append(lod0.data.materials[0])
    export_fbx([lod0], os.path.join(out_dir, name + ".fbx"))
    export_fbx([lod1], os.path.join(out_dir, f"{name}_LOD1.fbx"))
    print("TRIANGLES", name, "LOD0", triangles(lod0), "LOD1", triangles(lod1))
    print("UV_CHECK", name, "LOD0 faces outside their cell", faces_outside_their_cell(lod0),
          "LOD1", faces_outside_their_cell(lod1))

    for o in bpy.data.objects:
        if o.type == "MESH" and o is not lod0 and not o.name.startswith(("Plane",)):
            o.hide_render = True
    lod0.data.materials.clear()
    lod0.data.materials.append(textured_material(name + "_MaskPreview", control_mask))
    bpy.context.scene.display.shading.color_type = "TEXTURE"
    render_view(os.path.join(out_dir, f"{name}_preview_mask.png"), target, VIEWS["34"], distance)
