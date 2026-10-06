# Box cargo bike: closed box ahead of the rider on two small wheels, bicycle rear.
import os
import sys

# No __pycache__ next to the shared kit in the repository.
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "common"))
from vehicle_kit import bike_drivetrain, bike_rider, build  # noqa: E402

NAME = "SolarpunkCargoBikeBox01"

BOX_W, BOX_L, BOX_H = 0.82, 0.98, 0.92
BOX_FLOOR = 0.40
BOX_FRONT = -1.30
BOX_BACK = BOX_FRONT + BOX_L
FRONT_R = 0.21
REAR_R, REAR_Y = 0.33, 0.95
BOTTOM_BRACKET = (0, 0.45, 0.30)
SEAT_POST = (0, 0.60, 0.98)


def make(kit, p):
    box_y = (BOX_FRONT + BOX_BACK) / 2
    box_z = BOX_FLOOR + BOX_H / 2

    # Cargo box
    kit.box("Box", (BOX_W, BOX_L, BOX_H), (0, box_y, box_z), p.body, bevel=0.06, segments=3)
    kit.box("BoxSkirt", (BOX_W + 0.01, BOX_L + 0.01, 0.08), (0, box_y, BOX_FLOOR + 0.04), p.trim, bevel=0.02)
    kit.box("BoxLid", (BOX_W + 0.04, BOX_L + 0.04, 0.035), (0, box_y, BOX_FLOOR + BOX_H + 0.01), p.trim, bevel=0.015)
    for side in (-1, 1):
        x = side * (BOX_W / 2 + 0.004)
        kit.box(f"Door_{side}", (0.012, BOX_L * 0.62, BOX_H * 0.62), (x, box_y + 0.06, box_z + 0.02), p.glass, bevel=0.004)
        kit.box(f"Handle_{side}", (0.02, 0.10, 0.025), (x + side * 0.006, box_y + 0.06, box_z + 0.30), p.dark)
        kit.box(f"HeadLight_{side}", (0.10, 0.02, 0.05), (side * 0.27, BOX_FRONT - 0.005, BOX_FLOOR + 0.20), p.light)
    kit.box("FrontPlate", (0.36, 0.015, 0.12), (0, BOX_FRONT - 0.006, BOX_FLOOR + 0.20), p.trim)

    # Front axle under the box
    front_axle_y = box_y + 0.18
    for side in (-1, 1):
        x = side * (BOX_W / 2 - 0.02)
        kit.wheel(p, f"Wheel_F{'L' if side < 0 else 'R'}", FRONT_R, 0.07, x, front_axle_y)
        kit.arc_guard(f"Mudguard_F{side}", FRONT_R + 0.045, (x, front_axle_y, FRONT_R),
                      lambda co: co.z > FRONT_R + 0.02, p.trim)
    kit.box("Chassis", (BOX_W - 0.10, BOX_L - 0.10, 0.05), (0, box_y, BOX_FLOOR - 0.03), p.dark)

    # Bicycle rear
    head_low = (0, BOX_BACK + 0.10, BOX_FLOOR + 0.05)
    head_high = (0, BOX_BACK + 0.06, 0.90)
    seat_top = (0, 0.58, 0.82)
    rear_axle = (0, REAR_Y, REAR_R)
    kit.tube("Frame_Main", [head_low, (0, BOX_BACK + 0.35, 0.36), BOTTOM_BRACKET], 0.04, p.frame)
    kit.tube("Frame_Seat", [BOTTOM_BRACKET, seat_top], 0.032, p.frame)
    kit.tube("Frame_ChainStay", [BOTTOM_BRACKET, rear_axle], 0.02, p.frame)
    kit.tube("Frame_SeatStay", [(0, 0.60, 0.70), rear_axle], 0.016, p.frame)
    kit.tube("Frame_Head", [head_low, head_high], 0.035, p.frame)
    kit.tube("Frame_ToBox", [(0, BOX_BACK - 0.02, BOX_FLOOR - 0.03), head_low], 0.04, p.frame)
    kit.tube("Stem", [head_high, (0, BOX_BACK + 0.12, 1.02)], 0.022, p.dark)
    kit.tube("Handlebar", [(-0.30, BOX_BACK + 0.22, 1.00), (-0.22, BOX_BACK + 0.13, 1.03),
                           (0.22, BOX_BACK + 0.13, 1.03), (0.30, BOX_BACK + 0.22, 1.00)], 0.014, p.dark)
    for side in (-1, 1):
        kit.tube(f"Grip_{side}", [(side * 0.26, BOX_BACK + 0.19, 1.01), (side * 0.31, BOX_BACK + 0.23, 1.00)], 0.02, p.dark)

    # Drivetrain and battery
    bike_drivetrain(kit, p, BOTTOM_BRACKET, seat_top, SEAT_POST)
    bb_y, bb_z = BOTTOM_BRACKET[1], BOTTOM_BRACKET[2]
    kit.tube("Chain", [(0.05, bb_y + 0.09, bb_z + 0.04), (0.05, REAR_Y - 0.05, REAR_R + 0.03)], 0.006, p.dark, sides=4)
    kit.box("Battery", (0.10, 0.30, 0.09), (0, (bb_y + BOX_BACK) / 2 + 0.05, 0.42), p.dark, bevel=0.02)

    # Rear wheel, mudguard, rack, lamp
    kit.wheel(p, "Wheel_R", REAR_R, 0.05, 0, REAR_Y, spokes=12)
    kit.arc_guard("Mudguard_R", REAR_R + 0.04, rear_axle,
                  lambda co: co.z > REAR_R - 0.05 and co.y > REAR_Y - 0.18, p.frame)
    kit.box("Rack", (0.16, 0.36, 0.02), (0, REAR_Y - 0.02, REAR_R * 2 + 0.06), p.dark)
    for side in (-1, 1):
        kit.tube(f"RackStay_{side}", [(side * 0.07, REAR_Y + 0.14, REAR_R * 2 + 0.05), (side * 0.04, REAR_Y, REAR_R)],
                 0.008, p.dark, sides=4)
    kit.box("RearLamp", (0.08, 0.02, 0.04), (0, REAR_Y + 0.17, REAR_R * 2 + 0.04), p.red)

    bike_rider(kit, p, BOTTOM_BRACKET, SEAT_POST,
               shoulder=(0, SEAT_POST[1] - 0.12, 1.50),
               hands=[(0.27, BOX_BACK + 0.21, 1.02), (-0.27, BOX_BACK + 0.21, 1.02)])


build(HERE, NAME, make, target=(0, -0.15, 0.65))
