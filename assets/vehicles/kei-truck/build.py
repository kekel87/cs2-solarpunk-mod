# Electric kei truck: cab-over-engine cab, flat bed with drop sides, four small wheels.
import os
import sys

# No __pycache__ next to the shared kit in the repository.
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "common"))
from vehicle_kit import build  # noqa: E402

NAME = "SolarpunkKeiTruck01"

WIDTH = 1.46
WHEEL_R = 0.27
FRONT_Y, REAR_Y, TRACK = -1.12, 0.98, 0.58
CAB_FRONT, CAB_BACK = -1.72, -0.56
SILL_Z = 0.42
BED_FRONT, BED_BACK, BED_Z = -0.50, 1.68, 0.74


def make(kit, p):
    # Cab: side profile extruded across the width
    cab = [(CAB_BACK, SILL_Z), (CAB_FRONT + 0.02, SILL_Z), (CAB_FRONT, 0.86), (CAB_FRONT + 0.06, 1.16),
           (CAB_FRONT + 0.24, 1.76), (CAB_FRONT + 0.34, 1.84), (CAB_BACK, 1.85)]
    kit.prism_x("Cab", cab, WIDTH, p.body, bevel=0.05)
    windscreen = [(CAB_FRONT + 0.045, 1.19), (CAB_FRONT + 0.225, 1.74), (CAB_FRONT + 0.245, 1.735), (CAB_FRONT + 0.065, 1.19)]
    kit.prism_x("Windscreen", [(y - 0.012, z) for y, z in windscreen], WIDTH - 0.14, p.glass)
    for side in (-1, 1):
        x = side * (WIDTH / 2 + 0.004)
        kit.prism_x(f"DoorWindow_{side}", [(-0.66, 1.18), (-1.50, 1.18), (-1.38, 1.70), (-0.66, 1.72)], 0.01, p.glass, x=x)
        kit.box(f"DoorFront_{side}", (0.01, 0.015, 0.78), (x, -1.53, 0.84), p.trim)
        kit.box(f"DoorBack_{side}", (0.01, 0.015, 1.30), (x, -0.64, 1.08), p.trim)
        kit.box(f"DoorHandle_{side}", (0.012, 0.10, 0.03), (x, -0.78, 1.08), p.dark)
        kit.box(f"Mirror_{side}", (0.05, 0.04, 0.14), (side * (WIDTH / 2 + 0.07), -1.45, 1.32), p.dark)
        kit.box(f"HeadLight_{side}", (0.22, 0.03, 0.12), (side * 0.52, CAB_FRONT - 0.01, 0.82), p.light)
        kit.box(f"Indicator_{side}", (0.10, 0.03, 0.06), (side * 0.52, CAB_FRONT - 0.01, 0.68), p.amber)
    kit.box("Bumper", (WIDTH + 0.02, 0.10, 0.14), (0, CAB_FRONT - 0.02, 0.50), p.trim, bevel=0.02)
    kit.box("Grille", (0.60, 0.02, 0.10), (0, CAB_FRONT - 0.012, 0.78), p.trim)
    kit.box("Wiper", (0.50, 0.02, 0.012), (0, CAB_FRONT + 0.03, 1.20), p.dark)

    # Chassis and drop-side bed
    kit.box("Chassis", (0.80, BED_BACK - CAB_FRONT - 0.20, 0.12), (0, (BED_BACK + CAB_FRONT) / 2, SILL_Z - 0.02), p.dark)
    bed_y = (BED_FRONT + BED_BACK) / 2
    bed_l = BED_BACK - BED_FRONT
    kit.box("BedFloor", (WIDTH, bed_l, 0.06), (0, bed_y, BED_Z), p.trim)
    side_h = 0.30
    for side in (-1, 1):
        kit.box(f"DropSide_{side}", (0.04, bed_l, side_h), (side * (WIDTH / 2 - 0.02), bed_y, BED_Z + 0.03 + side_h / 2), p.frame)
        for i in range(3):
            y = BED_FRONT + 0.25 + i * (bed_l - 0.50) / 2
            kit.box(f"Hinge_{side}_{i}", (0.05, 0.05, 0.05), (side * (WIDTH / 2), y, BED_Z + 0.06), p.dark)
    kit.box("Tailgate", (WIDTH, 0.04, side_h), (0, BED_BACK - 0.02, BED_Z + 0.03 + side_h / 2), p.frame)
    kit.box("Headboard", (WIDTH, 0.05, side_h + 0.25), (0, BED_FRONT + 0.02, BED_Z + 0.03 + (side_h + 0.25) / 2), p.frame)
    for i, x in enumerate((-0.45, -0.15, 0.15, 0.45)):
        kit.box(f"GuardBar_{i}", (0.03, 0.03, 0.55), (x, BED_FRONT + 0.03, BED_Z + 0.58), p.dark)
    kit.box("GuardTop", (WIDTH - 0.30, 0.04, 0.04), (0, BED_FRONT + 0.03, BED_Z + 0.86), p.dark)
    for side in (-1, 1):
        kit.box(f"TailLight_{side}", (0.16, 0.03, 0.08), (side * 0.56, BED_BACK + 0.01, BED_Z - 0.06), p.red)
    kit.box("RearBumper", (WIDTH - 0.10, 0.06, 0.08), (0, BED_BACK + 0.02, SILL_Z + 0.04), p.dark)

    # A few crates on the bed so it reads as a delivery vehicle
    for i, (x, y) in enumerate(((-0.33, 0.10), (0.33, 0.10), (-0.33, 0.75))):
        kit.box(f"Crate_{i}", (0.55, 0.55, 0.40), (x, y, BED_Z + 0.23), p.cardboard)

    # Wheels with arches
    for label, y in (("F", FRONT_Y), ("R", REAR_Y)):
        for side in (-1, 1):
            x = side * TRACK
            kit.wheel(p, f"Wheel_{label}{'L' if side < 0 else 'R'}", WHEEL_R, 0.16, x, y, tyre=0.06)
            kit.arc_guard(f"Arch_{label}{side}", WHEEL_R + 0.07, (x + side * 0.08, y, WHEEL_R),
                          lambda co: co.z > WHEEL_R + 0.04, p.dark, minor=0.05)

    kit.anchor("DriverAnchor", (0.30, -1.15, 0.95))


build(HERE, NAME, make, target=(0, -0.05, 0.85), distance=1.35)
