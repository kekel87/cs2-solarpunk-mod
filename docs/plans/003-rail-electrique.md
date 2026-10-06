---
status: ready
created: 2026-10-05
updated: 2026-10-05
---

# Plan 003 — Rail électrique (onglet « Rail non électrique » + option « Trains électriques seulement »)

## Ce que tu verras en jeu

- Le menu Transport a un nouvel onglet **« Rail non électrique »**, placé juste après Train, avec l'icône pétrole. Il contient les voies, gares, ponts et yards Tigon sans caténaire.
- L'onglet **Train** ne garde que l'électrifié : voies avec caténaire, Dual Electric, 3rd Rail et tout le vanilla.
- Options → Solarpunk Mod : la case **« Trains électriques seulement »** est **décochée par défaut**. Cochée, elle ne force l'électrique que si voyageurs, fret ET déchets ont un modèle électrique ; sinon rien n'est forcé et le log dit pourquoi.
- Le log liste, au chargement, les modèles électriques/diesel trouvés par usage (voyageurs, fret, déchets).
- Si tu décoches la case en cours de partie, les diesels reviennent dans le sélecteur, sans recharger.
- Si tu retires le mod, la partie charge : les voies déjà posées sont toujours là et le non électrique retourne dans l'onglet Train.

## Contexte

- Décision humaine du 05/10/2026 : on fait un onglet plutôt que de cacher, plus une option. On écrit **notre propre code, pas ExtraLib** : pas de dépendance nouvelle.
- Mod Tigon : *Tigons Rail Infrastructure* (133250), playset du joueur. Sur ce mod, voir la décision 010 (« entrées non électrifiées à masquer si possible »).
- Recherche du 05/10/2026 (conversation, non consignée) : cette recherche a montré que le **jeu préfère le diesel**. Score `+1` si `EnergyTypes.Fuel` (`Game/Game.Prefabs/TransportVehicleSelectData.cs:458`), et seul le meilleur score est tiré au sort (`PickVehicle`, `:809-822`). Donc, avec Tigon et sans modèle choisi sur la ligne, les trains partent en diesel.

## Faits vérifiés dans le décompilé (1.6.2f1)

**Barre d'outils**
- `UIObject.LateInitialize` ajoute le prefab au buffer `UIGroupElement` de sa catégorie (`UIGroupPrefab.AddElement`, `UIGroupPrefab.cs:18-23`). Il pose aussi une `UnlockRequirement` et `UIObjectData.m_Group` (`UIObject.cs:67-83`).
- La barre d'outils affiche exactement ce buffer (`ToolbarUISystem.BindAssets`, `:410-432`). Un onglet vide est masqué (`GetSortedCategories`, `:396-407`). Le surlignage de l'onglet d'un asset passe par `UIObjectData.m_Group` (`:830-835`, `:1091-1093`).
- Un `UIAssetCategoryPrefab` dont `m_Menu` est renseigné s'insère tout seul dans le menu à son `LateInitialize` (`UIAssetCategoryPrefab.cs:30-39`).
- Les données du menu sont dans `UIAssetCategoryData.m_Menu`, une entité. L'icône et l'ordre viennent du `UIObject` de la catégorie (`m_Icon`, `m_Priority`).
- Le nom affiché d'un onglet utilise la clé `SubServices.NAME[<nom du prefab>]` (`PrefabUISystem.cs:1509`).
- Prefabs Tigon (dézippés depuis `133250_7/TO Rail Asset Pack.cok`) :
  - ceux qui ont un `m_Group` sont tous dans la catégorie vanilla Train (`UnityGUID:1239dee5…`) ;
  - le non électrique ne se repère **que par le nom** : `No Cable`, `No Cables`, `Plain`, `Non-electric`/`Non-Electric`, et `NE` en suffixe ou en mot (`TrainStation02NE`, `MediumTrainCoalYardNE`, `TO_TerminalRailYard02 NE`).

