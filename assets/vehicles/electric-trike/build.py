# Electric three-wheeled van: rounded one-seat cabin on a single front wheel, covered bed on two
# rear wheels.
import os
import sys

# No __pycache__ next to the shared kit in the repository.
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "common"))
from vehicle_kit import build  # noqa: E402

NAME = "SolarpunkElectricTrike01"

WHEEL_R = 0.22
FRONT_Y = -0.92
REAR_Y, REAR_TRACK = 0.80, 0.52
CAB_W = 1.10
CAB_BACK = -0.18
CHASSIS_Z = 0.42
BED_FRONT, BED_BACK, BED_W = -0.12, 1.25, 1.24


def make(kit, p):
    # Cabin: side profile (y, z) extruded across the width, nose rounded down to the fork
    cabin = [(CAB_BACK, CHASSIS_Z), (-0.95, CHASSIS_Z), (-1.14, 0.62), (-1.17, 0.98), (-1.08, 1.30),
             (-0.92, 1.58), (-0.78, 1.70), (CAB_BACK, 1.72)]
    kit.prism_x("Cabin", cabin, CAB_W, p.body, bevel=0.04)
    kit.prism_x("Roof", [(CAB_BACK + 0.02, 1.71), (-0.80, 1.69), (-0.74, 1.76), (CAB_BACK + 0.02, 1.76)],
                CAB_W - 0.08, p.body, bevel=0.02)
    windscreen = [(-1.185, 1.02), (-1.100, 1.31), (-0.945, 1.575), (-0.925, 1.565), (-1.080, 1.30), (-1.165, 1.02)]
    kit.prism_x("Windscreen", windscreen, CAB_W - 0.18, p.glass)
    for side in (-1, 1):
        x = side * (CAB_W / 2 + 0.004)
        kit.prism_x(f"SideWindow_{side}", [(-0.30, 1.05), (-0.90, 1.05), (-0.80, 1.52), (-0.30, 1.55)], 0.01, p.glass, x=x)
        kit.box(f"DoorLine_{side}", (0.01, 0.015, 0.55), (x, -0.28, 0.75), p.trim)
        kit.box(f"Mirror_{side}", (0.06, 0.03, 0.10), (side * (CAB_W / 2 + 0.06), -0.90, 1.30), p.dark)
        kit.box(f"HeadLight_{side}", (0.12, 0.03, 0.08), (side * 0.30, -1.155, 0.82), p.light)
        kit.box(f"Indicator_{side}", (0.06, 0.03, 0.04), (side * 0.46, -1.13, 0.70), p.amber)
    kit.prism_x("NoseApron", [(-0.95, CHASSIS_Z - 0.02), (-1.16, 0.60), (-1.185, 0.66), (-0.95, 0.66)],
                CAB_W - 0.30, p.trim)
    kit.box("Grille", (0.40, 0.02, 0.10), (0, -1.16, 0.62), p.trim)
    kit.box("Wiper", (0.40, 0.02, 0.012), (0, -1.12, 1.03), p.dark)

    # Single front wheel with a fork and a rounded mudguard
    kit.wheel(p, "Wheel_F", WHEEL_R, 0.10, 0, FRONT_Y, tyre=0.05)
    for side in (-1, 1):
        kit.tube(f"Fork_{side}", [(side * 0.08, FRONT_Y, WHEEL_R), (side * 0.08, -0.98, CHASSIS_Z + 0.10)], 0.025, p.dark)
    kit.arc_guard("Mudguard_F", WHEEL_R + 0.05, (0, FRONT_Y, WHEEL_R),
                  lambda co: co.z > WHEEL_R - 0.02 and co.y < FRONT_Y + 0.15, p.body, minor=0.06)

    # Chassis and covered bed
    kit.box("Chassis", (0.30, BED_BACK + 0.95, 0.10), (0, (BED_BACK - 0.95) / 2, CHASSIS_Z - 0.04), p.dark)
    bed_y = (BED_FRONT + BED_BACK) / 2
    bed_l = BED_BACK - BED_FRONT
    kit.box("BedFloor", (BED_W, bed_l, 0.06), (0, bed_y, 0.58), p.trim)
    kit.box("BedSides", (BED_W, bed_l, 0.30), (0, bed_y, 0.76), p.frame, bevel=0.015)
    kit.prism_x("Tarp", [(BED_FRONT + 0.02, 0.90), (BED_BACK - 0.02, 0.90), (BED_BACK - 0.02, 1.48),
                         (BED_BACK - 0.08, 1.56), (BED_FRONT + 0.08, 1.56), (BED_FRONT + 0.02, 1.48)],
                BED_W - 0.02, p.canvas, bevel=0.03)
    for i in range(4):
        y = BED_FRONT + 0.20 + i * (bed_l - 0.40) / 3
        kit.box(f"TarpRope_{i}", (BED_W + 0.01, 0.02, 0.02), (0, y, 0.92), p.dark)
    for side in (-1, 1):
        kit.box(f"TailLight_{side}", (0.10, 0.02, 0.07), (side * 0.48, BED_BACK + 0.005, 0.74), p.red)
    kit.box("Bumper", (BED_W - 0.10, 0.06, 0.06), (0, BED_BACK + 0.03, 0.52), p.dark)

    # Rear wheels under the bed, with arches
    for side in (-1, 1):
        x = side * REAR_TRACK
        kit.wheel(p, f"Wheel_R{'L' if side < 0 else 'R'}", WHEEL_R, 0.12, x, REAR_Y, tyre=0.05)
        kit.arc_guard(f"Arch_R{side}", WHEEL_R + 0.06, (x + side * 0.06, REAR_Y, WHEEL_R),
                      lambda co: co.z > WHEEL_R + 0.05, p.dark, minor=0.05)

    kit.anchor("DriverAnchor", (0, -0.45, 0.95))


build(HERE, NAME, make, target=(0, 0.05, 0.75), distance=1.15)
