import * as l10n from "cs2/l10n";
import { LocComponent, LocalizedNumberProps } from "cs2/l10n";

// The shipped cs2/l10n typings declare LocalizedNumber as both an interface and a component, so
// TypeScript only sees the interface; the game does export the component at runtime.
export const LocalizedNumber = (l10n as unknown as { LocalizedNumber: LocComponent<LocalizedNumberProps> }).LocalizedNumber;
