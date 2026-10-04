# Architecture

> Projet C# généré le 03/10/2026 par le template officiel du jeu (`ColossalOrder.ModTemplate`,
> sans écran d'options), import `Mod.props`/`Mod.targets` via `$(CSII_TOOLPATH)` (correctif Linux).
> Vignette du template non reprise (dépôt public) : `Properties/Thumbnail.png` à créer avant publication.
> `PublishConfiguration.xml` : `AccessLevel` Private tant que le mod n'est pas prêt ; balises vides retirées.

## Dépôt

```
CLAUDE.md                 consignes Claude
docs/design.md            vision, modules, mods compagnons
docs/architecture.md      ce fichier
docs/modding-linux.md     toolchain Fedora/Proton
docs/references/          recherches (code du jeu, mods existants)
docs/plans/               plans en cours (NNN-nom.md)
scripts/memory/           outils du graphe de mémoire
src/SolarpunkMod/         le mod : SolarpunkMod.csproj, Mod.cs (IMod), Properties/ (publication Paradox Mods)
  Info/                   panneau d'info : SolarpunkInfoUISystem (calcul + bindings), CityInfoLocale (textes en/fr)
  UI/                     module UI TypeScript/React (scaffold officiel create-csii-ui-mod)
```

## Module UI

`UI/` vient du template officiel (`.ModdingToolchain/npx-create-csii-ui-mod/template`). Les types `UI/types/*.d.ts`
(API UI de Colossal Order, fournis par l'outil officiel pour être copiés) restent commités : pratique
courante des mods publics, nécessaires au build ; à rafraîchir par `npm run update` après une mise à
jour du jeu. Seul écart
avec le stock : `webpack.config.js` sort via `path.join` (sinon, sous Linux, le bundle atterrit hors
de `Mods/`) — à réappliquer après un `npm run update`. `dotnet build` construit l'UI (cible
`BuildUI`, après `DeployWIP` qui vide le dossier du mod ; `npm ci` si `node_modules` manque).
Sortie : `SolarpunkMod.mjs` + `.css` à côté de la DLL. Identité commune C# / UI : `Mod.Id` =
`mod.json` `id` = `SolarpunkMod`, sert de groupe de bindings et de préfixe des clés de traduction.
Textes : enregistrés côté C# (`MemorySource`), lus côté UI par `useLocalization`.

## Principe d'organisation du mod (cible)

Un seul assembly, un dossier par module (`Garbage/`, `CargoTransit/`, `Climate/`…), chacun avec
ses systèmes ECS et ses prefabs créés par code. Un point d'entrée `Mod.cs` (`IMod.OnLoad`) qui
enregistre les systèmes et détecte les mods compagnons optionnels.
