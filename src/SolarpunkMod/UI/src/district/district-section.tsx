import { LocalizedPercentage, Unit } from "cs2/l10n";
import { PanelSection, PanelSectionRow } from "cs2/ui";
import { useInfoText } from "info/city-info-text";
import { LocalizedNumber } from "info/localized-number";
import styles from "district/district-section.module.scss";

type Rating = "Unknown" | "Good" | "Warning" | "Bad";

/** Properties written by SolarpunkDistrictSection.OnWriteProperties. */
type DistrictSectionProps = {
  carShare: number;
  bicycleShare: number;
  publicTransportShare: number;
  walkingShare: number;
  carShareRating: Rating;
  personalCarCount: number;
  deliveryTruckCount: number;
  deliveryTruckRating: Rating;
  smallDeliveryVehicleCount: number;
  garbageTruckCount: number;
  carShareHistory: number[];
  deliveryTruckHistory: number[];
};

const History = ({ values, rating }: { values: number[]; rating: Rating }) => {
  const max = Math.max(...values, 1e-6);
  return (
    <div className={`${styles.history} ${styles[rating]}`}>
      {values.map((value, index) => (
        <div key={index} className={styles.historyBar} style={{ height: `${(100 * value) / max}%` }} />
      ))}
    </div>
  );
};

const Share = ({ value }: { value: number }) => <LocalizedPercentage value={value} max={1} />;
const Count = ({ value }: { value: number }) => <LocalizedNumber value={value} unit={Unit.Integer} />;

const DistrictSection = (props: DistrictSectionProps) => {
  const infoText = useInfoText();
  const unknown = props.carShareRating === "Unknown";

  return (
    <PanelSection tooltip={infoText("DISTRICT_TOOLTIP")}>
      <PanelSectionRow uppercase left={infoText("DISTRICT_TITLE")} />
      <PanelSectionRow left={infoText("TRIPS")} right={unknown ? infoText("NOT_ENOUGH_DATA") : undefined} />
      <PanelSectionRow subRow left={infoText("CAR")} right={<span className={styles[props.carShareRating]}><Share value={props.carShare} /></span>} />
      <PanelSectionRow subRow left={infoText("BICYCLE")} right={<Share value={props.bicycleShare} />} />
      <PanelSectionRow subRow left={infoText("PUBLIC_TRANSPORT")} right={<Share value={props.publicTransportShare} />} />
      <PanelSectionRow subRow left={infoText("WALKING")} right={<Share value={props.walkingShare} />} />
      <History values={props.carShareHistory} rating={props.carShareRating} />
      <PanelSectionRow left={infoText("VEHICLES")} />
      <PanelSectionRow subRow left={infoText("PERSONAL_CARS")} right={<Count value={props.personalCarCount} />} />
      <PanelSectionRow
        subRow
        left={infoText("DELIVERY_TRUCKS")}
        right={<span className={styles[props.deliveryTruckRating]}><Count value={props.deliveryTruckCount} /></span>}
      />
      <PanelSectionRow subRow left={infoText("SMALL_DELIVERY_VEHICLES")} right={<Count value={props.smallDeliveryVehicleCount} />} />
      <PanelSectionRow subRow left={infoText("GARBAGE_TRUCKS")} right={<Count value={props.garbageTruckCount} />} />
      <History values={props.deliveryTruckHistory} rating={props.deliveryTruckRating} />
    </PanelSection>
  );
};

/**
 * Adds the section to the game's selected info panel, keyed by the C# section's full type name
 * (SolarpunkMod.Info.SolarpunkDistrictSection): renaming or moving that class breaks the section silently.
 */
export const addDistrictSection = (components: Record<string, unknown>) => {
  components["SolarpunkMod.Info.SolarpunkDistrictSection"] = DistrictSection;
  return components;
};