**Énergie des trains**
- Le départ d'un dépôt utilise `TransportDepotData.m_EnergyTypes` (`TransportDepotAISystem.cs:648`), combiné avec les améliorations installées par un **OU** (`TransportDepotData.cs:25`, `CombineStats` en `TransportDepotAISystem.cs:247`). Une amélioration Fuel (`TO_RailYard02 NE Maintenance Hall`) réintroduit donc le diesel si on ne la corrige pas aussi.
- Le sélecteur de modèle d'une ligne fait l'union des énergies des dépôts de la ville (`SelectVehiclesSection.cs:55-72`), puis appelle `ListVehicles` avec cette énergie (`:103`, `:375`).
- Une loco dont l'énergie ne correspond pas est ignorée **avant** que le modèle choisi sur la ligne soit validé (`TransportVehicleSelectData.cs:449-453`). Une ligne réglée sur un diesel recevra donc des trains électriques, sans qu'on touche à ses données sauvegardées.

**Ce qu'on écarte**
- **Le trafic de transit des connexions extérieures** n'est pas couvert. L'énergie y est codée en dur (`FuelAndElectricity`, `TrafficSpawnerAISystem.cs:191`, dans un job Burst), aucune donnée ne permet d'agir dessus, et ce n'est pas un levier « data ».
- **`Locked` est écarté** :
  - dans la barre d'outils, un prefab verrouillé s'affiche grisé au lieu de disparaître (`ToolbarUISystem.cs:380`) ;
  - surtout, l'état `Locked` des prefabs est **écrit dans la sauvegarde** (`BeginPrefabSerializationSystem.cs:83`, `ResolvePrefabsSystem.cs:62-75`). Verrouiller les diesels risquerait de les laisser verrouillés après le retrait du mod.

**Options du mod**
- On hérite de `Game.Modding.ModSetting` (`ModSetting.cs:13`), on appelle `RegisterInOptionsUI()`, et `Apply()` déclenche `onSettingsApplied` (`Game.Settings/Setting.cs:24,157-160`).
- Persistance : `AssetDatabase.global.LoadSettings(...)` (`Colossal.IO.AssetDatabase/AssetDatabase.cs:613`) + l'attribut `[FileLocation]` (`FileLocationAttribute.cs`).
- Tout est dans `Game` et `Colossal.IO.AssetDatabase`, déjà référencés : **aucune modification du `.csproj`**, seulement de nouveaux fichiers `.cs`.

## Décisions

1. **Déplacer, pas cacher.** On retire les prefabs Tigon non électriques du buffer de l'onglet Train et on les met dans notre onglet, en mettant aussi à jour `UIObjectData.m_Group`. Rien n'est supprimé.
2. **Repérage par nom**, avec une regex sensible à la casse : `No Cables?|\bPlain\b|Non-[Ee]lectric|NE\b`. On ne l'applique qu'aux prefabs de l'onglet Train (vanilla et mods confondus), et chaque prefab déplacé est écrit dans le log.
3. **L'onglet est créé par code**, comme la politique du lot 1 bis :
   - `UIAssetCategoryPrefab` nommé `SolarpunkNonElectricRail` ;
   - son `m_Menu` = le menu de l'onglet Train, et sa priorité = celle de Train + 1 ;
   - icône vanilla `Media/Game/Icons/Oil.svg`, présente dans le bundle UI, qui signale le diesel ;
   - le menu et l'onglet Train sont retrouvés par les données (via `UIObjectData.m_Group` d'un prefab trouvé), sans nom codé en dur.
4. **Option « Trains électriques seulement », décochée par défaut** (décision humaine après revue
   game-designer : Tigon n'a aucune loco de fret électrique et son wagon-poubelle `TO_TrainTrashCar01`
   est marqué Fuel ; un même dépôt tire voyageurs et fret, et un tirage raté ne fait rien partir
   sans log, `TransportDepotAISystem.cs:648-651`). On coche ce soir en recette pour mesurer.
   - Effet : `m_EnergyTypes = Electricity` sur **tous** les prefabs qui ont `TransportDepotData.m_TransportType == Train`, y compris les améliorations.
   - Les valeurs d'origine sont gardées en mémoire et restaurées quand on décoche.
5. **Wagons jamais bloqués** : le filtre d'énergie s'applique aussi aux wagons (`TransportVehicleSelectData.cs:436,449`).
   Quand l'option est active, les prefabs de train **sans moteur** (wagons, `TrainData` d'une voiture
   non motrice — à identifier dans le décompilé) passent à `EnergyTypes.None`, qui passe toujours le
   filtre (`:449`) ; valeurs d'origine restaurées en décochant.
