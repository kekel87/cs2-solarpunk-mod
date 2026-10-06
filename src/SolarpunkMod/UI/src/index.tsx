import { ModRegistrar } from "cs2/modding";
import { addDistrictSection } from "district/district-section";
import { CityInfoPanelToggle } from "info/city-info-panel-toggle";
import mod from "mod.json";

const register: ModRegistrar = (moduleRegistry) => {
  // A registrar that throws silences every mod registered after it: contain our failures.
  try {
    moduleRegistry.append("GameTopLeft", CityInfoPanelToggle);
    moduleRegistry.extend(
      "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
      "selectedInfoSectionComponents",
      addDistrictSection as never,
    );
  } catch (error) {
    console.error(`${mod.id}: UI registration failed`, error);
  }
};

export default register;
