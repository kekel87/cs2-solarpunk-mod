import { ValueBinding, useValue } from "cs2/api";
import * as l10n from "cs2/l10n";
import { LocComponent, LocalizedNumberProps, LocalizedPercentage, Unit } from "cs2/l10n";
import { Panel, PanelSection, PanelSectionRow } from "cs2/ui";
import * as bindings from "info/city-info-bindings";
import { useInfoText } from "info/city-info-text";
import styles from "info/city-info-panel.module.scss";

// The shipped cs2/l10n typings declare LocalizedNumber as both an interface and a component, so
// TypeScript only sees the interface; the game does export the component at runtime.
const LocalizedNumber = (l10n as unknown as { LocalizedNumber: LocComponent<LocalizedNumberProps> }).LocalizedNumber;

type RowProps = { labelKey: string; binding: ValueBinding<number> };

const ShareRow = ({ labelKey, binding }: RowProps) => {
  const infoText = useInfoText();
  return <PanelSectionRow left={infoText(labelKey)} right={<LocalizedPercentage value={useValue(binding)} max={1} />} />;
};

const CountRow = ({ labelKey, binding }: RowProps) => {
  const infoText = useInfoText();
  return <PanelSectionRow left={infoText(labelKey)} right={<LocalizedNumber value={useValue(binding)} unit={Unit.Integer} />} />;
};

export const CityInfoPanel = ({ onClose }: { onClose: () => void }) => {
  const infoText = useInfoText();

  return (
    <Panel className={styles.panel} header={infoText("TITLE")} onClose={onClose}>
      <PanelSection tooltip={infoText("TRIPS_TOOLTIP")}>
        <PanelSectionRow uppercase left={infoText("TRIPS")} />
        <ShareRow labelKey="CAR" binding={bindings.carShare} />
        <ShareRow labelKey="BICYCLE" binding={bindings.bicycleShare} />
        <ShareRow labelKey="PUBLIC_TRANSPORT" binding={bindings.publicTransportShare} />
        <ShareRow labelKey="WALKING" binding={bindings.walkingShare} />
      </PanelSection>
      <PanelSection>
        <PanelSectionRow uppercase left={infoText("VEHICLES")} />
        <CountRow labelKey="PERSONAL_CARS" binding={bindings.personalCarCount} />
        <CountRow labelKey="DELIVERY_TRUCKS" binding={bindings.deliveryTruckCount} />
        <CountRow labelKey="GARBAGE_TRUCKS" binding={bindings.garbageTruckCount} />
        <CountRow labelKey="CARGO_TRAINS" binding={bindings.cargoTrainCount} />
      </PanelSection>
      <PanelSection tooltip={infoText("RENEWABLE_TOOLTIP")}>
        <PanelSectionRow uppercase left={infoText("ELECTRICITY")} />
        <ShareRow labelKey="RENEWABLE_SHARE" binding={bindings.renewableElectricityShare} />
      </PanelSection>
    </Panel>
  );
};
