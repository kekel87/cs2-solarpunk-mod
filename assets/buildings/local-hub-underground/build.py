# Underground Local Hub: the platform is 20 m below, under the street; on the surface only a compact
# pavilion with two goods lifts for cargo bikes and trikes, a stair shelter, and a solar canopy over
# the loading forecourt.
# Lot 3 x 4 cells (24 x 32 m): street at -Y; the underground track and platform are sub-nets.
import math
import os
import sys

# No __pycache__ next to the shared kits in the repository.
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "common"))
from building_kit import CELL, build, import_preview_vehicle  # noqa: E402

NAME = "SolarpunkLocalHubUnderground01"

HALF_WIDTH = 1.5 * CELL                 # x from -12 to 12
FRONT, BACK = -2 * CELL, 2 * CELL       # y from -16 (street) to 16
GROUND = 0.15
PAVILION_X = 8.0
PAVILION_FRONT, PAVILION_BACK = -4.0, 6.0
PAVILION_TOP = GROUND + 5.0
LIFTS_X = (-4.0, 4.0)                   # goods lifts down to the platform at -20 m
LIFT_SIZE = 3.2
STAIR = (-10.5, -6.5, 9.0, 14.5)        # x0, x1, y0, y1 of the stair shelter over the opening
CANOPY_Z = GROUND + 3.4
CANOPY_X = (-HALF_WIDTH + 0.5, -0.5)     # canopy over the bike side only: the facade stays visible


def make(kit, p):
    kit.slab("Lot", -HALF_WIDTH, HALF_WIDTH, FRONT, BACK, 0.0, GROUND, p.paving)
    pavilion(kit, p)
    goods_lifts(kit, p)
    stair_shelter(kit, p)
    forecourt(kit, p)


def pavilion(kit, p):
    z0, z1 = GROUND, PAVILION_TOP
    # Timber frame with full-height glazing; solid back wall carries the lifts' machinery
    kit.slab("Floor", -PAVILION_X, PAVILION_X, PAVILION_FRONT, PAVILION_BACK, z0, z0 + 0.1, p.concrete)
    for face, y in (("Front", PAVILION_FRONT), ("Back", PAVILION_BACK - 0.05)):
        kit.slab(f"Glazing{face}", -PAVILION_X + 0.3, PAVILION_X - 0.3, y, y + 0.05, z0 + 0.1, z1 - 0.6,
                 p.glass if face == "Front" else p.wall)
    for side in (-1, 1):
        x0, x1 = sorted((side * PAVILION_X, side * (PAVILION_X - 0.05)))
        kit.slab(f"GlazingSide{side}", x0, x1, PAVILION_FRONT, PAVILION_BACK, z0 + 0.1, z1 - 0.6, p.glass)
    for i in range(7):
        x = -PAVILION_X + i * (2 * PAVILION_X) / 6
        for y in (PAVILION_FRONT, PAVILION_BACK):
            kit.slab(f"Post{i}_{int(y)}", x - 0.15, x + 0.15, y - 0.15, y + 0.15, z0, z1, p.timber)
    kit.slab("Fascia", -PAVILION_X - 0.4, PAVILION_X + 0.4, PAVILION_FRONT - 0.4, PAVILION_BACK + 0.4,
             z1 - 0.6, z1, p.trim)
    kit.slab("RoofGreen", -PAVILION_X - 0.2, PAVILION_X + 0.2, PAVILION_FRONT - 0.2, PAVILION_BACK + 0.2,
             z1, z1 + 0.15, p.sedum)
    kit.text("SignFacade", "LOCAL HUB", 0.55, (0, PAVILION_FRONT - 0.45, z1 - 0.48), p.wall, depth=0.05)
    # Rooftop solar on a light frame, tilted to the street
    tilt = math.radians(15)
    for row in range(2):            # the back third is left to the lift machine heads
        y = PAVILION_FRONT + 1.6 + row * 3.0
        for i in range(7):
            x = -PAVILION_X + 1.2 + i * 2.3
            kit.tilted_panel(f"RoofSolar{row}_{i}", (x, y, z1 + 0.65), (2.1, 2.7, 0.06), tilt, p.solar)


def goods_lifts(kit, p):
    """Two lift cabins large enough for a cargo bike, doors to the street, machine heads on the roof."""
    for i, x in enumerate(LIFTS_X):
        h = LIFT_SIZE / 2
        y0, y1 = PAVILION_BACK - 0.3 - LIFT_SIZE, PAVILION_BACK - 0.3
        kit.slab(f"LiftShaft{i}", x - h, x + h, y0, y1, GROUND, PAVILION_TOP + 1.6, p.wall)
        kit.slab(f"LiftDoor{i}", x - 1.1, x + 1.1, y0 - 0.05, y0, GROUND + 0.1, GROUND + 2.6, p.steel)
        kit.slab(f"LiftDoorFrame{i}", x - 1.3, x + 1.3, y0 - 0.1, y0 - 0.03, GROUND + 2.6, GROUND + 2.85, p.safety)
        kit.slab(f"LiftHead{i}", x - h - 0.1, x + h + 0.1, y0 - 0.1, y1 + 0.1, PAVILION_TOP + 1.6,
                 PAVILION_TOP + 1.9, p.trim)
        kit.slab(f"LiftLamp{i}", x - 0.3, x + 0.3, y0 - 0.12, y0 - 0.08, GROUND + 3.0, GROUND + 3.2, p.light)


