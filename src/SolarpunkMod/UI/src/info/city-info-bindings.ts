import { bindValue } from "cs2/api";
import mod from "mod.json";

const bind = (name: string) => bindValue<number>(mod.id, name, 0);

export const carShare = bind("carShare");
export const bicycleShare = bind("bicycleShare");
export const publicTransportShare = bind("publicTransportShare");
export const walkingShare = bind("walkingShare");
export const personalCarCount = bind("personalCarCount");
export const deliveryTruckCount = bind("deliveryTruckCount");
export const garbageTruckCount = bind("garbageTruckCount");
export const cargoTrainCount = bind("cargoTrainCount");
export const renewableElectricityShare = bind("renewableElectricityShare");
