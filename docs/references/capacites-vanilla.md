# Ce que le jeu simule déjà (vanilla 1.6.2f1)

> Synthèse du 03/10/2026 depuis la base du plugin `cs2-modding` 1.2.0
> (`references/mechanics/…`, marquée VOLATILE). **Pas encore vérifié dans notre décompilé** (absent
> de la machine). « [à vérifier] » = déduction non couverte par la base.

## 1. Fret

- **Achat** : `BuyingCompanySystem` (stock < `max(4000, 25 %)`) → `ResourceBuyer` →
  `ResourceBuyerSystem` cherche le vendeur par pathfinding, profil fret `(0.01, 0.01, transportCost, 0.01)` :
  seul l'**argent** compte → `DeliveryTruckAISystem` (`trade-and-restocking.md`, `travel-weights.md`).
- **`StorageCompany` = pivot** : entrepôts, connexions extérieures, terminaux `CargoTransportStation`.
  Un terminal = « entrepôt avec un arrêt dessus ».
- **Charger un train cargo = transfert de stock** (`TransportBoardingHelpers.LoadResources`).
- **Route ou rail** : pas de choix de mode, le chemin le moins cher (`CargoLoading`/`CargoTransport`
  ouvrent les arêtes de lignes cargo ; ligne = `m_TravelCost × distance`, arrêt = départ + attente).
- **Dernier km toujours en camion** : l'acheteur n'a pas d'arrêt.
- Connexions extérieures : `TradeSystem`, `OutsideTradeParameterData` (coefficients Road/Train/Air/Ship).
- Transit camion `DummyTraffic` : décoratif.

**Leviers** :
- *Facile* : baisser les coûts `PathfindTransportData` des lignes cargo et les coefficients Train de
  `OutsideTradeParameterData` → le rail gagne, sans code de sim.
- *Moyen* — Local Hub : `SaleFlags.Virtual` (vente livrée **sans véhicule**, directement en stock) ;
  ou intercepter `ResourceBuyer` avant `ResourceBuyerSystem` pour imposer le Hub comme vendeur
  (logique de Dedicated Cargo Facilities).
- *Moyen* — vélo-cargo : `DeliveryTruckSelectData.TrySelectItem` choisit le véhicule de livraison →
  y glisser un prefab vélo pour les petites charges d'un Hub.
- Wagons spécialisés : `CargoTransportVehicleData.m_Resources`, `m_MaxResourceCount`.

## 2. Transport

| Type | Lignes vanilla |
|---|---|
| Bus, Tram, Subway, Ferry | passagers |
| Train, Ship, Airplane | passagers + cargo |

Passager/cargo = 3 interrupteurs (connexion d'accès, paire `m_PassengerTransport`/`m_CargoTransport`
sur `TransportLineData` **et** `TransportStopData`, `PublicTransportVehicleData` /
`CargoTransportVehicleData`). Les dépôts gèrent déjà Tram et Subway ; `TransportVehicleSelectData`
choisit le modèle (sans prefab cargo → ligne vide).

- **Cargo tram / métro (moyen-dur)** : cloner ligne, arrêt, véhicule en mode cargo + terminal sur voie
  tram. Pièges : `TransportUsageData` ne compte pas le tram ; `PublicTransportFlags` ≠
  `CargoTransportFlags` ; switch Burst.
