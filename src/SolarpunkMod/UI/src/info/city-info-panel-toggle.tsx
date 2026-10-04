import { useState } from "react";
import { FloatingButton, Portal } from "cs2/ui";
import { CityInfoPanel } from "info/city-info-panel";
import { useInfoText } from "info/city-info-text";

export const CityInfoPanelToggle = () => {
  const [isOpen, setIsOpen] = useState(false);
  const infoText = useInfoText();

  return (
    <>
      <FloatingButton
        src="Media/Game/Icons/CityPark.svg"
        selected={isOpen}
        tooltipLabel={infoText("TITLE")}
        onSelect={() => setIsOpen(!isOpen)}
      />
      {isOpen && (
        <Portal>
          <CityInfoPanel onClose={() => setIsOpen(false)} />
        </Portal>
      )}
    </>
  );
};
