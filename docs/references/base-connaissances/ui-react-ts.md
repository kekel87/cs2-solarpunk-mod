# UI React / TypeScript (API cs2 du jeu)

> 05/10/2026, jeu 1.6.2f1, plugin cs2-modding 1.2.0 (skill `cs2-modding:cs2-modding-ui`).
> Abréviations : `README.md`. [V] vérifié · [S] supposé.
> Référence complète : `UIREF/binding-layer/binding-layer.md`,
> `UIREF/frontend-and-injection/frontend-and-injection.md`, `UIREF/ui-build-and-devloop/ui-build-and-devloop.md`.

## Où est la vérité

- **Front** : le bundle installé `Cities2_Data/Content/Game/UI/index.js` ; copie lisible
  `UIBUNDLE` (140 104 lignes, notée dans `~/.cs2-modding/setup.md`). Un grep du décompilé C# ne
  dit **rien** sur un composant ou un module JS [V `PLUGIN/cs2-modding-ui/SKILL.md`].
- **C#** : `Colossal.UI.Binding` (types de bindings) et `DEC/Game.UI.*` (systèmes).
- **Types TS** : `src/SolarpunkMod/UI/types/*.d.ts`, copiés du scaffold officiel, commités.

## Identité commune

`Mod.Id` (C#) = `mod.json` `id` = `"SolarpunkMod"` : **groupe de bindings** et **préfixe des clés de
traduction** (`docs/architecture.md`). Rien dans la couche de bindings n'empêche deux mods de prendre
le même groupe : le préfixe par l'id du mod est la seule protection [V `binding-layer.md` § *Three kinds*].

## Bindings C# → JS

- Trois sortes : **push** (`ValueBinding<T>`, `GetterValueBinding<T>`, `RawValueBinding`…),
  **trigger** (`TriggerBinding<…>`, JS → C#, rien ne revient), **call** (`CallBinding`, requête/réponse).
- Système : `UISystemBase` ; `AddBinding` (on pousse soi-même par `binding.Update(v)`) ou
  `AddUpdateBinding` (pompé par `UISystemBase.OnUpdate` : **appeler `base.OnUpdate()`** si on le
  surcharge, et le système doit être dans une phase, `UIUpdate`) [V `binding-layer.md` § *Registration*].
- `gameMode => GameMode.Game` : le système est désactivé hors partie (`OnGamePreload` règle `Enabled`)
  [V § *The game-mode gate*]. Modèle : `Info/SolarpunkInfoUISystem.cs`.
- `ValueBinding<T>.Update` ne pousse que si la valeur change ; avec un type référence muté en place,
  **rien ne part** [V § *Choosing a push binding*].
- `binding.active` = le front est abonné → sert de garde « panneau ouvert » (`IsPanelOpen`).
- `ValueBinding<long>` **lève à la construction** (pas de writer `long`) ; un writer qui reçoit
  `null` (string, liste) lève aussi [V § *Readers, writers*].
- Côté JS : `bindValue<T>(mod.id, "nom", défaut)` puis `useValue(binding)` (`UI/src/info/city-info-bindings.ts`).

## Injection dans l'interface du jeu (`moduleRegistry`)

Point d'entrée : `UI/src/index.tsx` exporte un `ModRegistrar`.

| Opération | Effet | Composition entre mods |
|---|---|---|
| `append(ancre, Composant)` | monte un composant sur une ancre (`GameTopLeft`…) | chaîne |
| `append(chemin, export, Composant, index?)` | insère dans les `children` de l'export | chaîne |
| `extend(chemin, export, cb)` | remplace l'export par `cb(valeurActuelle)` | chaîne (dernier inscrit = le plus externe) |
| `override(chemin, export, valeur)` | remplace | **écrase** les autres mods |
| `add(chemin, module)` | nouveau module | lève si existe déjà |

[V `frontend-and-injection.md` § *The module registry*, § *Composition*]

- Ancres typées : `Menu`, `Editor`, `Game`, `GameTopLeft`, `GameTopRight`, `GameBottomRight`,
  `UniversalModMenu` (`types/modding.d.ts:6`) ; `GameBottomLeft` existe au rendu mais n'est pas typée.
  `GameTopLeft` est **conditionnelle** (absente dans la mise en page `topLayout`) [V § *The append anchors*].
- Passer une **fonction composant simple** à `append` (pas `React.memo`/`forwardRef`).
- Le registre **lève des chaînes**, pas des `Error`. **Un registrar qui lève fait taire tous les
  mods chargés après lui** (ordre variable) → envelopper nos appels risqués dans `try/catch`
  [V § *Composition*].
- L'ordre entre mods = ordre de fin d'import, non déterministe.

## Sections du panneau d'info sélectionné

- C# : classe `InfoSectionBase` (`DEC/Game.UI.InGame/InfoSectionBase.cs`), enregistrée par
  `SelectedInfoUISystem.AddMiddleSection` (`SelectedInfoUISystem.cs:207`, aussi `AddTopSection`
  `:202`, `AddBottomSection` `:212`) ; `visible`, `OnProcess`, `OnWriteProperties`, `Reset` [V].
- Le type envoyé au front est `GetType().FullName` (`InfoSectionBase.cs:127`) [V 05/10].
- JS : `extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
  "selectedInfoSectionComponents", cb)` ; la **clé = nom complet du type C#**, ici
  `"SolarpunkMod.Info.SolarpunkDistrictSection"` (`UI/src/district/district-section.tsx`) [V
  `promised-registry-paths.md` § *The selected-info section map*]. Renommer ou déplacer la classe
  C# casse la section **sans erreur** : changer les deux ensemble.
- Le composant reçoit les propriétés écrites par `OnWriteProperties` (noms identiques, à typer à la main).

## Composants `cs2/ui`

- `PanelSection` / `PanelSectionRow` sont des **alias** de `InfoSection` / `InfoRow` du jeu
  (`UI/types/ui.d.ts:709-710` ; exports `UIBUNDLE:12685-12686`) [V 05/10]. Props utilisées :
  `tooltip`, `left`, `right`, `uppercase`, `subRow`.
- Autres exports utiles du même module : `Panel`, `PanelFoldout`, `Tooltip`, `Scrollable`,
  `Portal`, `MenuButton` (`UIBUNDLE:12680-12689`) [V].
- Composants non exportés par `cs2/*` : les atteindre par `moduleRegistry` (§ *Reusing the game's
  unexported components* de `frontend-and-injection.md`).

## Localisation

- Textes enregistrés **côté C#** : `GameManager.instance.localizationManager.AddSource("en-US",
  new MemorySource(dict))`, une source par langue (`*Locale.cs`). Clés du mod préfixées
  `SolarpunkMod.` ; pour les objets du jeu, **clés vanilla** (`Policy.TITLE[<nom prefab>]`,
  `SubServices.NAME[<nom>]` pour un onglet, plan 003) [V `TECH/localization/localization.md`,
  `vanilla-namespaces.md`].
- `RemoveSource` puis `AddSource` de la **même** instance = no-op ; et les sources retirées
  reviennent au prochain rechargement d'assets [V `localization.md` § *RemoveSource and AddSource*].
- Côté JS : `useLocalization().translate(clé)` (`UI/src/info/city-info-text.ts`).
- Côté C# vers UI : `LocalizedString.Id(clé)` ; une **string nue** est convertie en `Id` (clé), pas en
  texte → `LocalizedString.Value(texte)` pour du texte final [V `localization.md` § *LocalizedString*].
- Nombres : `LocalizedPercentage`, `LocalizedNumber` + `Unit` de `cs2/l10n` (unités du joueur :
  `TECH/units-and-formatting/units-and-formatting.md`).

## Build

- `dotnet build` lance la cible `BuildUI` (`npm ci` si besoin, puis `npm run build`) → `SolarpunkMod.mjs`
  + `.css` à côté de la DLL dans `Mods/SolarpunkMod` (`docs/architecture.md`).
- Le `.css` doit s'appeler comme le `.mjs` ; un `hasCSS` vrai sans feuille qui répond **bloque l'UI
  de tous les mods** [V `ui-build-and-devloop.md` § *The stylesheet contract*].
- Modules partagés fournis par le jeu (`React`, `cs2/*`…) = `externals` webpack : ne jamais les
  embarquer.
- Rechargement à chaud de l'UI seulement (pas du DLL) (`docs/modding-linux.md`).

## Pièges connus

- **Linux** : le `webpack.config.js` du scaffold écrit vers un chemin à `\\` → dossier hors de `Mods/`.
  Correctif `path.join(CSII_USERDATAPATH, "Mods", MOD.id)` (`webpack.config.js:14`), **écrasé par
  `npm run update`** : le réappliquer [V `ui-build-and-devloop.md` § build ; `docs/modding-linux.md`].
- **`npm run clean` supprime aussi le DLL C#** (même dossier `Mods/<id>`) [V § *Refreshing the declarations*].
- **Types `cs2/l10n` défectueux** : `LocalizedNumber` est déclaré à la fois interface (`types/l10n.d.ts:80`)
  et composant réexporté sous le même nom (`:174`, `:207`) → TS ne voit que l'interface ; cast dans
  `UI/src/info/localized-number.ts`. Même fichier : `export export const LocalizedPercentage` (`:179`)
  [V 05/10]. À revérifier après chaque `npm run update`.
- **Types vendus jamais rafraîchis** : compilent contre l'ancienne forme, échouent en silence après
  une mise à jour du jeu → `npm run update` dans le rituel de mise à jour (`README.md`).
- **Collisions CSS** : `localIdentName: "[local]_[hash:base64:3]"` (`webpack.config.js:64`) = 3 caractères
  de hash partagés par **tous** les mods ; préfixer par l'id du mod évite les collisions (édition
  écrasée par `npm run update`) [V `ui-build-and-devloop.md` § *The stylesheet contract*]. [S] Non
  fait chez nous à ce jour.
- `url(Media/…)` dans un SCSS fait échouer le build sans `url.filter` [V même §].
- Cohtml (Coherent Gameface) n'est pas Chrome : vérifier qu'une propriété CSS/JS existe (plugin
  `coherent-gameface`, non installé) [S].
- `Cannot abort request with ID` dans les logs : bruit de l'UI du jeu (`docs/modding-linux.md`).

## TypeScript / React — conventions du dépôt

- Fichiers `kebab-case.tsx`, un dossier par panneau (`info/`, `district/`), styles en
  `*.module.scss` [V dépôt].
- Composants fonctionnels, props typées par un `type` local qui documente leur source C#
  (`DistrictSectionProps`, commentaire « Properties written by … OnWriteProperties »).
- Pas de `any` ; cast `as never` / `as unknown as` seulement aux frontières mal typées du jeu, avec
  un commentaire (`index.tsx`, `localized-number.ts`).
