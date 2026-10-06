# Local Hub: neighbourhood logistics hub. Goods come in by rail on the back platform, shops fetch
# them from the street side with cargo bikes and small electric vehicles.
# Lot 4 x 5 cells (32 x 40 m): street at -Y, rail platform along +Y; the track itself is a sub-net.
import math
import os
import sys

# No __pycache__ next to the shared kits in the repository.
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "common"))
from building_kit import CELL, build, import_preview_vehicle  # noqa: E402

NAME = "SolarpunkLocalHub01"

HALF_WIDTH = 2 * CELL           # x from -16 to 16
FRONT, BACK = -2.5 * CELL, 2.5 * CELL   # y from -20 (street) to 20
GROUND = 0.15
DOCK = 1.10                     # wagon floor height: hall floor and platforms
HALL_X = 11.0
HALL_FRONT, HALL_BACK = -11.0, 7.0
WALL_TOP = DOCK + 6.0
TOOTH_RISE = 2.0
PLATFORM_EDGE = 11.0            # track axis at y = 14
CANOPY_BACK = 19.0
CANOPY_Z = WALL_TOP + 0.6


def make(kit, p):
    ground_and_dock(kit, p)
    hall(kit, p)
    sawtooth_roof(kit, p)
    rail_canopy(kit, p)
    street_front(kit, p)
    goods(kit, p)


def ground_and_dock(kit, p):
    kit.slab("Lot", -HALF_WIDTH, HALF_WIDTH, FRONT, BACK, 0.0, GROUND, p.paving)
    kit.slab("HallFloor", -HALL_X, HALL_X, HALL_FRONT - 1.0, HALL_BACK, GROUND, DOCK, p.concrete)
    kit.slab("Platform", -HALF_WIDTH, HALF_WIDTH, HALL_BACK, PLATFORM_EDGE, GROUND, DOCK, p.concrete)
    kit.slab("PlatformEdge", -HALF_WIDTH, HALF_WIDTH, PLATFORM_EDGE - 0.4, PLATFORM_EDGE, DOCK, DOCK + 0.02, p.safety)
    # Ramp from the street up to the dock, wide enough for cargo bikes side by side
    kit.prism_x("Ramp", [(HALL_FRONT - 6.0, GROUND), (HALL_FRONT - 1.0, DOCK), (HALL_FRONT - 1.0, GROUND)],
                16.0, p.concrete)
    for side in (-1, 1):
        x = side * 8.1
        kit.tube(f"RampRail{side}", [(x, HALL_FRONT - 6.0, GROUND + 0.9), (x, HALL_FRONT - 1.0, DOCK + 0.9)],
                 0.04, p.trim, sides=4)
        inner, outer = sorted((side * 8.2, side * HALL_X))
        kit.slab(f"Steps{side}", inner, outer, HALL_FRONT - 2.0, HALL_FRONT - 1.0, GROUND, DOCK, p.concrete)


def hall(kit, p):
    z0, z1 = DOCK, WALL_TOP
    # Side walls: white render, lower half planted (green wall)
    for side in (-1, 1):
        x0, x1 = (side * HALL_X, side * (HALL_X - 0.3)) if side < 0 else (side * (HALL_X - 0.3), side * HALL_X)
        kit.slab(f"SideWall{side}", x0, x1, HALL_FRONT, HALL_BACK, z0, z1, p.wall)
        outer = side * (HALL_X + 0.05)
        kit.slab(f"GreenWall{side}", min(outer, side * HALL_X), max(outer, side * HALL_X),
                 HALL_FRONT + 2.0, HALL_BACK - 2.0, z0 + 0.3, z0 + 3.2, p.sedum)
    # Front facade: timber cladding between three loading doors, glazed band above
    kit.slab("FrontWall", -HALL_X, HALL_X, HALL_FRONT, HALL_FRONT + 0.3, z0, z1, p.wall)
    doors = (-6.5, 0.0, 6.5)
    for i, x in enumerate(doors):
        kit.slab(f"Door{i}", x - 2.0, x + 2.0, HALL_FRONT - 0.06, HALL_FRONT, z0, z0 + 4.0, p.dark)
        kit.slab(f"DoorFrame{i}", x - 2.2, x + 2.2, HALL_FRONT - 0.12, HALL_FRONT - 0.04, z0 + 4.0, z0 + 4.25, p.trim)
    for i, x in enumerate((-9.6, -3.25, 3.25, 9.6)):
        width = 2.2 if abs(x) > 9 else 4.1
        kit.slab(f"Cladding{i}", x - width / 2, x + width / 2, HALL_FRONT - 0.08, HALL_FRONT, z0, z0 + 4.25, p.timber)
    kit.slab("GlassBand", -HALL_X + 0.4, HALL_X - 0.4, HALL_FRONT - 0.05, HALL_FRONT, z0 + 4.6, z1 - 0.8, p.glass)
    # Fascia above the green awning, carrying the name
    kit.slab("Fascia", -HALL_X, HALL_X, HALL_FRONT - 0.15, HALL_FRONT, z1 - 0.8, z1 + 0.3, p.trim)
    kit.text("SignFacade", "LOCAL HUB", 0.75, (0, HALL_FRONT - 0.22, z1 - 0.6), p.wall, depth=0.06)
    # Exposed steel portal frames on the facade
    for i, x in enumerate((-HALL_X, -3.25 - 2.05, 3.25 + 2.05, HALL_X)):
        kit.slab(f"FacadeColumn{i}", x - 0.2, x + 0.2, HALL_FRONT - 0.35, HALL_FRONT, GROUND, z1 + 0.3, p.steel)
    # Back wall towards the platform: three wide openings
    kit.slab("BackWall", -HALL_X, HALL_X, HALL_BACK - 0.3, HALL_BACK, z0 + 4.5, z1, p.wall)
    for i, x in enumerate((-HALL_X + 0.4, -3.5, 3.5, HALL_X - 0.4)):
        kit.slab(f"BackPier{i}", x - 0.4, x + 0.4, HALL_BACK - 0.3, HALL_BACK, z0, z0 + 4.5, p.wall)
    kit.slab("HallInside", -HALL_X + 0.3, HALL_X - 0.3, HALL_BACK - 0.6, HALL_BACK - 0.3, z0, z0 + 4.5, p.dark)


