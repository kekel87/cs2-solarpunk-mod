# Base de connaissances — modding CS2 du Solarpunk Mod

> Créée le 05/10/2026. Jeu **1.6.2f1** (`VersionInternal("1.6.2f1 (767.21d1) [6300.26419]")`,
> `~/Documents/dev/cs2-decompile/src/Game/Properties/AssemblyInfo.cs:16`), moteur Unity 2022.3.71f1,
> éditeur Unity de la chaîne d'outils 2022.3.62f2 (les deux diffèrent normalement, voir
> `code-csharp-ecs.md`). Plugin Claude Code **cs2-modding 1.2.0** (19/09/2026, vérifié contre 1.6.2f1).
>
> Marqueurs : **[V]** vérifié (source lue, citée) · **[S]** supposé / non vérifié · date = jour de
> la vérification. Un fait non daté vaut 05/10/2026.

## But

Ne pas redécouvrir. Cette base **n'est pas une copie** du plugin `cs2-modding` (≈ 130 000 mots
de références techniques vérifiées) : c'est un **index** « quand je fais X, lire Y », adapté à nos
modules, plus ce que notre code a établi et les leçons de nos erreurs.

## Quoi lire quand

| Je vais… | Lire d'abord |
|---|---|
| Écrire ou modifier un système C# (ECS, prefab, sauvegarde, ordre) | `code-csharp-ecs.md` |
| Toucher au module UI (panneau, section, binding, traduction) | `ui-react-ts.md` |
| Brancher sur une mécanique du jeu (fret, dépôts, déchets, politiques, district, barre d'outils) | `jeu-mecaniques-utiles.md` |
| Générer ou importer un modèle 3D | `assets-3d.md` |
| Commencer un plan, lancer des agents, décider de tester | `workflow-et-lecons.md` |

Chaque fichier renvoie vers le plugin par chemin. Abréviations utilisées partout :

- `PLUGIN` = `~/.claude/plugins/cache/csmodding/cs2-modding/1.2.0/skills`
- `TECH` = `PLUGIN/cs2-modding/references/technique`, `MECA` = `PLUGIN/cs2-modding/references/mechanics`
- `UIREF` = `PLUGIN/cs2-modding-ui/references`
- `DEC` = `~/Documents/dev/cs2-decompile/src/Game` (décompilé 1.6.2f1, `fichier.cs:ligne`)
- `UIBUNDLE` = `~/Documents/dev/cs2-decompile/ui/index.js` (bundle UI du jeu reformaté, 140 104 lignes)
- Registre machine du plugin : `~/.cs2-modding/setup.md`

Les **skills** du plugin se chargent par l'outil Skill : `cs2-modding:cs2-modding` (tronc : ECS,
prefabs, sauvegarde, mécaniques), `cs2-modding:cs2-modding-ui` (bindings, frontend),
`cs2-modding:cs2-mod-project` (build, toolchain Linux, publication), `cs2-modding:cs2-modding-setup`
(décompilation, options de lancement, catalogue de mods à lire). Les invoquer **avant** de coder
dans leur domaine (leçon du 05/10, `workflow-et-lecons.md`).

## Index du plugin par module du mod

