import { useLocalization } from "cs2/l10n";
import mod from "mod.json";

/** Returns a translator for the panel's texts, registered on the C# side (CityInfoLocale). */
export const useInfoText = () => {
  const { translate } = useLocalization();
  return (key: string) => translate(`${mod.id}.Info.${key}`);
};