def sawtooth_roof(kit, p):
    """Three teeth: glazed north lights facing the rail, slopes facing the street.
    The front tooth is a green roof, the two others carry solar panels."""
    tooth = (HALL_BACK - HALL_FRONT) / 3
    slope = math.atan2(TOOTH_RISE, tooth)
    for k in range(3):
        y0 = HALL_FRONT + k * tooth
        y1 = y0 + tooth
        kit.prism_x(f"Tooth{k}", [(y0, WALL_TOP), (y1, WALL_TOP), (y1, WALL_TOP + TOOTH_RISE)],
                    2 * HALL_X, p.wall if k else p.sedum)
        kit.slab(f"NorthLight{k}", -HALL_X + 0.5, HALL_X - 0.5, y1, y1 + 0.05,
                 WALL_TOP + 0.2, WALL_TOP + TOOTH_RISE - 0.15, p.glass)
        kit.slab(f"Gutter{k}", -HALL_X - 0.1, HALL_X + 0.1, y0 - 0.15, y0 + 0.15, WALL_TOP - 0.1, WALL_TOP + 0.1, p.trim)
        if k == 0:
            continue
        mid_y, mid_z = (y0 + y1) / 2, WALL_TOP + TOOTH_RISE / 2 + 0.12
        for i in range(10):
            x = -HALL_X + 1.15 + i * 2.18
            for j in (-1, 1):
                offset = j * 1.4
                kit.tilted_panel(f"RoofSolar{k}_{i}_{j}",
                                 (x, mid_y + offset * math.cos(slope), mid_z + offset * math.sin(slope)),
                                 (2.0, 2.6, 0.06), slope, p.solar)


def rail_canopy(kit, p):
    """Steel canopy over the platform and the track, timber deck, solar panels facing the street."""
    kit.slab("CanopyDeck", -HALF_WIDTH, HALF_WIDTH, HALL_BACK, CANOPY_BACK, CANOPY_Z, CANOPY_Z + 0.25, p.timber)
    kit.slab("CanopyEdge", -HALF_WIDTH, HALF_WIDTH, CANOPY_BACK - 0.2, CANOPY_BACK, CANOPY_Z - 0.3, CANOPY_Z + 0.3, p.trim)
    for i in range(5):
        x = -HALF_WIDTH + 0.3 + i * (2 * HALF_WIDTH - 0.6) / 4
        kit.slab(f"CanopyBeam{i}", x - 0.15, x + 0.15, HALL_BACK, CANOPY_BACK, CANOPY_Z - 0.5, CANOPY_Z, p.steel)
        kit.slab(f"CanopyColumn{i}", x - 0.18, x + 0.18, CANOPY_BACK - 0.6, CANOPY_BACK - 0.24, GROUND, CANOPY_Z, p.steel)
        kit.slab(f"PlatformColumn{i}", x - 0.15, x + 0.15, PLATFORM_EDGE - 1.6, PLATFORM_EDGE - 1.3, DOCK, CANOPY_Z, p.steel)
    tilt = math.radians(12)
    for row in range(4):
        y = HALL_BACK + 1.4 + row * 2.85
        for i in range(14):
            x = -HALF_WIDTH + 1.15 + i * 2.27
            kit.tilted_panel(f"CanopySolar{row}_{i}", (x, y, CANOPY_Z + 0.55), (2.1, 2.6, 0.06), tilt, p.solar)
    for i, x in enumerate((-12.0, -4.0, 4.0, 12.0)):
        kit.slab(f"PlatformLamp{i}", x - 0.3, x + 0.3, PLATFORM_EDGE - 1.6, PLATFORM_EDGE - 1.3,
                 CANOPY_Z - 0.6, CANOPY_Z - 0.5, p.light)


