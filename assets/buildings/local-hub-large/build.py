# Large Local Hub: an urban infill building for a dense district, inspired by the mixed-use street
# fronts of the Urban Promenades creator pack. Logistics ground floor on the street line (loading
# doors, cargo bike arcade, "Local Hub" shopfront), goods lifts down to an underground freight
# platform at -20 m, four storeys of flats and offices with planted balconies, a setback top floor
# with a rooftop terrace under a solar pergola. Blind party walls on both sides to sit between
# neighbours; a small back yard.
# Lot 4 x 4 cells (32 x 32 m): street at -Y, the facade is on the front lot line.
import math
import os
import sys

# No __pycache__ next to the shared kits in the repository.
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "common"))
import bpy  # noqa: E402
from building_kit import CELL, build, import_preview_vehicle, render_view  # noqa: E402

NAME = "SolarpunkLocalHubLarge01"

HALF_WIDTH = 2 * CELL                   # x from -16 to 16: party walls on both lot sides
FRONT, BACK = -2 * CELL, 2 * CELL       # y from -16 (street line) to 16
GROUND = 0.15
DEPTH_BACK = 6.0                        # building from the street line to y = 6, yard behind
GROUND_TOP = GROUND + 5.0               # tall logistics ground floor
STOREY = 3.2
UPPER_FLOORS = 3                        # full floors 1-3, then a setback attic floor
ATTIC_SETBACK = 2.5
ARCADE = (6.5, HALF_WIDTH - 0.6, 2.4)   # x0, x1, depth: cargo bike arcade under the first floor
LIFTS_X = (-9.0, -4.0)                  # goods lifts to the platform at -20 m, behind the loading doors
LIFT_Y = (1.0, 4.4)
TOTAL_TOP = GROUND_TOP + UPPER_FLOORS * STOREY


def make(kit, p):
    kit.slab("Lot", -HALF_WIDTH, HALF_WIDTH, FRONT, BACK, 0.0, GROUND, p.paving)
    volume(kit, p)
    ground_floor(kit, p)
    upper_facades(kit, p)
    balconies(kit, p)
    attic_and_roof(kit, p)
    back_yard(kit, p)


def volume(kit, p):
    """Main block: render walls (recoloured), full-height blind party walls, cornice bands."""
    x0, x1 = -HALF_WIDTH + 0.3, HALF_WIDTH - 0.3
    ax0, _, depth = ARCADE
    # The ground floor steps back over the cargo bike arcade; the floors above stay on the street line
    kit.slab("CoreGround", x0, ax0, FRONT + 0.4, DEPTH_BACK, GROUND, GROUND_TOP, p.wall)
    kit.slab("CoreGroundArcade", ax0, x1, FRONT + depth, DEPTH_BACK, GROUND, GROUND_TOP, p.wall)
    kit.slab("Core", x0, x1, FRONT + 0.4, DEPTH_BACK, GROUND_TOP, TOTAL_TOP, p.wall)
    # Vertical rhythm: pilasters every two bays on the upper floors
    for i in range(5):
        x = -14.4 - 0.8 + i * 7.2
        kit.slab(f"Pilaster{i}", x - 0.25, x + 0.25, FRONT + 0.15, FRONT + 0.4, GROUND_TOP, TOTAL_TOP, p.trim)
    for side in (-1, 1):
        a, b = sorted((side * HALF_WIDTH, side * (HALF_WIDTH - 0.3)))
        kit.slab(f"PartyWall{side}", a, b, FRONT, DEPTH_BACK, GROUND, TOTAL_TOP + 0.6, p.concrete)
    # Plinth band over the ground floor and a cornice on top: the horizontal lines of the street
    kit.slab("GroundBand", x0, x1, FRONT - 0.15, FRONT + 0.4, GROUND_TOP - 0.1, GROUND_TOP + 0.45, p.trim)
    kit.slab("Cornice", x0, x1, FRONT - 0.25, DEPTH_BACK + 0.25, TOTAL_TOP, TOTAL_TOP + 0.35, p.trim)


