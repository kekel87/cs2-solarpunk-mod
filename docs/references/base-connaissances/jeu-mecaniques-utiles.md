# Mécaniques du jeu établies et consommées par le mod

> 05/10/2026, jeu 1.6.2f1. Abréviations : `README.md` (`DEC` = décompilé `src/Game`, `MECA` =
> références mécaniques du plugin). [V] vérifié dans le décompilé (date) · [S] supposé.
> Résumé seulement : le détail est dans le plan cité et dans le fichier du plugin.

## Fret, vendeurs, Local Hub

- Un commerce qui manque de marchandise reçoit `ResourceBuyer` ; `ResourceBuyerSystem` (GameSimulation,
  intervalle 16, `ResourceBuyerSystem.cs:1179-1181`, `SystemOrder.cs:395`) cherche un vendeur par
  chemin [V 05/10].
- **Un vendeur doit être un locataire** : à l'arrivée du chemin, `ProcessResourceBuyer` n'accepte la
  destination que si elle porte `PropertyRenter` ou est une connexion extérieure
  (`ResourceBuyerSystem.cs:587-589`) ; sinon destination périmée (`:692-698`). **Une gare cargo n'est
  jamais vendeuse** d'un commerce [V 05/10]. D'où `HubSaleSystem` qui fait la vente lui-même, ancré
  juste avant `ResourceBuyerSystem` (plan 007, décision-016).
- Vente par un stockage, côté argent (`BuyJob`, `ResourceBuyerSystem.cs:195-260`) : prix industriel
  + coût commercial, mise à jour des `TradeCost` des deux côtés, `BuyingCompany.m_LastTradePartner`,
  `CompanyStatisticData` ; la marchandise d'un `StorageCompany` ne sort qu'au chargement du véhicule
  [V 05/10]. Reproduit dans `HubSaleSystem.Sell`.
- Un vendeur doit pouvoir servir **au moins la moitié** de la demande (`TryPickHub`, d'après
  `ProcessResourceBuyer`) [V plan 007].
- Données de stockage d'un bâtiment = `StorageCompanyData` / `StorageLimitData` **du prefab
  principal**, puis `UpgradeUtils.CombineStats` des extensions (`StorageCompanySystem.cs:264-269`) :
  un bâtiment sans ces données sur son propre prefab plante l'indexation → on les copie sur les
  prefabs hôtes (`StationPlatformSystem.GiveCargoPrefabData`) [V 05/10].
- Petits véhicules : le véhicule de livraison vanilla de plus petite capacité par ressource,
  créé comme `TripNeededSystem.SpawnDeliveryTruck` avec un habitant au guidon (`Hub/HubDeliveryVehicles.cs`) [V plan 006/007].
- Véhicules du hub (lot C, codé non recetté) : nos 4 modèles importés (électriques, 20/18/45/60 km/h,
  capacités 500/1000/2000/4000), verrouillés pour le hub, repli sur la moto vanilla
  (`Hub/HubVehicleModels.cs`) ; le prefab se retrouve par nom, pas par `PrefabID` (voir `assets-3d.md`).
- Plugin : `MECA/economy-and-companies/trade-and-restocking.md`, `economy-and-companies.md`.

## Dépôts, énergie, sélection des véhicules

- Un dépôt crée ses véhicules avec `TransportDepotData.m_EnergyTypes` (`TransportDepotAISystem.cs:648`),
  combiné aux améliorations par OU (`CombineStats`, `:247`) [V plan 003].
- `TransportVehicleSelectData.ListVehicles/SelectVehicle` : un modèle dont l'énergie n'est pas dans
  celles du dépôt est **écarté**, sauf énergie `None` qui passe partout
  (`TransportVehicleSelectData.cs:396`) ; puis **score** : +2 si c'est le modèle choisi sur la ligne,
  **+1 si `EnergyTypes.Fuel`** (voitures `:405`, trains `:460`, bateaux `:587`) ; seul le meilleur
  score est tiré au sort → **le jeu préfère le diesel** [V 05/10].