def street_front(kit, p):
    # Green awning over the loading ramp
    z = DOCK + 4.6
    kit.slab("Awning", -HALL_X, HALL_X, HALL_FRONT - 4.5, HALL_FRONT - 0.35, z, z + 0.25, p.steel)
    kit.slab("AwningGreen", -HALL_X + 0.2, HALL_X - 0.2, HALL_FRONT - 4.3, HALL_FRONT - 0.5, z + 0.25, z + 0.4, p.sedum)
    for i, x in enumerate((-HALL_X + 0.3, -3.6, 3.6, HALL_X - 0.3)):
        kit.slab(f"AwningPost{i}", x - 0.12, x + 0.12, HALL_FRONT - 4.4, HALL_FRONT - 4.16, GROUND, z, p.steel)
    # Cargo bike stands on both sides of the ramp
    for side in (-1, 1):
        for i in range(5):
            x = side * (10.0 + i * 1.2)
            y = FRONT + 2.2
            kit.tube(f"BikeStand{side}_{i}", [(x, y - 0.35, GROUND), (x, y - 0.35, GROUND + 0.75),
                                              (x, y + 0.35, GROUND + 0.75), (x, y + 0.35, GROUND)],
                     0.03, p.trim, sides=4)
        kit.tree(f"Tree{side}", side * 13.0, HALL_FRONT - 3.0, GROUND, 6.5, 2.2, p)
        kit.slab(f"Planter{side}", side * 13.0 - 1.2, side * 13.0 + 1.2, HALL_FRONT - 4.2, HALL_FRONT - 1.8,
                 GROUND, GROUND + 0.5, p.timber)
        kit.slab(f"PlanterSoil{side}", side * 13.0 - 1.1, side * 13.0 + 1.1, HALL_FRONT - 4.1, HALL_FRONT - 1.9,
                 GROUND + 0.5, GROUND + 0.55, p.sedum)
    # Street sign
    sign_x, sign_y = -HALF_WIDTH + 1.5, FRONT + 0.8
    kit.slab("SignPost", sign_x - 0.1, sign_x + 0.1, sign_y - 0.1, sign_y + 0.1, GROUND, GROUND + 3.4, p.steel)
    kit.slab("SignPanel", sign_x - 1.4, sign_x + 1.4, sign_y - 0.15, sign_y - 0.05, GROUND + 2.2, GROUND + 3.2, p.trim)
    kit.text("SignStreet", "LOCAL HUB", 0.45, (sign_x, sign_y - 0.2, GROUND + 2.5), p.wall, depth=0.03)
    for i, x in enumerate((-6.5, 0.0, 6.5)):
        kit.slab(f"WallLamp{i}", x - 0.3, x + 0.3, HALL_FRONT - 0.3, HALL_FRONT - 0.1,
                 DOCK + 4.3, DOCK + 4.45, p.light)


def pallet_stack(kit, p, name, x, y, z, boxes):
    kit.slab(name + "Pallet", x - 0.6, x + 0.6, y - 0.4, y + 0.4, z, z + 0.15, p.pallet)
    for i, (dx, dy, h) in enumerate(boxes):
        kit.slab(f"{name}Box{i}", x + dx - 0.27, x + dx + 0.27, y + dy - 0.18, y + dy + 0.18,
                 z + 0.15, z + 0.15 + h, p.cardboard)


def goods(kit, p):
    full = [(-0.3, -0.2, 0.5), (0.3, -0.2, 0.5), (-0.3, 0.2, 0.5), (0.3, 0.2, 0.9)]
    low = [(-0.3, 0.0, 0.4), (0.3, 0.0, 0.4)]
    for i, (x, y, boxes) in enumerate(((-13.5, 8.6, full), (-12.2, 9.6, low), (12.8, 8.8, full),
                                       (-5.0, HALL_FRONT - 1.6, low), (5.0, HALL_FRONT - 1.6, full))):
        pallet_stack(kit, p, f"Goods{i}", x, y, DOCK, boxes)
    kit.slab("Crates", 14.0, 15.4, 9.2, 10.4, DOCK, DOCK + 1.0, p.timber)


def scale_props(kit, p):
    kit.mannequin_standing(-1.5, FRONT + 3.0, p)
    kit.mannequin_standing(-9.0, 9.0, p)
    import_preview_vehicle(os.path.join(HERE, "..", "..", "vehicles", "cargo-bike-box", "SolarpunkCargoBikeBox01.fbx"),
                           (1.5, FRONT + 3.5, GROUND))


if __name__ == "__main__":
    build(HERE, NAME, make, target=(0, 0, 3.0), distance=46, decorate=scale_props)