6. **Garde-fou global** : on ne force que si **voyageurs ET fret ET déchets** ont chacun au moins une
   loco électrique tirable pour le thème de la ville (`CheckRequirements`, `:454`). Sinon on ne force
   rien du tout et on écrit un avertissement. Dans tous les cas, log du recensement par usage.

## Étapes de dev

Dossier : `src/SolarpunkMod/Rail/`. Les options vont à la racine, parce qu'elles servent à tout le mod.

1. **`src/SolarpunkMod/ModSettings.cs`**
   - `ModSettings : ModSetting`, avec `[FileLocation(Mod.Id)]` et une propriété `ElectricTrainsOnly` (défaut `false`) ;
   - `SetDefaults()`.
2. **`src/SolarpunkMod/ModSettingsLocale.cs`** : textes en/fr pour le titre du mod, le libellé et la description de l'option, via `GetSettingsLocaleID` / `GetOptionLabelLocaleID` / `GetOptionDescLocaleID`, enregistrés en `MemorySource` comme `PolicyLocale`.
3. **`Mod.cs`**
   - créer `ModSettings`, `RegisterInOptionsUI()`, `AssetDatabase.global.LoadSettings(Mod.Id, settings, new ModSettings(this))` ;
   - l'exposer en `static` pour les systèmes ;
   - `UnregisterInOptionsUI()` dans `OnDispose` ;
   - enregistrer les deux systèmes Rail, sans phase, comme `EndResidentialParkingPolicySystem`.
4. **`Rail/NonElectricRailCategorySystem.cs`**, en `OnGamePreload`, une seule fois par session (garde `m_Prefab != null`) :
   - trouver l'onglet Train : la première entité de prefab dont le nom correspond à la regex et dont `UIObjectData.m_Group` porte `UIAssetCategoryData` ;
   - en déduire le menu (`UIAssetCategoryData.m_Menu` → `PrefabSystem.GetPrefab<UIAssetMenuPrefab>`) ;
   - créer et ajouter notre `UIAssetCategoryPrefab` ;
   - déplacer chaque élément correspondant : retrait de `UIGroupElement` côté Train, ajout de `UIGroupElement` + `UnlockRequirement(RequireAny)` côté nôtre, puis `UIObjectData.m_Group` = nous ;
   - opération idempotente : on saute ce qui est déjà chez nous ;
   - si Tigon est absent, aucun prefab ne correspond : on ne crée pas l'onglet et on ne fait rien.
5. **`Rail/RailLocale.cs`** : `SubServices.NAME[SolarpunkNonElectricRail]` = « Non-electric Rail » / « Rail non électrique ». Si le jeu affiche une infobulle d'onglet, ajouter aussi sa clé.
6. **`Rail/ElectricTrainsOnlySystem.cs`**
   - applique ou restaure l'énergie des dépôts en `OnGameLoadingComplete` et sur `onSettingsApplied`, via `EntityManager.SetComponentData` depuis le thread principal ;
   - garde un dictionnaire des valeurs d'origine (prefab → `EnergyTypes`) ;
   - wagons à `None` (décision 5) et garde-fou global (décision 6) ;
   - log : recensement voyageurs / fret / déchets × électrique / diesel, dépôts modifiés.
7. `docs/architecture.md` : ajouter `Rail/` et `ModSettings` à l'arborescence (le `doc-keeper` le fera à la finalisation).
8. `/simplify`, puis `dotnet build` vert.

## Sauvegarde

- Rien de nouveau n'est sérialisé. Les buffers d'onglet, `UIObjectData` et `TransportDepotData` sont des données de prefab, reconstruites à chaque lancement. L'option est stockée dans les réglages du joueur, pas dans la partie.
- Seule trace : l'état `Locked` de **notre** prefab d'onglet, écrit comme pour tout prefab débloquable. Un prefab inconnu au rechargement est déjà toléré : c'est le précédent de la politique du lot 1 bis, retrait du mod testé.
- Retrait du mod : les voies déjà posées sont des objets ordinaires de Tigon, intacts. Les lignes gardent leur modèle choisi.

## Risques et hypothèses à vérifier en jeu

