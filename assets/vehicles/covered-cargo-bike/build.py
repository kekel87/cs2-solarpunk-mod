# Covered cargo bike: electric-assist delivery quadricycle, rider under a light canopy at the front,
# tall closed cargo box at the rear, four bicycle-size wheels. No branding.
import os
import sys

# No __pycache__ next to the shared kit in the repository.
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "common"))
from vehicle_kit import bike_drivetrain, bike_rider, build  # noqa: E402

NAME = "SolarpunkCoveredCargoBike01"

WHEEL_R = 0.33
FRONT_AXLE_Y, REAR_AXLE_Y = -0.86, 0.94
FRONT_TRACK, REAR_TRACK = 0.44, 0.50
BOX_W, BOX_FRONT, BOX_BACK = 1.14, 0.06, 1.36
BOX_FLOOR, BOX_TOP = 0.46, 1.86
ROOF_FRONT = -0.94
BOTTOM_BRACKET = (0, -0.78, 0.32)
SEAT_TOP = (0, -0.34, 0.74)
SEAT_POST = (0, -0.30, 0.88)


def make(kit, p):
    box_y = (BOX_FRONT + BOX_BACK) / 2
    box_l = BOX_BACK - BOX_FRONT
    box_h = BOX_TOP - BOX_FLOOR

    # Rear cargo box with a rear door and a side accent band
    kit.box("Box", (BOX_W, box_l, box_h), (0, box_y, BOX_FLOOR + box_h / 2), p.body, bevel=0.05, segments=2)
    kit.box("BoxSkirt", (BOX_W + 0.01, box_l + 0.01, 0.10), (0, box_y, BOX_FLOOR + 0.05), p.trim, bevel=0.02)
    for side in (-1, 1):
        x = side * (BOX_W / 2 + 0.004)
        kit.box(f"Band_{side}", (0.012, box_l * 0.86, 0.10), (x, box_y, BOX_FLOOR + 0.55), p.trim)
    kit.box("RearDoor", (BOX_W * 0.80, 0.012, box_h * 0.80), (0, BOX_BACK + 0.004, BOX_FLOOR + box_h * 0.46), p.trim, bevel=0.004)
    kit.box("DoorHandle", (0.04, 0.02, 0.18), (0.30, BOX_BACK + 0.014, BOX_FLOOR + 0.70), p.dark)
    for side in (-1, 1):
        kit.box(f"RearLamp_{side}", (0.08, 0.02, 0.16), (side * (BOX_W / 2 - 0.08), BOX_BACK + 0.012, BOX_FLOOR + 0.30), p.red)
        kit.box(f"Indicator_{side}", (0.06, 0.02, 0.05), (side * (BOX_W / 2 - 0.08), BOX_BACK + 0.012, BOX_FLOOR + 0.48), p.amber)

    # Canopy over the rider: roof curving down to the windscreen, two slim C-shaped side frames,
    # a front sill; the sides stay open as on the reference.
    top = BOX_TOP + 0.02
    kit.prism_x("Roof", [(BOX_FRONT, top), (-0.82, top), (-1.10, top - 0.12), (-1.25, 1.60),
                         (-1.12, 1.56), (-0.99, top - 0.20), (-0.80, top - 0.09), (BOX_FRONT, top - 0.09)],
                BOX_W, p.frame, bevel=0.012)
    side_frame = [(-0.82, top), (-1.10, top - 0.12), (-1.27, 1.52), (-1.34, 1.02), (-1.36, 0.70),
                  (-1.26, 0.70), (-1.25, 1.02), (-1.19, 1.48), (-1.02, top - 0.20), (-0.82, top - 0.09)]
    for side in (-1, 1):
        kit.prism_x(f"SideFrame_{side}", side_frame, 0.07, p.frame, x=side * (BOX_W / 2 - 0.035), bevel=0.01)
    kit.box("Sill", (BOX_W, 0.12, 0.06), (0, -1.31, 0.73), p.frame, bevel=0.01)
    kit.prism_x("Windscreen", [(-1.31, 0.76), (-1.25, 1.52), (-1.22, 1.52), (-1.28, 0.76)], BOX_W - 0.10, p.glass)

    # Lower nose between the front wheels, with headlight
    kit.prism_x("Nose", [(-1.34, 0.32), (-1.36, 0.70), (-1.10, 0.70), (-0.98, 0.32)], 0.56, p.body, bevel=0.03)
    kit.box("HeadLight", (0.24, 0.02, 0.06), (0, -1.37, 0.58), p.light)
    for side in (-1, 1):
        kit.box(f"Mirror_{side}", (0.04, 0.10, 0.07), (side * (BOX_W / 2 + 0.03), -1.05, 1.18), p.dark)

    # Low chassis from the nose to the box
    kit.box("Chassis", (0.62, BOX_FRONT + 1.26, 0.06), (0, (BOX_FRONT - 1.20) / 2, 0.30), p.dark)
    kit.box("Battery", (0.22, 0.30, 0.12), (0, -0.20, 0.40), p.dark, bevel=0.02)

    # Wheels: front pair with mudguards, rear pair tucked under the box with arches
    for side, label in ((-1, "L"), (1, "R")):
        fx = side * FRONT_TRACK
        kit.wheel(p, f"Wheel_F{label}", WHEEL_R, 0.06, fx, FRONT_AXLE_Y, spokes=4)
        kit.arc_guard(f"Mudguard_F{label}", WHEEL_R + 0.04, (fx, FRONT_AXLE_Y, WHEEL_R),
                      lambda co: co.z > WHEEL_R - 0.02, p.frame, minor=0.035)
        kit.tube(f"FrontArm_{label}", [(side * 0.28, FRONT_AXLE_Y, 0.32), (fx, FRONT_AXLE_Y, WHEEL_R)], 0.025, p.dark, sides=4)
        rx = side * REAR_TRACK
        kit.wheel(p, f"Wheel_R{label}", WHEEL_R, 0.06, rx, REAR_AXLE_Y)
        kit.arc_guard(f"Arch_R{label}", WHEEL_R + 0.05, (side * (BOX_W / 2 + 0.01), REAR_AXLE_Y, WHEEL_R),
                      lambda co: co.z > WHEEL_R - 0.02, p.trim)

    # Rider position: low frame, seat post, handlebar behind the windscreen
    kit.tube("Frame_Seat", [BOTTOM_BRACKET, (0, -0.55, 0.40), SEAT_TOP], 0.035, p.frame)
    kit.tube("Steering", [(0, -1.00, 0.36), (0, -0.92, 1.02)], 0.03, p.frame)
    kit.tube("Frame_Down", [(0, -0.97, 0.52), BOTTOM_BRACKET], 0.035, p.frame)
    kit.tube("Handlebar", [(-0.28, -0.80, 1.00), (-0.20, -0.90, 1.04), (0.20, -0.90, 1.04), (0.28, -0.80, 1.00)],
             0.014, p.dark)
    kit.box("Dashboard", (0.20, 0.04, 0.08), (0, -0.93, 1.08), p.dark)
    bike_drivetrain(kit, p, BOTTOM_BRACKET, SEAT_TOP, SEAT_POST)

    bike_rider(kit, p, BOTTOM_BRACKET, SEAT_POST,
               shoulder=(0, SEAT_POST[1] + 0.05, 1.42),
               hands=[(0.26, -0.81, 1.01), (-0.26, -0.81, 1.01)])


build(HERE, NAME, make, target=(0, 0.0, 0.95), distance=1.15)