def stair_shelter(kit, p):
    """Glazed shelter over the stairs to the platform; the dark floor marks the opening (the stairs
    themselves and the platform are part of the underground sub-nets)."""
    x0, x1, y0, y1 = STAIR
    kit.slab("StairOpening", x0, x1, y0, y1, GROUND, GROUND + 0.02, p.dark)
    for x in (x0, x1):
        kit.slab(f"StairWall{int(x)}", x - 0.1, x + 0.1, y0, y1, GROUND, GROUND + 1.1, p.wall)
        for y in (y0, y1):
            kit.slab(f"StairPost{int(x)}_{int(y)}", x - 0.1, x + 0.1, y - 0.1, y + 0.1, GROUND, GROUND + 3.0, p.steel)
    kit.slab("StairRoof", x0 - 0.3, x1 + 0.3, y0 - 0.3, y1 + 0.3, GROUND + 3.0, GROUND + 3.15, p.glass)
    kit.slab("StairSign", x0 - 0.2, x1 + 0.2, y0 - 0.32, y0 - 0.22, GROUND + 2.4, GROUND + 3.0, p.trim)
    # Ventilation stack for the underground station, planted at its foot
    kit.slab("Vent", 7.5, 10.5, 9.5, 13.5, GROUND, GROUND + 2.6, p.wall)
    kit.slab("VentGrille", 7.4, 10.6, 9.4, 13.6, GROUND + 2.0, GROUND + 2.5, p.trim)
    kit.slab("VentPlanter", 7.0, 11.0, 9.0, 14.0, GROUND, GROUND + 0.5, p.timber)


def forecourt(kit, p):
    """Solar canopy over the loading forecourt, cargo bike stands, two trees, a totem."""
    y0, y1 = FRONT + 1.5, PAVILION_FRONT - 0.5
    cx0, cx1 = CANOPY_X
    kit.slab("CanopyDeck", cx0, cx1, y0, y1, CANOPY_Z, CANOPY_Z + 0.2, p.timber)
    for i in range(3):
        x = cx0 + 0.5 + i * (cx1 - cx0 - 1.0) / 2
        kit.slab(f"CanopyColumn{i}", x - 0.15, x + 0.15, y0 + 0.3, y0 + 0.6, GROUND, CANOPY_Z, p.steel)
        kit.slab(f"CanopyBeam{i}", x - 0.12, x + 0.12, y0, y1, CANOPY_Z - 0.35, CANOPY_Z, p.steel)
    tilt = math.radians(10)
    for row in range(3):
        y = y0 + 1.5 + row * 3.0
        for i in range(5):
            x = cx0 + 1.2 + i * 2.2
            kit.tilted_panel(f"CanopySolar{row}_{i}", (x, y, CANOPY_Z + 0.5), (2.05, 2.7, 0.06), tilt, p.solar)
    for i in range(6):
        x = -9.0 + i * 1.2
        y = y0 + 4.5
        kit.tube(f"BikeStand{i}", [(x, y - 0.35, GROUND), (x, y - 0.35, GROUND + 0.75),
                                   (x, y + 0.35, GROUND + 0.75), (x, y + 0.35, GROUND)], 0.03, p.trim, sides=4)
    # Loading bays in front of the lifts, painted on the paving
    for i, x in enumerate((1.5, 4.5, 7.5, 10.5)):
        kit.slab(f"BayLine{i}", x - 0.06, x + 0.06, y0 + 1.0, PAVILION_FRONT - 1.0, GROUND, GROUND + 0.01, p.safety)
    for side in (-1, 1):
        tx = side * 10.0
        kit.tree(f"Tree{side}", tx, 3.0, GROUND, 6.0, 1.9, p)
        kit.slab(f"Planter{side}", tx - 1.1, tx + 1.1, 1.9, 4.1, GROUND, GROUND + 0.5, p.timber)
    sx, sy = HALF_WIDTH - 1.2, FRONT + 0.8
    kit.slab("Totem", sx - 0.5, sx + 0.5, sy - 0.2, sy + 0.2, GROUND, GROUND + 3.5, p.trim)
    kit.text("TotemSign", "HUB", 0.45, (sx, sy - 0.25, GROUND + 2.6), p.wall, depth=0.03)


def scale_props(kit, p):
    kit.mannequin_standing(-2.0, FRONT + 5.0, p)
    import_preview_vehicle(os.path.join(HERE, "..", "..", "vehicles", "cargo-bike-box", "SolarpunkCargoBikeBox01.fbx"),
                           (3.0, FRONT + 5.5, GROUND))


if __name__ == "__main__":
    build(HERE, NAME, make, target=(0, 0, 2.5), distance=38, decorate=scale_props, street_y=FRONT - 6.0)
