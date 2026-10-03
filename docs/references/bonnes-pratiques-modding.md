# Bonnes pratiques de modding CS2

> Recherche du 03/10/2026. **[V]** source lue · **[R]** référence qui dit avoir vérifié contre le jeu
> 1.6.2f1, à revérifier dans le décompilé · **[S]** supposé.

## Ressource n°1 : plugin `cs2-modding`

**[V]** [CitiesSkylinesModding/agents-plugins](https://github.com/CitiesSkylinesModding/agents-plugins)
(MIT, organisation communautaire de PrefabDumpMax et UrbanDevKit). Plugins Claude Code :

- **`cs2-modding`** : ≈ 240 000 mots, chaque affirmation **vérifiée contre 1.6.2f1** (marqueurs
  `VOLATILE:` / `UNVERIFIED:`). Mécanique (transport, dispatch, économie, climat, eau), technique
  (prefabs, sérialisation, patching, Burst, cycle de vie), toolchain Linux, publication, catalogue
  de mods commentés.
- **`unity-devtools`** : évalue du C# et lit l'ECS dans le jeu en cours (Linux annoncé, Proton [S]).
- **`coherent-gameface`** : pilote l'UI cohtml via CDP.

Installation (décision de l'humain) : `/plugin marketplace add CitiesSkylinesModding/agents-plugins`
puis `/plugin install cs2-modding@csmodding`.

## Docs

- **[V]** [Wiki officiel](https://cs2.paradoxwikis.com/Modding) : Modding_Toolchain, UI_Modding,
  Options_UI, Logging, Debugging, Localize_your_mod, **PrefabSystem**, Systems,
  How_To_Avoid_Memory_Leaks, Mod_security.
- **[V]** [Modding_Toolchain_on_Linux](https://cs2.paradoxwikis.com/Modding_Toolchain_on_Linux) :
  patch de `Mod.props` (environnement au lieu du registre), wrappers ModPostProcessor/ModPublisher
  (`unset DOTNET_*`, filtre stderr wineserver), variables `CSII_*` dans
  `~/.config/environment.d/60-cs2-modding.conf`. À confronter à `docs/modding-linux.md`.
- **[V]** Gabarits et libs : [CS2-Templates](https://github.com/River-Mochi/CS2-Templates),
  [UrbanDevKit](https://github.com/CitiesSkylinesModding/UrbanDevKit),
  [PrefabDumpMax](https://github.com/CitiesSkylinesModding/PrefabDumpMax) (diff des prefabs entre patchs).
- **Obsolète** : tout guide BepInEx d'avant la toolchain officielle 2024.

## Mods open source à lire

| Mod | Pour quoi |
|---|---|
| [AdvancedTransitOperations](https://github.com/huasyl/AdvancedTransitOperations) `TramTrain/AssetSystem.cs` | **Le plus proche de nous** : clone « Double Tram Track », voies `Train \| Tram`, `AddPrefab` vérifié, système qui s'éteint (`Enabled = false`) une fois fait |
| [Road Builder](https://github.com/JadHajjar/RoadBuilder-CSII) | Prefabs réseau par code ; sans le mod, routes « grises » et save qui charge |
| [Water Features](https://github.com/yenyang/Water_Features) | **Modèle de sauvegarde** : replie l'état custom dans le vanilla avant sérialisation |
| [Anarchy](https://github.com/yenyang/Anarchy) `Components/TransformRecord.cs` | `ISerializable` minimal, valeurs bornées au `Deserialize` |
| [Plop the Growables](https://github.com/algernon-A/PlopTheGrowables) | Plus petit exemple « désactiver un système vanilla + fork » |
| [Time2Work](https://github.com/ruzbeh0/Time2Work) | Remplacement d'une douzaine de systèmes |
| [Traffic](https://github.com/krzychu124/Traffic) | Outils, lanes, `AddPrefab` |
| [Scene Explorer](https://github.com/krzychu124/SceneExplorer) | Inspecteur ECS en jeu, indispensable |
| [Extended Transport Manager](https://github.com/klyte45/CS2-ExtendedTransportManager) · [Smart Transportation](https://github.com/ruzbeh0/SmartTransportation) | Lignes, modèles de véhicules |
| [Hall of Fame](https://github.com/toverux/HallOfFame) | UI soignée |

## Patterns

- **Systèmes [V]** : `updateSystem.UpdateAt<T>(SystemUpdatePhase.X)` dans `OnLoad`.
  `GetUpdateInterval` = **puissance de 2** (`262144 / updatesPerDay`).
- **Remplacer un système vanilla [R]** : ancrer le fork sur l'original, pas `UpdateAt` (qui le met en
  fin de phase) :
  ```csharp
  World.GetOrCreateSystemManaged<Vanilla>().Enabled = false;
  updateSystem.UpdateBefore<MyFork, Vanilla>(SystemUpdatePhase.GameSimulation);
  ```
  Piège : si le type vanilla est renommé, rien n'est désactivé, sans erreur.
- **Ordre de préférence [R]** : insérer un système → fork → réécrire l'`EntityQuery` d'un système
  vanilla → réflexion cachée → **Harmony en dernier** (9 mods sur 22 étudiés patchent).
- **Burst [R]** : un patch Harmony sur l'`Execute` d'un job Burst **ne s'exécute jamais**.
- **PrefabSystem [R]** :
  - lire la référence sur le `PrefabBase`, écrire dans les `*Data` ; modifier un `*Data` ne met pas
    à jour les instances posées ;
  - `AddPrefab` renvoie un `bool` et **avale les exceptions** → tester le retour ;
  - cloner soi-même = `Clone` + `Remove<ObsoleteIdentifiers>()` + `AddPrefab` (ce que fait
    `DuplicatePrefab`) ;
  - `TryGetPrefab` peut renvoyer `true` avec `null` ; mettre en cache l'`Entity`, jamais l'index ;
  - prefab dont un prérequis (DLC, mod) manque : non enregistré, sans erreur ;
  - homonymes `Game.Prefabs.X` / `Game.Buildings.X` → noms qualifiés.
  - Un initialiseur de champ C# d'une classe prefab **n'est pas** la valeur du jeu : lire le `*Data`.
- **Mod optionnel [R]** : `GameManager.instance.modManager`, `isValid && canBeLoaded`, nom complet
  avec délimiteur (`"Nom, "`) ; `ListModsEnabled()` depuis `OnLoad` ne voit qu'une partie des mods
  → enregistrer les systèmes conditionnels dans un callback différé ; cache `static bool?`.
- **Mémoire [V]** : `Dispose` de toute collection `Native*` (`OnDestroy` pour Persistent).

## Sauvegarde

- **[R]** Composant sans `ISerializable` / `IEmptySerializable` = **abandonné sans prévenir**.
- **[R]** Premier champ écrit = **entier de version du mod** (pas `reader.context.version`, qui est
  celle du jeu), branches additives ; `try/catch` dans `Deserialize` ne protège de rien.
- **[R]** Renommer un type sérialisé : `[FormerlySerializedAs]` de `Colossal.Serialization.Entities`.
- **[R]** **Retrait du mod** : nos composants disparaissent (ville intacte) ; nos prefabs par code
  deviennent des **placeholders** (bâtiment « fantôme », save qui charge). Confirmé par le README de
  Road Builder [V]. Pour l'état de sim : replier dans le vanilla (Water Features).

## Pièges

- **Patchs du jeu** : signatures et valeurs changent → lire en jeu, diff PrefabDumpMax à chaque
  version, logger les templates introuvables.
- **[R]** `PublicTransportFlags` ≠ `CargoTransportFlags` : bits divergents dès `RequiresMaintenance`,
  jamais de cast.
- **Publication [R]** : `AccessLevel` **sensible à la casse** (`private` en minuscules ou absent =
  **Public**) ; après la 1ʳᵉ publication, recopier l'Id dans `<ModId Value="…"/>` sinon doublon ;
  `<Dependency Id=…/>` ; `LongDescription` et `ChangeLog` en colonne 0 ; profils `PublishNewMod`,
  `PublishNewVersion`, `UpdatePublishedConfiguration`.

## Cargo tram : pas de nouveau `TransportType`

- **[V par absence]** Aucun mod CS2 n'ajoute de valeur à `TransportType` ni de cargo tram jouable.
- **[R]** Passager ou cargo = **trois interrupteurs** :
  1. connexion d'accès `Pedestrian` / `Cargo` ;
  2. paire `m_PassengerTransport` / `m_CargoTransport`, identique sur `TransportLineData` **et**
     `TransportStopData` ;
  3. composants du véhicule : `PublicTransportVehicleData` ou
     `CargoTransportVehicleData { m_Resources, m_CargoCapacity, … }`.

  Sélection via `TransportVehicleSelectData.SelectVehicle` : sans prefab véhicule cargo, la ligne
  reste vide.
- **[S] Hypothèse** : cargo tram = `TransportType.Tram` + ligne, arrêt et véhicule tram **clonés en
  mode cargo**. Ajouter une valeur d'enum est exclu (switch vanilla compilés Burst). À vérifier :
  `TransportLinePrefab`, `Game.Routes.InitializeSystem`, `PathUtils`, dispatch vers une
  `CargoTransportStation` de type tram.