- **Raccourci (facile-moyen)** : ligne **Cargo Train** sur voies mixtes `Train|Tram` en voirie
  (clone « Double Tram Track » d'AdvancedTransitOperations) = CarGoTram visuel sans nouveau type.
- `VehicleSideEffectData` : bruit/pollution par prefab — l'électrique n'est pas plus propre par règle.

## 3. Déchets

Collecte : `GarbageProducer` → dispatch → `GarbageFacility` (pénalité d'efficacité si non collecté).
Modificateur de district sur la production. Transferts entre installations via
`GarbageTransferDispatchSystem` (`Road | CargoLoading`). Export existant via `TradeSystem`.
Détail code : `dechets-par-rail-code-du-jeu.md`. *Facile-moyen*, surtout du prefab.

## 4. Voiture

- **Vélo** : `TripNeededSystem` tire 20 % pour un `BicycleOwner` (× modificateur district
  `BikeProbability`) ; **une voiture réservée l'emporte**. Sinon le pathfinder compare marche,
  transit, voiture + parking, taxi, vélo.
- **Poids citoyen** (`CitizenUtils.GetPathfindWeights`) : temps 1,25-20, comportement 2, argent
  `2500 × taille ménage / consommation`, confort 1-3. Plafond `kMaxPathfindCost = 17000`.
- **Parking** : parking plein = refusé ; tarif bâtiment > district (`PaidParking`, `ParkingFee`).
- **5 politiques de district** : `PaidParking`, `ForbidCombustionEngines`, `ForbidTransitTraffic`,
  `ForbidHeavyTraffic`, `ForbidBicycles`. ⚠️ **Surcoûts, pas des interdictions** ; poids
  comportement du fret = 0,01 → **l'interdiction poids lourds vanilla est quasi sans effet sur la
  livraison** [à vérifier en jeu]. Vraie interdiction = par voie (`PublicOnly`), ce qu'exploite Road Rules.

**Leviers** : ZTL par données (*facile*) ; secteur vraiment sans voiture via restrictions de voies posées
par notre système ou Road Rules (*moyen-dur*) ; tirage vélo / poids (*moyen*).

## 5. Climat

- `ClimateSystem` : température, précipitations, nuages, brouillard — **global**, pas par cellule ;
  forçable (`overrideValue`). Effets : conso électrique, solaire, incendie, loisirs, tourisme.
  **Aucun effet de la chaleur sur la santé.** `GetHeatingMultiplier` alimente un champ que rien ne lit.
- Eau de surface simulée sur GPU (pilotable par `WaterSourceData`) ; nappe = carte de cellules.
- **Inondation par la pluie = code mort** : `SoilWaterSystem` (compteur pluie → `Flood`) n'est
  enregistré dans aucune phase. Le relancer (*moyen-dur*) = **meilleure porte d'entrée eaux pluviales**.
- **`CellMapSystem<T>`** générique (8 couches : pollution sol/air/bruit, nappe, ressources, eau du sol,
  attractivité, vent), sérialisation fournie → **carte « chaleur » faisable (*moyen*)** ; décider du
  retrait du mod (sérialisée).
- Arbres : seulement via l'ambiance « Forest » → attractivité → valeur foncière/tourisme. La pollution
  rend les plantes malades ; **les arbres n'absorbent rien** (rôle d'Árvore).

## 6. Politiques

Buffer `Policy` sur ville/district/bâtiment/ligne ; portée déduite du prefab (`DistrictModifiers`,
`DistrictOptions`, `RouteOptions`…). Une option s'applique à une route seulement si **tous** ses
districts l'ont.
- Nouvelle politique combinant l'existant : *facile* (`PolicyTogglePrefab`/`PolicySliderPrefab` + locale).
- Nouvel effet : *moyen* — **ne pas** étendre les enums (buffers positionnels, sauvegarde) ; politique
  « marqueur » lue par **notre** système.

## 7. Bonheur, valeur foncière

- Bonheur = (bien-être + santé)/2 ; bruit → bien-être ; pollution → santé ; vélo +1 santé ; modificateur
  district « Wellbeing » ; pénalité trafic sur le bien-être.
- **Valeur foncière par segment** : services, **disponibilité bus et tram/métro** (le tram est déjà
  récompensé, pas la voiture) ; la carte ajoute attractivité, forêt, rive, pollution.
- `CityEffects` (bâtiments signature), `PollutionEmitModifier`, déblocages par usage des transports
  (`TransportRequirementData`) → progression solarpunk (débloquer le Hub après X t de fret rail).

## Top 5 leviers

1. Machinerie cargo (3 interrupteurs + `CargoTransportStation` = `StorageCompany`).
2. `SaleFlags.Virtual` + interception `ResourceBuyer` + `DeliveryTruckSelectData` (dernier km sans camion).
3. Coûts de pathfinding du fret (donnée pure).
4. Politiques en données + politiques « marqueur » (les `Forbid*` vanilla ne sont que des surcoûts).
5. `CellMapSystem<T>`, `ClimateSystem.temperature`, `SoilWaterSystem` dormant.

## Ce que le jeu ne simule pas

Température locale et effet de la chaleur sur la santé · ruissellement, imperméabilisation
(inondation par la pluie = code mort) · absorption par les arbres, pluie qui lave l'air · CO₂,
carburant · vraie interdiction de circuler par district · tri/recyclage/compost (déchets = une seule
ressource) · dernier km hors camion, cargo léger · part modale (aucun mod ne l’affiche, InfoLoom non plus).
