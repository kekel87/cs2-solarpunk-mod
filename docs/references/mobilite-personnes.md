# Mobilité des personnes — bimode, porte-voitures, automatique

> Recherche du 03/10/2026 sur les trois idées de l'humain. Moteur : base `cs2-modding` 1.2.0
> (VOLATILE), pas de décompilé sur la machine. **[V]** base · **[S]** supposé · **[W]** web.

## 1. Voitures bimodes rail-route / capsules automatiques (PRT)

| Cas | Chiffres | Leçon |
|---|---|---|
| [DMV Asa Seaside Railway](https://www.euronews.com/next/2021/12/27/a-new-bus-which-can-turn-itself-into-a-train-has-just-started-operating-in-japan) (Japon) | Depuis 12/2021, 21 places, 60 km/h sur rail | Seul bimode en service ; expédient rural |
| [RUF](https://faculty.washington.edu/jbs/itrans/big/rufsummary.pdf) (Danemark) | Voiture 2-4 places qui s'accroche en convoi sur un monorail | **Exactement l'idée, jamais construit** |
| [Morgantown PRT](https://en.wikipedia.org/wiki/Morgantown_Personal_Rapid_Transit) | Depuis 1975, ≈ 12 000 voyageurs/j, 98,7 % de dispo | Marche sur un campus captif |
| [Heathrow ULTra](https://mediacentre.heathrow.com/pressrelease/detail/5605) | 21 capsules de 4 places, 3,8 km, ≈ 500 000 voyageurs/an | **Cas d'usage réel = parking-relais** |
| [Masdar PRT](https://www.thenationalnews.com/uae/environment/masdar-city-s-prt-a-test-drive-for-future-of-transport-1.377189) | Réduit à 2 stations | Infra séparée trop chère |
| [Suncheon SkyCube](https://www.advancedtransit.org/advanced-transit/applications/suncheon/) | Recettes 1,3 Md KRW / coûts 4 Md KRW | Échec économique |
| [Glydways](https://www.glydways.com/atlanta/) | Pilote Atlanta 800 m, ouverture visée 12/2026 | Pas encore prouvé |

**CS2** :
- [V] Aucun véhicule ne change de réseau : `CarData` / `TrainData` exclusifs ; navigation, IA
  (`TransportCarAISystem` / `TransportTrainAISystem`) et `PathMethod` (`Road` / `Track`) séparés ;
  `CarLane` ≠ `TrackLane`.
- [V] Ligne = anneau fixe d'arrêts ; seul service à la demande = taxi, route seule ; dispatch dépôt →
  ligne = `switch` en dur sur `TransportType`.
- [V] Un même itinéraire peut enchaîner voiture, parking, marche, transport en commun (`Parking`,
  `Pedestrian`, `PublicTransportDay`). [S] Qu'un citoyen fasse vraiment « je me gare puis je prends le
  métro » : **à vérifier en jeu**.
- Vrai bimode : **très dur** (détruire la voiture, créer une rame en gardant les passagers, itinéraire
  route+rail que le pathfinder ne sait pas faire, risque sauvegarde).

**POC** : **navette-capsule fausse-bimode** à la Heathrow — véhicule métro/tram cloné, 1 voiture de
4-10 places, intervalle très court, ligne parking-relais en lisière → centre, stationnement cher au
centre. Surtout du prefab, *facile-moyen*. Mesh vanilla réutilisé ou modèle libre.

## 2. Trains porte-voitures

| Cas | Statut |
|---|---|
| [DB AutoZug](https://en.wikipedia.org/wiki/DB_AutoZug) | Fermé 10/2016 (repris en partie par Nightjet) |
| [Eurotunnel Le Shuttle](https://www.getlinkgroup.com/en/our-group/eurotunnel/) | ≈ 2,2 M voitures + 1,2 M camions (2024) : franchit un obstacle infranchissable |
| [RAlpin Lötschberg](https://www.railwaygazette.com/freight/2025/05/08/trans-alpine-rolling-highway-lorry-carrying-service-to-end-this-year/) | ≈ 72 000 camions (2024), pertes ; arrêt 13/12/2025 |
| [Lorry-Rail / VIIA](https://en.wikipedia.org/wiki/Lorry-Rail_S.A.) | Semi-remorques non accompagnées, 1 050 km, depuis 2007, actif |

Leçon : marche **là où la route ne passe pas** ; meurt face à l'autoroute. Le non-accompagné survit mieux.

**CS2** : [V] aucun véhicule ne peut en porter un autre (`Passenger` = citoyens, fret = `Resources`) ;
transit entre bords de carte décoratif (`DummyTraffic`). Littéral = **dur** et sans effet de sim.

**Traduction utile** : le **parking-relais en gare d'entrée de ville** (= POC de l'idée 1), habillé
au besoin d'un train décoratif chargé de voitures. Version fret : terminal « autoroute ferroviaire »
à la connexion extérieure via les coefficients de `OutsideTradeParameterData`.

## 3. Trains et métros automatiques (GoA4)

| Cas | Chiffres |
|---|---|
| Paris ligne 14 | Automatique depuis 1998, 85 s en pointe ; lignes 1 (2012) et 4 (2022-23) automatisées sans couper le service |
| [Copenhague](https://en.wikipedia.org/wiki/Copenhagen_Metro) | **24 h/24**, 2 min en pointe |
| [Rio Tinto AutoHaul](https://www.railjournal.com/freight/rio-tinto-operates-first-driverless-freight-train/) | Train de fret ≈ 28 000 t sans conducteur, 280 km, opérationnel 2019 |
| [UITP](https://www.uitp.org/publications/the-benefits-of-full-metro-automation/) | 40-60 % d'économie de personnel GoA4 vs GoA2 [W, à vérifier] |

**CS2** : [V] pas de conducteur ; personnel sur les bâtiments (`WorkplaceData`) ; upkeep de dépôt
**fixe**, indépendante du nombre de véhicules ; [S] aucun coût par véhicule trouvé. Leviers :
`CalculateVehicleCount`, options `Day`/`Night`, politique `VehicleInterval`, `m_StopDuration`,
`m_UnbunchingFactor`.

**POC** : amélioration de dépôt « Automatisation GoA4 » — moins de travailleurs, intervalle et arrêts
plus courts, service de nuit ; débloquée par l'usage du rail (`TransportRequirementData`). *Facile*
mais **peu visible** → amélioration transversale plutôt que module.

## Recommandation de la recherche

- **Meilleur POC : parking-relais + navette-capsule automatique** (absorbe l'idée 2).
- À écarter : porte-voitures littéral, vrai bimode (très dur ; RUF jamais construit, AutoZug et RAlpin fermés).
- **Synergies** : capsule et cargo tram partagent la technique (cloner un véhicule tram/métro) ; le
  parking-relais est le jumeau voyageurs du Local Hub → un prefab « **porte de ville** » (gare + hub +
  parking-relais, vélo-cargo en aval) ; GoA4 applicable au train-poubelle (« AutoHaul des déchets »).
