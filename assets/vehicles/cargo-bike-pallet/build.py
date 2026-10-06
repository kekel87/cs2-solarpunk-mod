# Electric bike towing a two-wheel trailer that carries a Euro pallet of boxes.
import os
import sys

# No __pycache__ next to the shared kit in the repository.
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "common"))
from vehicle_kit import bike_drivetrain, bike_rider, build  # noqa: E402

NAME = "SolarpunkCargoBikePallet01"

WHEEL_R = 0.33
FRONT_Y, REAR_Y = -0.55, 0.50
BOTTOM_BRACKET = (0, 0.02, 0.30)
SEAT_POST = (0, 0.18, 1.00)

PALLET_L, PALLET_W = 1.20, 0.80
TRAILER_FRONT = 1.10
TRAILER_FLOOR = 0.30
TRAILER_WHEEL_R = 0.25


def make_bike(kit, p):
    head_low = (0, FRONT_Y + 0.22, 0.55)
    head_high = (0, FRONT_Y + 0.17, 0.92)
    seat_top = (0, 0.15, 0.82)
    rear_axle = (0, REAR_Y, WHEEL_R)
    front_axle = (0, FRONT_Y, WHEEL_R)
    kit.tube("Frame_Down", [head_low, (0, -0.15, 0.36), BOTTOM_BRACKET], 0.038, p.frame)
    kit.tube("Frame_Seat", [BOTTOM_BRACKET, seat_top], 0.032, p.frame)
    kit.tube("Frame_ChainStay", [BOTTOM_BRACKET, rear_axle], 0.02, p.frame)
    kit.tube("Frame_SeatStay", [(0, 0.16, 0.72), rear_axle], 0.016, p.frame)
    kit.tube("Frame_Head", [head_low, head_high], 0.032, p.frame)
    kit.tube("Fork", [head_low, front_axle], 0.02, p.frame)
    kit.tube("Stem", [head_high, (0, FRONT_Y + 0.22, 1.02)], 0.02, p.dark)
    kit.tube("Handlebar", [(-0.28, FRONT_Y + 0.32, 1.00), (-0.20, FRONT_Y + 0.22, 1.03),
                           (0.20, FRONT_Y + 0.22, 1.03), (0.28, FRONT_Y + 0.32, 1.00)], 0.014, p.dark)
    kit.box("Battery", (0.09, 0.30, 0.09), (0, -0.22, 0.52), p.dark, bevel=0.02)
    kit.box("HeadLight", (0.07, 0.05, 0.05), (0, FRONT_Y + 0.12, 0.90), p.light)

    bike_drivetrain(kit, p, BOTTOM_BRACKET, seat_top, SEAT_POST)

    for label, y in (("F", FRONT_Y), ("R", REAR_Y)):
        kit.wheel(p, f"Wheel_{label}", WHEEL_R, 0.05, 0, y, spokes=8)
    kit.arc_guard("Mudguard_F", WHEEL_R + 0.04, front_axle, lambda co: co.z > WHEEL_R + 0.05, p.frame)
    kit.arc_guard("Mudguard_R", WHEEL_R + 0.04, rear_axle,
                  lambda co: co.z > WHEEL_R - 0.05 and co.y > REAR_Y - 0.18, p.frame)


