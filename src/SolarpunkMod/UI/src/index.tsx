import { ModRegistrar } from "cs2/modding";
import { CityInfoPanelToggle } from "info/city-info-panel-toggle";

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append("GameTopLeft", CityInfoPanelToggle);
};

export default register;