def ground_floor(kit, p):
    """Street line: two loading doors, the shopfront, then the cargo bike arcade."""
    y = FRONT
    z0, z1 = GROUND, GROUND_TOP - 0.1
    # Loading doors over the lifts: roll-up doors in safety-yellow frames
    for i, cx in enumerate(LIFTS_X):
        kit.slab(f"LoadingDoor{i}", cx - 2.0, cx + 2.0, y + 0.25, y + 0.4, z0, z0 + 3.8, p.steel)
        for k in range(5):
            z = z0 + 0.6 + k * 0.7
            kit.slab(f"DoorRib{i}_{k}", cx - 2.0, cx + 2.0, y + 0.2, y + 0.25, z, z + 0.06, p.trim)
        kit.slab(f"DoorFrame{i}", cx - 2.25, cx + 2.25, y + 0.15, y + 0.25, z0 + 3.8, z0 + 4.1, p.safety)
        for side in (-1, 1):
            fx = cx + side * 2.1
            kit.slab(f"DoorJamb{i}{side}", fx - 0.15, fx + 0.15, y + 0.15, y + 0.25, z0, z0 + 3.8, p.safety)
        kit.slab(f"DoorLamp{i}", cx - 0.3, cx + 0.3, y + 0.05, y + 0.15, z0 + 4.25, z0 + 4.45, p.light)
    for x in (-HALF_WIDTH + 0.3, -1.4, ARCADE[0] - 0.3):
        kit.slab(f"Pier{int(x * 10)}", x - 0.3, x + 0.3, y, y + 0.4, z0, GROUND_TOP - 0.1, p.trim)
    # Shopfront between the doors and the arcade: timber mullions, glazing, raised sign
    sx0, sx1 = -1.0, ARCADE[0] - 0.5
    kit.slab("Shopfront", sx0, sx1, y + 0.3, y + 0.4, z0 + 0.4, z1 - 1.25, p.glass)
    kit.slab("ShopSill", sx0, sx1, y + 0.2, y + 0.45, z0, z0 + 0.4, p.timber)
    for i in range(5):
        x = sx0 + i * (sx1 - sx0) / 4
        kit.slab(f"ShopMullion{i}", x - 0.1, x + 0.1, y + 0.2, y + 0.3, z0, z1 - 0.3, p.timber)
    kit.slab("ShopFascia", sx0 - 0.2, sx1 + 0.2, y + 0.1, y + 0.3, z1 - 1.25, z1 - 0.3, p.timber)
    kit.text("Sign", "LOCAL HUB", 0.6, ((sx0 + sx1) / 2, y + 0.05, z1 - 1.05), p.wall, depth=0.06)
    # Fabric awning over the shopfront, as on the promenade buildings
    kit.slab("Awning", sx0 - 0.2, sx1 + 0.2, y - 1.6, y + 0.2, z0 + 3.2, z0 + 3.35, p.trim)
    # Cargo bike arcade: the facade steps back under the first floor; stands, ramp edge, columns
    ax0, ax1, depth = ARCADE
    kit.slab("ArcadeVoid", ax0, ax1, y, y + depth, GROUND, GROUND + 0.02, p.dark)
    kit.slab("ArcadeBackWall", ax0, ax1, y + depth, y + depth + 0.1, GROUND, GROUND_TOP, p.timber)
    kit.slab("ArcadeGlass", ax0 + 0.5, ax1 - 0.5, y + depth - 0.05, y + depth, GROUND + 0.3, GROUND_TOP - 1.0, p.glass)
    for i in range(3):
        x = ax0 + 0.2 + i * (ax1 - ax0 - 0.4) / 2
        kit.slab(f"ArcadeColumn{i}", x - 0.2, x + 0.2, y + 0.1, y + 0.5, GROUND, GROUND_TOP, p.concrete)
    for i in range(5):
        x = ax0 + 1.0 + i * 1.6
        kit.tube(f"BikeStand{i}", [(x, y + 0.7, GROUND), (x, y + 0.7, GROUND + 0.75),
                                   (x, y + 1.5, GROUND + 0.75), (x, y + 1.5, GROUND)], 0.03, p.trim, sides=4)
    # Street planter on the lot edge, clear of the loading doors
    kit.slab("Planter", -14.6, -12.4, y - 1.1, y - 0.1, GROUND, GROUND + 0.55, p.timber)
    kit.tree("StreetTree", -13.5, y - 0.6, GROUND + 0.55, 4.5, 1.2, p)