def make_trailer(kit, p):
    center_y = TRAILER_FRONT + PALLET_L / 2
    axle_y = center_y + 0.05

    # Chassis: side rails, cross members, drawbar to the bike's rear axle
    for side in (-1, 1):
        x = side * (PALLET_W / 2 + 0.03)
        kit.tube(f"Rail_{side}", [(x, TRAILER_FRONT - 0.05, TRAILER_FLOOR), (x, TRAILER_FRONT + PALLET_L + 0.05, TRAILER_FLOOR)],
                 0.025, p.frame)
        kit.tube(f"Fender_Stay_{side}", [(x, axle_y, TRAILER_FLOOR), (x + side * 0.08, axle_y, TRAILER_FLOOR)], 0.02, p.frame)
    for i, y in enumerate((TRAILER_FRONT - 0.05, axle_y, TRAILER_FRONT + PALLET_L + 0.05)):
        kit.tube(f"Cross_{i}", [(-PALLET_W / 2 - 0.03, y, TRAILER_FLOOR), (PALLET_W / 2 + 0.03, y, TRAILER_FLOOR)],
                 0.022, p.frame)
    kit.tube("Drawbar", [(0, TRAILER_FRONT - 0.05, TRAILER_FLOOR), (0, REAR_Y + 0.35, 0.42), (0.06, REAR_Y, WHEEL_R + 0.04)],
             0.025, p.frame)
    kit.box("Hitch", (0.06, 0.08, 0.06), (0.06, REAR_Y + 0.04, WHEEL_R + 0.04), p.dark)
    kit.tube("Mast", [(0, TRAILER_FRONT - 0.05, TRAILER_FLOOR), (0, TRAILER_FRONT - 0.05, 1.00)], 0.022, p.frame)
    kit.box("Beacon", (0.08, 0.04, 0.08), (0, TRAILER_FRONT - 0.07, 1.02), p.light)

    for side in (-1, 1):
        x = side * (PALLET_W / 2 + 0.14)
        kit.wheel(p, f"Wheel_T{'L' if side < 0 else 'R'}", TRAILER_WHEEL_R, 0.05, x, axle_y, spokes=6)
        kit.arc_guard(f"Mudguard_T{side}", TRAILER_WHEEL_R + 0.04, (x, axle_y, TRAILER_WHEEL_R),
                      lambda co: co.z > TRAILER_WHEEL_R + 0.02, p.trim)
    kit.box("TailLight", (0.30, 0.02, 0.05), (0, TRAILER_FRONT + PALLET_L + 0.08, TRAILER_FLOOR), p.red)

    # Euro pallet: three bottom boards, nine blocks, top deck
    base = TRAILER_FLOOR + 0.03
    for i, x in enumerate((-PALLET_W / 2 + 0.05, 0, PALLET_W / 2 - 0.05)):
        kit.box(f"PalletBoard_{i}", (0.10, PALLET_L, 0.022), (x, center_y, base + 0.011), p.wood)
        for j, y in enumerate((-PALLET_L / 2 + 0.07, 0, PALLET_L / 2 - 0.07)):
            kit.box(f"PalletBlock_{i}{j}", (0.10, 0.12, 0.078), (x, center_y + y, base + 0.061), p.wood)
    kit.box("PalletDeck", (PALLET_W, PALLET_L, 0.022), (0, center_y, base + 0.111), p.wood)

    # Stacked boxes, wrapped
    deck = base + 0.122
    carton = (0.38, 0.58, 0.36)
    for i, (x, y) in enumerate(((-0.20, -0.30), (0.20, -0.30), (-0.20, 0.30), (0.20, 0.30))):
        kit.box(f"Carton_{i}", carton, (x, center_y + y, deck + carton[2] / 2), p.cardboard)
    for i, (x, y) in enumerate(((-0.20, -0.30), (0.20, 0.0), (-0.20, 0.30))):
        kit.box(f"CartonTop_{i}", (0.38, 0.40, 0.30), (x, center_y + y, deck + 0.36 + 0.15), p.cardboard)
    for i, z in enumerate((deck + 0.18, deck + 0.50)):
        kit.box(f"Strap_{i}", (PALLET_W + 0.01, 0.04, 0.02), (0, center_y, z), p.dark)


def make(kit, p):
    make_bike(kit, p)
    make_trailer(kit, p)
    bike_rider(kit, p, BOTTOM_BRACKET, SEAT_POST,
               shoulder=(0, SEAT_POST[1] - 0.18, 1.50),
               hands=[(0.26, FRONT_Y + 0.31, 1.02), (-0.26, FRONT_Y + 0.31, 1.02)])


build(HERE, NAME, make, target=(0, 0.75, 0.55), distance=1.25)