| Module / tâche | Fichiers du plugin |
|---|---|
| **Fret, rail, Local Hub** (vendeurs, stock, achats) | `MECA/economy-and-companies/trade-and-restocking.md`, `economy-and-companies.md`, `production-graph.md` |
| **Dépôts, sélection de véhicules, lignes** | `MECA/transportation-and-vehicles/depots-and-dispatch.md`, `lines-and-fleet.md`, `vehicles.md`, `stops-and-boarding.md`, `transit-routing.md`, `outside-connections.md` |
| **Déchets** (collecte, installations, dispatch) | `MECA/city-services-and-coverage/city-services-and-coverage.md` (§ services), `dispatch.md`, `coverage.md`, `budget-workforce-and-upkeep.md` |
| **Politiques de district** | `MECA/city-state-and-progression/policies.md` ; districts : `MECA/zoning-buildings-and-land-value/districts-and-themes.md` |
| **Stationnement, trafic** (fin du stationnement, fourrière) | `MECA/roads-and-traffic/parking.md`, `travel-weights.md`, `route-selection.md`, `MECA/citizens-and-households/travel-and-trips.md` |
| **Barre d'outils, déblocage** | `MECA/city-state-and-progression/unlocking.md`, `progression.md` ; `TECH/custom-tools/toolbar-position.md` |
| **Climat urbain** (futur) | `MECA/environment-and-pollution/*` (`climate-and-weather.md`, `water-and-groundwater.md`, `cell-maps.md`) |
| **Prefabs créés par code** (clone, extension, nouveau type) | `TECH/prefabs-and-assets/prefabs-and-assets.md` (§ *When you may do it*, *Cloning*, *Editing a prefab*), `prefab-data-initialisation.md`, `regenerating-a-prefab.md` |
| **Ordre des systèmes, phases, cycle de vie** | `TECH/mod-lifecycle-and-ordering/mod-lifecycle-and-ordering.md`, `phase-catalogue.md` ; `DEC/Game.Common/SystemOrder.cs` |
| **Corps d'un système** (requêtes, tags, barrières, jobs) | `TECH/ecs-in-this-game/ecs-in-this-game.md`, `universal-tags.md`, `update-frame-buckets.md` |
| **Performance, dépendances, mémoire** | `TECH/performance-and-memory/performance-and-memory.md`, `cleanup-components.md`, `colossal-collections.md`, `burst-at-debug-time.md` |
| **Sauvegarde, retrait du mod** | `TECH/save-serialization/save-serialization.md` (§ *Choosing where your data lives*, *Type identity, renames and uninstalls*), `deserialize-phase-census.md` |
| **Harmony** (dernier recours) | `TECH/patching/patching.md`, `what-gets-patched.md` ; alternatives : `TECH/placement-definitions/placement-definitions.md` |
| **Options (ModSettings)** | `TECH/settings-and-input/settings-and-input.md`, `building-the-options-page.md`, `attribute-catalog.md` |
| **Traductions** | `TECH/localization/localization.md`, `vanilla-namespaces.md` ; nombres/unités : `TECH/units-and-formatting/units-and-formatting.md` |
| **Compatibilité, dépendances de mods** (Tigon, etc.) | `TECH/mod-compatibility/mod-compatibility.md`, `cross-mod-api.md`, `shared-namespaces.md` |
| **Diagnostic** (le mod ne fait rien, logs) | `TECH/diagnostics/diagnostics.md`, `loader-states.md`, `reporting-to-the-player.md` ; menu debug : `TECH/debug-menu/debug-menu.md` |
| **Trouver un type dans le décompilé** | `TECH/navigating-the-decompile/navigating-the-decompile.md`, `decompiler-artifacts.md` |
| **UI : bindings C#↔JS** | `UIREF/binding-layer/binding-layer.md` |
| **UI : injection (sections, anchors, registry)** | `UIREF/frontend-and-injection/frontend-and-injection.md`, `promised-registry-paths.md` |
| **UI : build webpack, rafraîchir les types** | `UIREF/ui-build-and-devloop/ui-build-and-devloop.md` |
| **Build Linux** | `PLUGIN/cs2-mod-project/references/linux-toolchain.md`, `build-pipeline.md` ; chez nous : `docs/modding-linux.md` |
| **Publication Paradox Mods** | `PLUGIN/cs2-mod-project/references/publishing.md` |
| **Mods open source à lire pour un problème** | `PLUGIN/cs2-modding-setup/references/mod-catalog.md` ; chez nous : `docs/references/bonnes-pratiques-modding.md` |
| **Débogueur, inspection en jeu** | `PLUGIN/cs2-modding-setup/references/debug-patching-linux.md` (patch **non installé** au 05/10 : `Debug patch: (none)` dans le registre) |

Lire un marqueur du plugin : `VOLATILE:` = vrai à 1.6.2f1, peut bouger, l'endroit à revérifier est
nommé ; `UNVERIFIED:` = piste, à confirmer avant de bâtir dessus (`PLUGIN/cs2-modding/SKILL.md`,
§ *Reading a marked claim*).

## Mise à jour (jeu ou plugin qui change)

1. **Plugin mis à jour** : lire `~/.claude/plugins/cache/csmodding/cs2-modding/<version>/CHANGELOG.md`,
   repérer les références touchées, corriger les chemins de l'index ci-dessus (le numéro de version
   est dans `PLUGIN`).
2. **Jeu mis à jour** :
   - re-décompiler (skill `cs2-modding:cs2-modding-setup`, § *Refreshing after a game update*) ; le
     décompilé est un dépôt git, un commit par version → `git -C ~/Documents/dev/cs2-decompile diff`
     sur les fichiers cités ici ;
   - si Unity / Entities change : refaire la toolchain (`docs/modding-linux.md`, correctif `ntsync`
     à réappliquer) ;
   - UI : `npm run update` dans `src/SolarpunkMod/UI` puis **réappliquer** `path.join` dans
     `webpack.config.js` (`ui-react-ts.md`) ;
   - revalider chaque **[V]** de cette base dont le fichier cité a changé dans le diff ; dater la
     revalidation ; passer en **[S]** ce qu'on ne peut pas revérifier.
3. Mettre à jour la version et la date en tête de ce fichier.