def windows(kit, p, prefix, y, x_ranges, z0, z1, facing):
    """A row of windows with timber frames on a facade at depth y; facing -1 for the street."""
    for i, (a, b) in enumerate(x_ranges):
        kit.slab(f"{prefix}Glass{i}", a, b, y + facing * 0.02, y + facing * 0.08, z0, z1, p.glass)
        kit.slab(f"{prefix}Lintel{i}", a - 0.15, b + 0.15, y + facing * 0.05, y + facing * 0.15, z1, z1 + 0.15, p.timber)


def upper_facades(kit, p):
    """Window rhythm on the street and back facades, floors 1 to 3."""
    bays = [(-14.4 + i * 3.6, -14.4 + i * 3.6 + 2.0) for i in range(8)]
    for floor in range(UPPER_FLOORS):
        z0 = GROUND_TOP + floor * STOREY + 0.6
        windows(kit, p, f"Street{floor}", FRONT + 0.4, bays, z0, z0 + 1.9, -1)
        windows(kit, p, f"Back{floor}", DEPTH_BACK, bays[1:-1], z0, z0 + 1.6, 1)


def balconies(kit, p):
    """Alternating planted balconies on the street facade: slab, timber railing, planter, shrubs."""
    for floor in range(UPPER_FLOORS):
        z = GROUND_TOP + floor * STOREY
        offset = 0 if floor % 2 == 0 else 1
        for i in range(offset, 8, 2):
            a = -14.4 + i * 3.6 - 0.5
            b = a + 3.0
            y0, y1 = FRONT - 1.3, FRONT + 0.4
            kit.slab(f"Balcony{floor}_{i}", a, b, y0, y1, z, z + 0.2, p.concrete)
            kit.slab(f"BalconyRail{floor}_{i}", a, b, y0, y0 + 0.08, z + 0.2, z + 1.1, p.timber)
            kit.slab(f"BalconyPlanter{floor}_{i}", a + 0.1, b - 0.1, y0 + 0.1, y0 + 0.5, z + 0.2, z + 0.65, p.timber)
            kit.slab(f"BalconyGreen{floor}_{i}", a + 0.15, b - 0.15, y0 + 0.12, y0 + 0.48, z + 0.65, z + 0.95, p.leaves)


def attic_and_roof(kit, p):
    """Setback attic floor with a rooftop terrace, sedum roof, solar pergola, lift heads."""
    z0 = TOTAL_TOP + 0.35
    ay0 = FRONT + 0.4 + ATTIC_SETBACK
    x0, x1 = -HALF_WIDTH + 0.3, HALF_WIDTH - 0.3
    kit.slab("Terrace", x0, x1, FRONT + 0.4, ay0, z0 - 0.05, z0 + 0.05, p.timber)
    kit.slab("TerraceRail", x0, x1, FRONT - 0.2, FRONT + 0.4, z0, z0 + 1.0, p.glass)
    kit.slab("Attic", x0 + 1.0, x1 - 1.0, ay0, DEPTH_BACK - 0.5, z0, z0 + STOREY, p.timber)
    attic_bays = [(-12.6 + i * 3.6, -12.6 + i * 3.6 + 2.4) for i in range(7)]
    windows(kit, p, "Attic", ay0, attic_bays, z0 + 0.3, z0 + 2.6, -1)
    roof = z0 + STOREY
    kit.slab("AtticRoof", x0 + 0.8, x1 - 0.8, ay0 - 0.2, DEPTH_BACK - 0.3, roof, roof + 0.25, p.sedum)
    for side in (-1, 1):
        kit.slab(f"TerracePlanter{side}", side * 13.0 - 1.4, side * 13.0 + 1.4, FRONT + 0.6, FRONT + 1.6,
                 z0, z0 + 0.6, p.timber)
        kit.tree(f"TerraceTree{side}", side * 13.0, FRONT + 1.1, z0 + 0.6, 3.2, 0.9, p)
    # Solar pergola over the terrace and the attic roof: light steel frame, panels tilted to the street
    pz = roof + 1.6
    for i in range(5):
        x = -12.0 + i * 6.0
        kit.slab(f"PergolaPost{i}", x - 0.1, x + 0.1, ay0 + 0.5, ay0 + 0.7, roof, pz, p.steel)
    kit.slab("PergolaBeam", x0 + 1.5, x1 - 1.5, ay0 + 0.5, ay0 + 0.7, pz - 0.2, pz, p.steel)
    tilt = math.radians(12)
    for row in range(3):
        y = ay0 + 1.6 + row * 3.0
        for i in range(10):
            x = -12.6 + i * 2.8
            kit.tilted_panel(f"Solar{row}_{i}", (x, y, pz + 0.35), (2.6, 2.8, 0.06), tilt, p.solar)
    for i, cx in enumerate(LIFTS_X):
        kit.slab(f"LiftHead{i}", cx - 1.7, cx + 1.7, LIFT_Y[0], LIFT_Y[1], roof, roof + 1.4, p.concrete)


