# Shared building blocks for the last-mile vehicle scripts.
# Units: metres, Z up, front = -Y. Wheels touch z = 0.
import math
import os

import bmesh
import bpy
import numpy
from mathutils import Vector

# Texture atlas: one flat-colour cell per palette swatch, 256 px per cell (4 x 4 cells in 1024 px,
# 8 x 8 in 2048 px when a palette has more than 16 swatches). Each face is projected into the
# central 15 % of its cell, so mipmaps only blend neighbouring cells at the very last levels.
CELL_PIXELS = 256
UV_SPAN = 0.15
LOD1_RATIO = 0.35

# _ControlMask channels: colour mask 1, 2, 3 (Paradox wiki, Asset Pipeline: Buildings).
BODY_MASK = (1.0, 0.0, 0.0)
FRAME_MASK = (0.0, 1.0, 0.0)
ACCENT_MASK = (0.0, 0.0, 1.0)
NO_MASK = (0.0, 0.0, 0.0)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name, rgb, mask=NO_MASK, metallic=0.0, coat=0.0, gloss=0.3):
    """Preview colour, plus what the swatch bakes into the texture atlas."""
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    m["control_mask"] = mask
    # _MaskMap: R metallic, G coat, B unused (black), A glossiness.
    m["mask_map"] = (metallic, coat, 0.0, gloss)
    return m


class Palette:
    """
    Neutral colours: the game recolours body / frame / accent through the control mask. Kept fairly
    matte: glossy or coated swatches mirror the sky and streak in the game's screen-space reflections.
    """

    def __init__(self):
        self.body = material("Body", (0.93, 0.93, 0.91), BODY_MASK, gloss=0.3)
        self.trim = material("Trim", (0.30, 0.31, 0.32), ACCENT_MASK, gloss=0.25)
        self.frame = material("Frame", (0.85, 0.86, 0.86), FRAME_MASK, metallic=0.2, gloss=0.35)
        self.dark = material("Dark", (0.08, 0.08, 0.09), gloss=0.2)
        self.rim = material("Rim", (0.55, 0.56, 0.58), metallic=0.6, gloss=0.4)
        self.glass = material("Glass", (0.28, 0.36, 0.40), gloss=0.6)
        self.light = material("Light", (1.0, 0.95, 0.75), gloss=0.5)
        self.amber = material("Amber", (0.95, 0.55, 0.10), gloss=0.5)
        self.red = material("Red", (0.75, 0.08, 0.06), gloss=0.5)
        self.wood = material("Wood", (0.72, 0.58, 0.40), gloss=0.1)
        self.cardboard = material("Cardboard", (0.78, 0.64, 0.44), gloss=0.1)
        self.canvas = material("Canvas", (0.88, 0.88, 0.84), BODY_MASK, gloss=0.15)
        self.mannequin = material("Mannequin", (0.62, 0.66, 0.70))

    def swatches(self):
        """Exported swatches, in atlas cell order (the mannequin is preview only)."""
        return [self.body, self.trim, self.frame, self.dark, self.rim, self.glass, self.light,
                self.amber, self.red, self.wood, self.cardboard, self.canvas]