- Le filtre d'énergie s'applique aussi aux wagons ; un wagon `Fuel` seul bloque un usage entier
  derrière un dépôt électrique (wagon-poubelle de Tigon) → `ElectricTrainsOnlySystem` met leurs
  wagons à `None` [V plan 003].
- Le sélecteur de modèle d'une ligne fait l'union des énergies des dépôts de la ville
  (`SelectVehiclesSection.cs:55-72`) [V plan 003].
- Trafic de transit des connexions extérieures : énergie en dur `FuelAndElectricity`
  (`TrafficSpawnerAISystem.cs:191`, job Burst) : hors de portée par les données [V plan 003].
- Passager ou cargo = trois interrupteurs (accès, `m_PassengerTransport`/`m_CargoTransport` sur
  ligne **et** arrêt, composant du véhicule) ; aucun mod n'ajoute de `TransportType` [R
  `docs/references/bonnes-pratiques-modding.md`].
- Plugin : `MECA/transportation-and-vehicles/depots-and-dispatch.md`, `lines-and-fleet.md`, `vehicles.md`.

## Barre d'outils et déblocage

- `UIObject.LateInitialize` ajoute le prefab au buffer `UIGroupElement` de son groupe par
  `UIGroupPrefab.AddElement`, qui ajoute **aussi** une `UnlockRequirement(RequireAny)` au groupe
  (`UIGroupPrefab.cs:18-23`), et renseigne `UIObjectData.m_Group` (`UIObject.cs:67-83`) [V 05/10].
- Un groupe naît `Locked` (`UIGroupPrefab.cs:15`) ; `UnlockSystem` le débloque dès qu'un élément
  l'est (RequireAny) [V 05/10 ; S pour le comportement en jeu, non recetté].
- Un `UIAssetCategoryPrefab` avec `m_Menu` s'insère seul dans le menu (`UIAssetCategoryPrefab.cs:30-39`) ;
  nom affiché = clé `SubServices.NAME[<nom du prefab>]` (`PrefabUISystem.cs:1509`) [V plan 003].
- Déplacer un asset d'onglet = retirer de `UIGroupElement` côté source, `AddElement` côté cible,
  mettre `UIObjectData.m_Group` (`Rail/NonElectricRailCategorySystem.MoveNonElectricAssets`).
- Un prefab verrouillé s'affiche grisé (`ToolbarUISystem.cs:380`) ; `Locked` est **sauvegardé** →
  ne pas verrouiller du vanilla [V plan 003].
- Plugin : `MECA/city-state-and-progression/unlocking.md`, `TECH/custom-tools/toolbar-position.md`.

## Améliorations de service (`ServiceUpgrade`)

- `ServiceUpgrade.m_Buildings` = bâtiments hôtes acceptés (`ServiceUpgrade.cs:17`), autorisé sur
  `BuildingPrefab` (`:8-14`) ; à la pose, `ServiceUpgradeSystem` ajoute à l'**hôte** les composants
  déclarés par `IServiceUpgrade.GetUpgradeComponents` (`ServiceUpgradeSystem.cs:146-153`) [V plan 008].
- `CargoTransportStation` (`CargoTransportStation.cs:74-86`) et `GarbageFacility` (`GarbageFacility.cs:18`)
  sont des `IServiceUpgrade` ; en extension, la station cargo ne crée pas son stockage propre mais
  greffe `StorageCompany`, `Resources`, `TradeCost`… sur l'hôte [V plan 008/009].
- **Les systèmes lisent les données sur le prefab de l'hôte, puis additionnent les extensions**
  (stock : `StorageCompanySystem.cs:264-269` ; déchets : `GarbageFacilityAISystem.cs:227-234`) →
  l'hôte doit porter un `*Data` (même vide) [V 05/10].
- Un arrêt posé par une extension charge/décharge dans le premier `StorageCompany` en remontant les
  propriétaires (`TransportTrainAISystem.cs:1199-1215`) [V plan 008/009].
- Un arrêt = un seul type d'accès (`TransportStop.cs:40-42`, `WaypointConnectionSystem.cs:524-549`)
  → un quai = un arrêt d'un type [V plan 008].