def back_yard(kit, p):
    """Yard behind the block: covered cargo bike shelter, a tree, bins enclosure."""
    y0, y1 = DEPTH_BACK + 1.5, BACK - 1.0
    kit.slab("ShelterRoof", -12.0, 0.0, y0, y1, GROUND + 2.6, GROUND + 2.75, p.sedum)
    for x in (-12.0, -6.0, 0.0):
        for y in (y0, y1):
            kit.slab(f"ShelterPost{int(x)}_{int(y)}", x - 0.1, x + 0.1, y - 0.1, y + 0.1, GROUND, GROUND + 2.6, p.steel)
    kit.slab("YardPlanter", 4.0, 7.0, y0, y0 + 3.0, GROUND, GROUND + 0.5, p.timber)
    kit.tree("YardTree", 5.5, y0 + 1.5, GROUND + 0.5, 6.0, 1.8, p)
    kit.slab("BinsEnclosure", 9.0, 14.0, y1 - 3.0, y1, GROUND, GROUND + 1.6, p.timber)


def scale_props(kit, p):
    kit.mannequin_standing(9.0, FRONT + 1.2, p)
    import_preview_vehicle(os.path.join(HERE, "..", "..", "vehicles", "cargo-bike-box", "SolarpunkCargoBikeBox01.fbx"),
                           (11.5, FRONT + 1.2, GROUND), rotation_z=math.pi / 2)
    # Sidewalk in front of the street line, preview only
    kit.slab("Sidewalk", -60, 60, FRONT - 4.0, FRONT, 0.0, 0.12, p.paving, export=False)


def situation(kit, p):
    """Grey mid-rise neighbours on both party walls, to judge how the block fits a street front."""
    for side, height, depth in ((-1, 17.0, 14.0), (1, 20.0, 18.0)):
        x0, x1 = sorted((side * HALF_WIDTH, side * (HALF_WIDTH + 24.0)))
        kit.slab(f"Neighbour{side}", x0, x1, FRONT, FRONT + depth, 0.0, height, p.mannequin, export=False)


if __name__ == "__main__":
    target, distance = (0, -4, 8), 52
    build(HERE, NAME, make, target=target, distance=distance, decorate=scale_props, street_y=FRONT - 9.0)
    # Street-front preview with neighbours: show every preview-only mesh again, colours by material
    for o in bpy.data.objects:
        if o.type == "MESH":
            o.hide_render = o.name == NAME or o.name.startswith(NAME + "_LOD1")
    bpy.context.scene.display.shading.color_type = "MATERIAL"
    from building_kit import BuildingKit, BuildingPalette  # noqa: E402
    situation(BuildingKit(), BuildingPalette())
    render_view(os.path.join(HERE, f"{NAME}_preview_situation.png"), (0, -6, 9), (0.55, -1.2, 0.55), 70)