- **[S] L'onglet est verrouillé (grisé)** : notre catégorie naît `Locked`. Elle compte sur l'`UnlockSystem` et les `UnlockRequirement(RequireAny)` des éléments déplacés. Repli : en `OnGameLoadingComplete`, désactiver `Locked` sur l'onglet dès qu'un de ses éléments est débloqué.
- **[S] La barre d'outils ne se rafraîchit pas** : comme le déplacement se fait en `OnGamePreload`, avant le premier affichage, ce n'est pas attendu.
- **[S] Il n'existe aucune loco électrique de fret vanilla** (thème NA notamment). C'est ce que couvre le garde-fou ; le log le dira.
- **[S] `TransportDepotData` est réécrit au rechargement** : il est `ISerializable`. On vérifie qu'une partie sauvegardée avec l'option cochée charge **sans** le mod avec ses diesels (scénario 6).
- **Faux positif ou faux négatif de la regex** : on relit dans le log la liste des prefabs déplacés à la première recette.
- **Tigon renomme ses assets** : la liste se décale, sans casser la partie.

## Scénarios de recette

1. **Onglet** : le menu Transport montre « Rail non électrique » (icône pétrole) juste après Train.
   - Il contient `Twoway Train Track Plain`, `TrainStation01 Non-electric`, `CargoTrainTerminal01NE` et `TrussArchBridge01NE`.
   - L'onglet Train ne les contient plus, mais garde `TO_Twoway Train Track Dual Electric` et les voies vanilla.
2. **Poser** une voie et une gare depuis le nouvel onglet : l'outil, le coût et le raccordement marchent comme avant.
3. **Log au chargement** : le recensement voyageurs / fret / déchets s'affiche ; l'option décochée ne change rien au jeu.
4. **Option cochée**, sur une ligne de train voyageurs puis une ligne fret (si le garde-fou laisse forcer, sinon noter l'avertissement) :
   - le sélecteur de modèle ne liste aucun diesel ;
   - les trains qui sortent sont électriques (voir l'infobulle du véhicule) ;
   - une ligne précédemment réglée sur un diesel reçoit des trains électriques.
5. **Décocher l'option** en cours de partie : les diesels réapparaissent dans le sélecteur sans recharger. La recocher les fait disparaître.
6. **Fret et déchets toujours servis**, option cochée : sur 10 minutes, gares de marchandises, yards NE et Garbage Yard Tigon envoient toujours des trains.
7. **Retrait du mod** :
   - sauvegarder avec l'option cochée et une voie Plain posée ;
   - quitter et retirer le mod ;
   - recharger : la partie charge, la voie est là, le non électrique est revenu dans Train, les diesels sont disponibles.
8. **Deux parties d'affilée** dans la même session : l'onglet n'est ni dupliqué ni vide, et les éléments ne sont pas en double.

## Hors périmètre

- Les améliorations NE des yards et des gares (`m_Group: null`), qui s'affichent dans le panneau d'amélioration, pas dans la barre d'outils.
- Les trains de transit des connexions extérieures (énergie codée en dur, voir ci-dessus).
- Un vrai filtre dans le sélecteur de modèle de la ligne (injection UI) ; trams et métro.
- Le décor des caténaires : un train électrique peut rouler sur une voie Plain, parce que la caténaire n'est qu'un décor (`UtilityTypes.Catenary`).

## Écarts après audit du 05/10

- Restauration de l'énergie relue sur les prefabs (`TransportDepot.m_EnergyTypes`, `TrainPrefab.m_EnergyType`), plus de valeurs mémorisées ; rafraîchi seulement si l'option change. Onglet : seuls les assets ferroviaires (voie `TrackData` Train ou bâtiment avec arrêt de train) votent et sont déplacés.

## Écart après recette du 05/10 (soir)

L'onglet « Rail non électrique » est **abandonné** : décision de l'humain en recette, le rail diesel
est entièrement **masqué** de la barre d'outils (`Rail/HideNonElectricRailSystem.cs`). Les assets
déjà posés continuent de fonctionner. Piège : `UIObject.LateInitialize` remet un asset dans son
groupe à chaque réinitialisation du prefab, il faut aussi vider `UIObject.m_Group` (managé).
L'onglet Train porte une icône train + éclair composée au lancement depuis les icônes du jeu
(`Rail/RailTabIcons.cs`). `RailLocale.cs` et `NonElectricRailCategorySystem.cs` sont supprimés.