- Modifier `WorkplaceData` & co ne touche pas les bâtiments posés : `Created` sur un bâtiment, ou
  `Created`/`Deleted` sur une extension, relance la copie [V `TECH/prefabs-and-assets/prefabs-and-assets.md` § *Editing a prefab*].
- Plugin : `TECH/prefabs-and-assets/prefabs-and-assets.md` (§ archetype hooks, point 3),
  `MECA/city-services-and-coverage/budget-workforce-and-upkeep.md`.

## Vélo-cargo hybride (recherche 05/10, plan 007 lot D)

- Un vélo = véhicule `Car` + composant `Bicycle` (`BicyclePrefab.cs:26-58`) [V 05/10].
- Voies : `CarLaneData.m_RoadTypes` → `PathMethod` (`VehicleUtils.cs:2327-2345`) ; `ForbidBicycles`
  retire `Bicycle` ; une voie piétonne `AllowBicycle` = arête vélo à 20 km/h
  (`LanesModifiedSystem.cs:321-325`) [V].
- Trottoirs de route **non cyclables** (`LaneSystem.cs:4348`) [V]. Rues piétonnes : pas de règle
  « véhicules autorisés », tout dépend des données du prefab [S].
- `CarNavigationSystem` lit `Bicycle` sans filtre de type (`CarNavigationSystem.cs:384`) [V].
- `DeliveryTruckAISystem` code en dur `Road | CargoLoading` (`:1104-1132`) ; job Burst, non
  patchable [V].
- Un nouveau chemin n'est demandé que si `Obsolete` sans `Pending` (`VehicleUtils.cs:199-206`) →
  un système exécuté avant `DeliveryTruckAISystem` peut poser un chemin vélo [V].
- Le chemin de retour est demandé dans le même tick (non interceptable) → **flux inversé** : aller
  simple hub → commerce, suppression du véhicule après livraison [V].
- Piège : cloner `BicyclePrefab` + `DeliveryTruck` plante (`PathInformation` absent,
  `DeliveryTruck.cs:33-37`) [V].
- `DeliveryTruckAISystem` tourne à chaque frame sur 1/16 des camions (filtre `UpdateFrame`) ; un
  système qui précède doit appliquer le même filtre [V 05/10].
- `Bicycle` est `IEmptySerializable` : sauvegardé avec le véhicule [V 05/10].
- Le seuil de 2 000 de `BuyingCompanySystem.cs:330` est figé : pour que les commerces « commandent
  moins mais plus souvent », le mod ne vend qu'une partie de la commande (decision-028).

## Déchets

- `Garbage` est une `Resource` comme les autres ; stock déchets et stock cargo = **même buffer**
  `Game.Economy.Resources` (`GarbageFacilityAISystem.cs:239-263`) [V plan 009].
- `GarbageTransferDispatchSystem` cherche en `PathMethod.Road | CargoLoading` (`:270, :276, :286`) et
  n'accepte que des destinations `GarbageFacility` (`:206`) → une installation de déchets
  embranchée devient atteignable par train [V plan 009]. Les trains cargo vanilla seuls ne livrent
  pas des déchets à une gare sans `GarbageFacility` (question ouverte du 03/10 tranchée par ce fait).
- Modèle Tigon : un bâtiment = `CargoTransportStation` (Garbage) + `GarbageFacility` ; wagon
  `TO_TrainTrashCar01` (Garbage, **Fuel**) [V plan 009].
- Embranchement ferroviaire (plan 009 lot A, codé non recetté) : amélioration à 50 000 sur
  incinérateurs / décharges / déchets industriels (`Waste/RailSidingSystem.cs`), code commun avec la
  gare mixte (`Station/CargoUpgradeCloner.cs`). Le **recyclage est absent du code du jeu** → lot C.
- Indicateur « Déchets en train » (plan 009 lot B, codé non recetté) : cargaison lue par wagon
  (`TransportTrainAISystem.cs:975`), panneau ville seulement, pas au district
  (`Info/SolarpunkInfoUISystem.UpdateCargoTrains`).