class Kit:
    """Creates meshes and remembers which ones belong to the exported vehicle."""

    def __init__(self):
        self.exported = []
        self.preview_only = []

    def _keep(self, o, mat, export=True):
        o.data.materials.append(mat)
        (self.exported if export else self.preview_only).append(o)
        return o

    def box(self, name, size, loc, mat, bevel=0.0, segments=2, export=True):
        bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
        o = bpy.context.object
        o.name = name
        o.scale = size
        bpy.ops.object.transform_apply(scale=True)
        if bevel:
            mod = o.modifiers.new("bevel", "BEVEL")
            mod.width = bevel
            mod.segments = segments
            bpy.ops.object.modifier_apply(modifier="bevel")
        return self._keep(o, mat, export)

    def cylinder_x(self, name, radius, depth, loc, mat, vertices=16):
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                            location=loc, rotation=(0, math.pi / 2, 0))
        o = bpy.context.object
        o.name = name
        return self._keep(o, mat)

    def torus_x(self, name, major, minor, loc, mat, major_segments=18, minor_segments=6):
        bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor,
                                         major_segments=major_segments, minor_segments=minor_segments,
                                         location=loc, rotation=(0, math.pi / 2, 0))
        o = bpy.context.object
        o.name = name
        return self._keep(o, mat)

    def tube(self, name, points, radius, mat, sides=6, export=True):
        """Tube through points, smoothed by a low-resolution bezier."""
        curve = bpy.data.curves.new(name, "CURVE")
        curve.dimensions = "3D"
        curve.bevel_depth = radius
        curve.bevel_resolution = max(0, sides // 4 - 1)
        curve.resolution_u = 3
        curve.use_fill_caps = True
        spline = curve.splines.new("BEZIER")
        spline.bezier_points.add(len(points) - 1)
        for point, co in zip(spline.bezier_points, points):
            point.co = co
            point.handle_left_type = point.handle_right_type = "AUTO"
        o = bpy.data.objects.new(name, curve)
        bpy.context.collection.objects.link(o)
        bpy.context.view_layer.objects.active = o
        o.select_set(True)
        bpy.ops.object.convert(target="MESH")
        o = bpy.context.object
        o.select_set(False)
        return self._keep(o, mat, export)

    def prism_x(self, name, profile, width, mat, x=0.0, bevel=0.0):
        """Extrudes a side profile [(y, z), ...] across X: cabins, rounded noses."""
        mesh = bpy.data.meshes.new(name)
        bm = bmesh.new()
        half = width / 2
        left = [bm.verts.new((x - half, y, z)) for y, z in profile]
        right = [bm.verts.new((x + half, y, z)) for y, z in profile]
        bm.faces.new(left[::-1])
        bm.faces.new(right)
        count = len(profile)
        for i in range(count):
            j = (i + 1) % count
            bm.faces.new((left[i], left[j], right[j], right[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(mesh)
        bm.free()
        o = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(o)
        if bevel:
            bpy.context.view_layer.objects.active = o
            mod = o.modifiers.new("bevel", "BEVEL")
            mod.width = bevel
            mod.segments = 2
            mod.limit_method = "ANGLE"
            bpy.ops.object.modifier_apply(modifier="bevel")
        return self._keep(o, mat)

    def arc_guard(self, name, radius, loc, keep_if, mat, minor=0.022):
        """Part of a ring around a wheel: mudguards and wheel arches."""
        bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=minor, major_segments=24,
                                         minor_segments=4, location=loc, rotation=(0, math.pi / 2, 0))
        o = bpy.context.object
        o.name = name
        bm = bmesh.new()
        bm.from_mesh(o.data)
        doomed = [v for v in bm.verts if not keep_if(o.matrix_world @ v.co)]
        bmesh.ops.delete(bm, geom=doomed, context="VERTS")
        bm.to_mesh(o.data)
        bm.free()
        return self._keep(o, mat)

    def wheel(self, palette, name, radius, width, x, y, spokes=0, tyre=0.03):
        """Tyre + rim + hub; bicycle wheels get a thin rim and spokes, small wheels a disc."""
        z = radius
        self.torus_x(name + "_Tire", radius - tyre, tyre, (x, y, z), palette.dark)
        if spokes:
            self.torus_x(name + "_Rim", radius - tyre - 0.035, 0.014, (x, y, z), palette.rim,
                         major_segments=18, minor_segments=4)
            for i in range(spokes):
                a = math.pi * 2 * i / spokes
                tip = (x, y + math.cos(a) * (radius - 0.06), z + math.sin(a) * (radius - 0.06))
                self.tube(f"{name}_Spoke{i}", [(x, y, z), tip], 0.006, palette.dark, sides=4)
        else:
            self.cylinder_x(name + "_Rim", radius - tyre - 0.02, width * 0.55, (x, y, z), palette.rim)
        self.cylinder_x(name + "_Hub", 0.035, width * 0.9, (x, y, z), palette.dark, vertices=8)

    def anchor(self, name, location):
        """Empty marking where the vanilla citizen sits (ActivityLocation)."""
        bpy.ops.object.empty_add(type="PLAIN_AXES", location=location)
        bpy.context.object.name = name


def mannequin(kit, palette, hip, shoulder, hands, feet, head_offset=Vector((0, -0.04, 0.22))):
    """Grey scale figure for previews only, standing in for the vanilla citizen."""
    hip, shoulder = Vector(hip), Vector(shoulder)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.11,
                                         location=shoulder + head_offset)
    kit._keep(bpy.context.object, palette.mannequin, export=False)
    kit.tube("Mannequin_Torso", [tuple(hip), tuple(shoulder)], 0.12, palette.mannequin, sides=8, export=False)
    for side, hand in zip((1, -1), hands):
        kit.tube(f"Mannequin_Arm{side}", [tuple(shoulder + Vector((side * 0.18, 0, 0))), hand],
                 0.04, palette.mannequin, sides=8, export=False)
    for side, (knee, foot) in zip((1, -1), feet):
        kit.tube(f"Mannequin_Leg{side}", [tuple(hip + Vector((side * 0.10, 0, 0))), knee, foot],
                 0.055, palette.mannequin, sides=8, export=False)


def bike_drivetrain(kit, p, bottom_bracket, seat_top, seat_post):
    """Seat post, saddle, chainring, hub motor, cranks and pedals shared by the bicycles."""
    kit.tube("SeatPost", [seat_top, seat_post], 0.016, p.dark)
    kit.box("Saddle", (0.15, 0.27, 0.06), (0, seat_post[1] + 0.03, seat_post[2] + 0.03), p.dark, bevel=0.02)
    bb_y, bb_z = bottom_bracket[1], bottom_bracket[2]
    kit.cylinder_x("Chainring", 0.10, 0.012, (0.05, bb_y, bb_z), p.dark, vertices=14)
    kit.cylinder_x("Motor", 0.07, 0.10, (0, bb_y, bb_z), p.dark, vertices=10)
    for side in (-1, 1):
        crank_end = (side * 0.09, bb_y + side * 0.12, bb_z - side * 0.06)
        kit.tube(f"Crank_{side}", [(side * 0.08, bb_y, bb_z), crank_end], 0.012, p.dark)
        kit.box(f"Pedal_{side}", (0.09, 0.06, 0.02), (side * 0.13, crank_end[1], crank_end[2]), p.dark)


def bike_rider(kit, p, bottom_bracket, seat_post, shoulder, hands):
    """Rider anchor on the saddle, and the preview mannequin pedalling from it."""
    bb_y, bb_z = bottom_bracket[1], bottom_bracket[2]
    kit.anchor("RiderAnchor", (0, seat_post[1] + 0.02, seat_post[2] + 0.07))
    mannequin(kit, p,
              hip=(0, seat_post[1] + 0.02, seat_post[2] + 0.08),
              shoulder=shoulder,
              hands=hands,
              feet=[((0.12, bb_y - 0.22, 0.78), (0.13, bb_y + 0.12, bb_z - 0.04)),
                    ((-0.12, bb_y - 0.05, 0.68), (-0.13, bb_y - 0.12, bb_z + 0.08))])


VIEWS = {"34": (3.0, -3.2, 2.0), "side": (4.6, 0.0, 0.9), "back": (-2.6, 3.2, 1.8)}


def setup_render():
    """Workbench scene with a ground plane, lit like the game's asset previews."""
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    shading = scene.display.shading
    shading.light = "STUDIO"
    shading.color_type = "MATERIAL"
    shading.show_shadows = True
    shading.show_cavity = True
    shading.cavity_type = "BOTH"
    shading.show_object_outline = True
    scene.render.resolution_x, scene.render.resolution_y = 960, 600
    scene.world = bpy.data.worlds.new("World")
    bpy.ops.mesh.primitive_plane_add(size=20, location=(0, 0, 0))
    bpy.context.object.data.materials.append(material("Ground", (0.50, 0.55, 0.50)))


def render_view(path, target, offset, distance):
    scene = bpy.context.scene
    target = Vector(target)
    bpy.ops.object.camera_add(location=target + Vector(offset) * distance)
    camera = bpy.context.object
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 38
    scene.camera = camera
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def joined_copy(objects, name):
    """One mesh object holding a copy of every exported part, at the origin."""
    mesh = bpy.data.meshes.new(name)
    joined = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(joined)
    copies = []
    for o in objects:
        copy = o.copy()
        copy.data = o.data.copy()
        bpy.context.collection.objects.link(copy)
        copies.append(copy)
    bpy.ops.object.select_all(action="DESELECT")
    for copy in copies:
        copy.select_set(True)
    joined.select_set(True)
    bpy.context.view_layer.objects.active = joined
    bpy.ops.object.join()
    return joined


def bake_atlas(joined, swatches, name, out_dir):
    """
    Flat-colour atlas: each swatch gets one cell, every face of that swatch is planar-projected
    inside it. Writes _BaseColor, _ControlMask, _MaskMap and a flat _Normal, then leaves the mesh
    with a single material named after the asset.
    """
    cell_of = {m.name: i for i, m in enumerate(swatches)}
    slot_cell = [cell_of[m.name] if m else None for m in joined.data.materials]
    cells = 4 if len(swatches) <= 16 else 8
    size = cells * CELL_PIXELS
    cell_size = 1.0 / cells

    bm = bmesh.new()
    bm.from_mesh(joined.data)
    uv = bm.loops.layers.uv.verify()
    lowest = Vector([min(v.co[i] for v in bm.verts) for i in range(3)])
    extent = max(max(v.co[i] for v in bm.verts) - lowest[i] for i in range(3))
    for face in bm.faces:
        cell = slot_cell[face.material_index]
        if cell is None:
            raise ValueError(f"{name}: face without a palette material (slot {face.material_index})")
        centre_u = ((cell % cells) + 0.5) * cell_size
        centre_v = ((cell // cells) + 0.5) * cell_size
        # Planar projection on the face's dominant axis: keeps every face a non-zero UV area
        axis = max(range(3), key=lambda i: abs(face.normal[i]))
        a, b = [i for i in range(3) if i != axis]
        for loop in face.loops:
            co = (loop.vert.co - lowest) / extent
            loop[uv].uv = (centre_u + cell_size * UV_SPAN * (co[a] - 0.5),
                           centre_v + cell_size * UV_SPAN * (co[b] - 0.5))
        face.material_index = 0
    bm.to_mesh(joined.data)
    bm.free()
    joined.data["atlas_cells"] = cells

    def save(suffix, colour_of, alpha_of=lambda m: 1.0):
        pixels = numpy.zeros((size, size, 4), dtype=numpy.float32)
        pixels[:, :, 3] = 1.0
        for i, m in enumerate(swatches):
            x, y = (i % cells) * CELL_PIXELS, (i // cells) * CELL_PIXELS
            step = CELL_PIXELS
            pixels[y:y + step, x:x + step, :3] = colour_of(m)
            pixels[y:y + step, x:x + step, 3] = alpha_of(m)
        image = bpy.data.images.new(f"{name}{suffix}", size, size, alpha=True)
        if suffix != "_BaseColor":
            image.colorspace_settings.name = "Non-Color"
        image.pixels.foreach_set(pixels.ravel())
        image.filepath_raw = os.path.join(out_dir, f"{name}{suffix}.png")
        image.file_format = "PNG"
        image.save()
        return image

    base_colour = save("_BaseColor", lambda m: (0.85, 0.85, 0.85) if tuple(m["control_mask"]) != NO_MASK
                       else tuple(m.diffuse_color)[:3])
    # Alpha: snow removal on buildings [S for vehicles]; left opaque.
    control_mask = save("_ControlMask", lambda m: tuple(m["control_mask"]))
    save("_MaskMap", lambda m: tuple(m["mask_map"])[:3], lambda m: m["mask_map"][3])
    save("_Normal", lambda m: (0.5, 0.5, 1.0))

    joined.data.materials.clear()
    joined.data.materials.append(textured_material(name, base_colour))
    return control_mask


def faces_outside_their_cell(o):
    """Faces whose UVs do not all fall in one atlas cell (mip bleeding). Zero-area UVs count too."""
    cells = o.data["atlas_cells"]
    bm = bmesh.new()
    bm.from_mesh(o.data)
    uv = bm.loops.layers.uv.verify()
    bad = 0
    for face in bm.faces:
        uvs = [loop[uv].uv.copy() for loop in face.loops]
        if len({(int(u * cells), int(v * cells)) for u, v in uvs}) != 1:
            bad += 1
            continue
        area = 0.0
        for i in range(1, len(uvs) - 1):
            area += abs((uvs[i] - uvs[0]).cross(uvs[i + 1] - uvs[0])) / 2
        if area <= 0.0:
            bad += 1
    bm.free()
    return bad


def textured_material(name, image):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    node = m.node_tree.nodes.new("ShaderNodeTexImage")
    node.image = image
    m.node_tree.nodes.active = node
    m.node_tree.links.new(node.outputs["Color"], m.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    return m


def export_fbx(objects, path):
    """The game's importer reads raw vertices: bake its axes (Y up) and units (centimetres) into the
    meshes for the export, as the CS2-Exporter-for-Blender add-on does, then undo it for the previews."""
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bake_game_space(objects, math.radians(-90), 100)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, global_scale=1.0, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", object_types={"MESH"}, add_leaf_bones=False,
                             axis_forward="-Z", axis_up="Y", path_mode="STRIP")
    bake_game_space(objects, math.radians(90), 0.01, undo=True)


def bake_game_space(objects, angle, scale, undo=False):
    steps = [("scale", scale), ("rotation", angle)] if undo else [("rotation", angle), ("scale", scale)]
    for step, value in steps:
        for o in objects:
            if step == "rotation":
                o.rotation_euler = (value, 0, 0)
            else:
                o.scale = (value, value, value)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def triangles(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)


def build(out_dir, name, make, target, distance=1.0):
    """
    Runs a vehicle script: model, colour previews, then the importer set: one joined mesh with a
    texture atlas (LOD0) and a decimated copy (LOD1), each in its own FBX, plus a control mask preview.
    """
    reset_scene()
    palette = Palette()
    kit = Kit()
    make(kit, palette)

    setup_render()
    for view, offset in VIEWS.items():
        render_view(os.path.join(out_dir, f"{name}_preview_{view}.png"), target, offset, distance)

    lod0 = joined_copy(kit.exported, name)
    control_mask = bake_atlas(lod0, palette.swatches(), name, out_dir)
    export_fbx([lod0], os.path.join(out_dir, name + ".fbx"))

    lod1 = lod0.copy()
    lod1.data = lod0.data.copy()
    lod1.name = lod1.data.name = f"{name}_LOD1"
    bpy.context.collection.objects.link(lod1)
    decimate = lod1.modifiers.new("decimate", "DECIMATE")
    decimate.ratio = LOD1_RATIO
    bpy.context.view_layer.objects.active = lod1
    bpy.ops.object.modifier_apply(modifier="decimate")
    export_fbx([lod1], os.path.join(out_dir, f"{name}_LOD1.fbx"))
    print("TRIANGLES", name, "LOD0", triangles(lod0), "LOD1", triangles(lod1))
    print("UV_CHECK", name, "LOD0 faces outside their cell", faces_outside_their_cell(lod0),
          "LOD1", faces_outside_their_cell(lod1))

    # Mask preview: only the textured LOD0, coloured by its control mask
    for o in kit.exported + kit.preview_only + [lod1]:
        o.hide_render = True
    lod0.data.materials.clear()
    lod0.data.materials.append(textured_material(name + "_MaskPreview", control_mask))
    bpy.context.scene.display.shading.color_type = "TEXTURE"
    render_view(os.path.join(out_dir, f"{name}_preview_mask.png"), target, VIEWS["34"], distance)