- Plugin : `MECA/city-services-and-coverage/city-services-and-coverage.md`, `dispatch.md`.
  Ancienne note : `docs/references/dechets-par-rail-code-du-jeu.md` (16/09, pré-1.6).

## Politiques de district

- Politique par code : `PolicyTogglePrefab` + `UIObject` (icône) + `DistrictModifiers` ; créée en
  `OnGamePreload` (`Policies/EndResidentialParkingPolicySystem.cs`). Textes : `Policy.TITLE[<nom>]`,
  `Policy.DESCRIPTION[<nom>]` [V dépôt].
- État : buffer `Policy` sur le district (`m_Policy`, `m_Flags & PolicyFlags.Active`).
- Bascule : entité événement `Event` + `Modify` (`Game.Policies/Modify.cs` : `m_Entity`, `m_Policy`,
  `m_Flags`), créée par `PoliciesUISystem.cs:241` (et par le menu debug `DebugSystem.cs:3489`),
  consommée par `ModifiedSystem` en `Modification4` (`SystemOrder.cs:146`) ; **vit une frame** →
  la lire dans la même phase (`ParkingBanToggleSystem`, `ModificationBarrier4`) [V plan 005, 05/10].
- Un modificateur à effet nul (`ParkingFee` +0) sert à faire rafraîchir les voies de stationnement
  par `LanePoliciesSystem` à la bascule (commentaire dans `EndResidentialParkingPolicySystem`) [V dépôt].
- Plugin : `MECA/city-state-and-progression/policies.md` (quatre portées, composition des modificateurs).

## District d'une entité

- Bâtiment : `CurrentDistrict.m_District` (`Game.Areas/CurrentDistrict.cs:8`).
- Route : `BorderDistrict.m_Left` / `m_Right` (`BorderDistrict.cs:8-10`), porté par toute route
  (`RoadPrefab.cs:64`), maintenu par `CurrentDistrictSystem.cs:206-234` ; une route frontière compte
  pour ses deux districts [V plan 004].
- Voie / véhicule (révisé 05/10) : test d'appartenance = position du milieu de voie dans les
  triangles de la zone du district (`CurrentDistrictSystem.DistrictIterator`,
  `AreaUtils.GetTriangle2`) ; carrefours, chemins, pistes et voies ferrées comptés, une route de
  bordure ne compte plus double (`Info/DistrictLocator.cs`). Remplace la remontée des `Owner`.
- District sélectionné dans le panneau : entité qui porte `District` **et** `Area`
  (`SolarpunkDistrictSection.OnUpdate`, modèle `AverageHappinessSection.cs:546`) [V plan 004].
- Plugin : `MECA/zoning-buildings-and-land-value/districts-and-themes.md`,
  `MECA/city-services-and-coverage/coverage.md` (§ district).

## Fermeture du jeu

- `GameManager.cs:805-806` : les mods sont libérés (`OnDispose`) avant la destruction du monde ECS →
  tout accès à `m_Settings` ou au monde au dispose doit être gardé (NRE corrigé dans
  `ElectricTrainsOnlySystem`) [V 05/10].

## Écoles

- La portée affichée d'une école est une couverture de service le long des routes (`ServiceCoverage`
  `m_Range` / capacité), utilisée pour le bonheur (`CitizenHappinessSystem.GetEducationBonuses`) [V 05/10].
- Le choix réel de l'école = recherche d'itinéraire `FindSchoolSystem` : piéton + TC de jour +
  voiture si le ménage en a, coût max = priorité école × 1,1, places libres, district du logement.
  Les élèves vont donc à une école hors portée si un trajet est possible [V 05/10].

## Temps

- 1 jour de jeu = 262 144 frames (`TimeSystem.kTicksPerDay`, `TimeSystem.cs:18`) et c'est aussi un
  mois [V 05/10] → un préavis réaliste en jours dépasse la construction d'un quartier (choix des 6 h,
  `ImpoundSystem`). Plugin : `MECA/simulation-time-and-units/cadence.md`, `calendar.md`.
