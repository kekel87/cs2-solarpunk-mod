---
status: in-progress
created: 2026-10-05
updated: 2026-10-05
---

# Plan 008 — Gare mixte à quais dédiés (voyageurs / fret / déchets)

## Ce que tu verras en jeu (V1)

- Sur une gare voyageurs du jeu de base, le panneau des améliorations propose « **Quai fret** » : une
  petite gare de marchandises collée à la gare, avec sa propre voie et son propre quai.
- Une fois posée, la gare accueille **une ligne voyageurs ET une ligne de marchandises**, chacune sur
  son quai. Le panneau de la gare montre le confort voyageurs et les ressources échangées.
- Le fret arrive et repart par train ; les camions viennent charger et décharger au quai fret.
- Retirer le mod : la gare voyageurs reste intacte et fonctionne ; seul le quai fret devient une
  boîte « objet manquant » à démolir (à confirmer en recette, scénario 3).

## Contexte

- Idée de l'humain du 05/10/2026 : `design.md`, module 10 (stations mixtes, quais dédiés voyageurs /
  fret / déchets, plusieurs tailles, surface / souterrain, puis tram et métro).
- Faisabilité vérifiée le même jour (rapport en conversation, résumé dans `design.md`) : faisable par
  clone de prefab, sans système de simulation maison.
- Ce plan se code **après** le Local Hub (plan 007), dont il reprend le savoir-faire « prefab créé par
  code au pré-chargement ».

## Faits vérifiés dans le décompilé (1.6.2f1)

**Mixte possible dans un même bâtiment**
- `TransportStation` et `CargoTransportStation` partagent `TransportStationData` et
  `Game.Buildings.TransportStation` (HashSet, sans doublon) : `TransportStation.cs:26-46`,
  `CargoTransportStation.cs:39-72`. Le code vanilla combine déjà les deux drapeaux
  (`BuildingInitializeSystem.cs:477-497`).

**Le fret comme amélioration (modèle « Airport01 Cargo Terminal »)**
- `CargoTransportStation` est un `IServiceUpgrade` : quand son prefab porte un `ServiceUpgrade`, il
  ne crée pas son propre stockage (`CargoTransportStation.cs:51-64`, `GetComponent<ServiceUpgrade>() == null`),
  mais `GetUpgradeComponents` greffe `StorageCompany`, `Resources`, `TradeCost`,
  `StorageTransferRequest`, `TransportCompany` sur **le bâtiment principal** (`:74-87`).
- `ServiceUpgradeSystem` ajoute ces composants au propriétaire à la pose (`ServiceUpgradeSystem.cs:146-153`)
  et les retire quand l'amélioration est supprimée (`:174-228`).
- `ServiceUpgrade.m_Buildings` liste les bâtiments qui acceptent l'amélioration (`ServiceUpgrade.cs:17`).
- `BuildingExtensionPrefab.m_ExternalLot` : extension posée sur son propre lot, à côté du bâtiment
  (`BuildingExtensionPrefab.cs:14`, lu en `BuildingInitializeSystem.cs:927`).
- Les ressources stockées des améliorations s'additionnent par OU (`StorageCompanyData.Combine`,
  `StorageCompanyData.cs:33-36`) ; `CargoTransportStation.m_TradedResources` les définit (`:21`, `:96-100`).

**Quais et arrêts**
- Un arrêt (`TransportStop`) porte `m_PassengerTransport` et `m_CargoTransport` (`TransportStop.cs:40-42`)
  mais **un seul** `m_AccessConnectionType` (`:16`) : piéton pour les voyageurs, cargo pour le fret
  (`WaypointConnectionSystem.cs:524-549`). Donc **un quai = un arrêt d'un seul type**.
- Les arrêts sont des sous-objets du bâtiment (`ObjectSubObjects.m_SubObjects`, `ObjectSubObjectInfo` :
  `m_Object`, `m_Position`, `m_Rotation` relatifs au bâtiment, `ObjectSubObjectInfo.cs:12-22`) ; les voies
  des sous-réseaux (`ObjectSubNetInfo.m_NetPrefab`, `m_BezierCurve` relative, `ObjectSubNetInfo.cs:31-39`) ;
  les accès des sous-voies (`ObjectSubLaneInfo.m_LanePrefab`, `m_BezierCurve`, `ObjectSubLaneInfo.cs:12-18`).
  Ce sont de simples tableaux sur les composants du prefab : on les **copie depuis un prefab vanilla**
  plutôt que de les inventer.
- Un train de fret charge dans le stock du premier propriétaire qui a `StorageCompany`, en remontant
  `Owner` depuis l'arrêt (`TransportTrainAISystem.cs:1199-1214`). Un arrêt de l'extension remonte donc
  au stock greffé sur la gare principale.

**Quai déchets**
- Il n'existe pas de filtre « déchets » par arrêt : le stock est par bâtiment. Le yard déchets de Tigon
  (`MediumTrainTrashYard.Prefab`) est un `CargoTransportStation` avec `m_TradedResources = [40]`
  (Garbage) **plus** un `GarbageFacility`. Un quai dédié aux déchets = un quai fret sur lequel seule une
  ligne déchets s'arrête (ce sont les wagons qui choisissent la ressource), avec Garbage dans les
  ressources échangées.

**Souterrain**
- Une gare souterraine = sous-réseaux de voie à y = −20 (`TO_UndergroundRailStation01`, prefab Tigon).
  Pas de composant de bâtiment dédié.

## Décisions

1. **V1 = extension « Quai fret » sur les gares voyageurs vanilla**, pas une gare mixte clonée entière.
   Raison : la sauvegarde. Retirer le mod laisse la gare voyageurs intacte ; une gare mixte clonée
   entière deviendrait tout entière un « objet manquant » (même mécanisme que le plan 007,
   `ResolvePrefabsSystem.cs:535-545`), et l'astuce de l'option 2 du 007 ne s'applique pas : aucune gare
   vanilla n'est mixte, il n'y a rien vers quoi rabattre la référence.
2. **Pas de dépendance Tigon** : on copie la géométrie (voie, arrêt cargo, accès) d'une gare de
   marchandises **vanilla**, trouvée par les données (prefab avec `CargoTransportStation` et voie de
   type Train, hors mods), pas par son nom.
3. **Ressources échangées** du quai fret = celles de la gare de marchandises vanilla copiée.
4. Les gares acceptées (`ServiceUpgrade.m_Buildings`) = toutes les gares voyageurs ferroviaires
   vanilla, trouvées par les données (`TransportStation` + arrêt voyageurs Train).
5. **Décisions de l'humain (05/10/2026)** : V1 validée sous forme d'extension, modèle 3D repris d'une
   gare de marchandises du jeu. **Penser aux déchets et aux autres usages de Tigon** (yards déchets,
   charbon, pétrole, agricole, bois…) : V2 généralise l'extension en quais dédiés par usage, et les
   gares voyageurs de Tigon deviennent des cibles si Tigon est présent (détection au runtime, pas de
   dépendance obligatoire). **À garder en mémoire** : la gare mixte autonome à quais dédiés reste
   l'objectif à terme, quand la sauvegarde le permettra.

## Lots

- **V1** — Extension « Quai fret » (surface) pour les gares voyageurs vanilla : voyageurs + fret.
- **V2** — Quais dédiés par usage : quai déchets (Garbage dans les ressources + `GarbageFacility`,
  comme le yard Tigon, pour une ligne train-poubelle dédiée), puis quais spécialisés inspirés des
  yards Tigon (charbon, pétrole, agricole, bois) ; gares voyageurs Tigon acceptées si Tigon présent.
- **V3** — Tailles et souterrain : variantes de l'extension (une voie / deux voies), version
  souterraine (sous-réseaux à y = −20) pour les gares de métro-RER.
- **V4** — Tram et métro mixtes (rejoint le jalon 4 cargo tram) : ligne, arrêt et véhicule cargo
  clonés pour Tram/Subway, onglet dans la vue d'ensemble des transports.

### Écarts de V1 au code (05/10/2026)

- Dossier `src/SolarpunkMod/Station/` (`StationPlatformSystem`, `StationPlatformLocale` depuis la V2).
- Le quai est un **`BuildingPrefab` cloné** de `CargoTrainTerminal01` (comme le Local Hub) avec un
  `ServiceUpgrade`, pas un `BuildingExtensionPrefab` : c'est la forme des extensions ferroviaires de
  Tigon (`CargoTrainTerminal01 Rail Extension`, `m_MaxPlacementDistance` 200) et `ServiceUpgrade` est
  autorisé sur `BuildingPrefab` (`ServiceUpgrade.cs:8-14`). Gros lot (38x17) en V1 ; tailles en V3.
- Gares acceptées : tout prefab avec `PublicTransportStationData` dont un sous-objet est un arrêt Train
  voyageurs, **gares Tigon comprises** si Tigon est installé (détection par les données).
- **Trouvaille** : `StorageCompanySystem` lit `StorageLimitData` et `StorageCompanyData` du prefab du
  bâtiment qui porte le stock **avant** d'y ajouter les améliorations
  (`StorageCompanySystem.cs:264-269`) ; une gare voyageurs n'en a pas. Le mod copie donc sur chaque
  gare acceptée les données cargo du terminal (`StorageCompanyData`, `StorageLimitData`,
  `CargoTransportStationData`, `TransportCompanyData`), données de prefab seulement. Les requêtes qui
  cherchent des prefabs d'entrepôt exigent aussi `IndustrialProcessData` : pas d'effet sur les gares
  sans quai. Stock d'une gare équipée ≈ 2× celui du terminal (base copiée + amélioration).
- Coût de l'amélioration : 60 000, comme les extensions Tigon.

### V2 partielle — Quai déchets (05/10/2026, in-progress, codé à l'aveugle)

**Ce que tu verras en jeu** : sur une gare voyageurs, une seconde amélioration « **Quai déchets** »
(60 000) à côté du « Quai fret ». Une fois posé, les bennes y déchargent, une ligne de train cargo
dédiée aux déchets emporte les ordures vers un autre quai déchets ou un yard déchets Tigon, et une
partie est traitée sur place. Retirer le mod : le quai devient un « objet manquant ».

- `FreightPlatformSystem` devient `StationPlatformSystem`, qui crée les deux améliorations avec le même
  clonage (`AddPlatform`) ; le quai déchets ne diffère que par `MakeWastePlatform`.
- Quai déchets = clone de `CargoTrainTerminal01` avec `CargoTransportStation.m_TradedResources` =
  `[Garbage]` et un `GarbageFacility` aux valeurs du `MediumTrainTrashYard` de Tigon (capacité 500 000,
  5 bennes, 1 transport, traitement 25 000) — valeurs reprises, aucune dépendance à Tigon.
  `GarbageFacility` est un `IServiceUpgrade` : il greffe `Game.Buildings.GarbageFacility`, `Resources`,
  `ServiceDispatch`, `OwnedVehicle` sur la gare (`GarbageFacility.cs:61-70`) ; le stock de déchets est
  le même buffer `Resources` que celui du fret (`GarbageFacilityAISystem.cs:242`).
- **Même écart qu'en V1** : `GarbageFacilityAISystem` indexe `GarbageFacilityData` du prefab de la
  gare avant d'y ajouter les améliorations (`GarbageFacilityAISystem.cs:227-234`) ; le mod donne à
  chaque gare acceptée un `GarbageFacilityData` vide (addition neutre, `GarbageFacilityData.Combine`).
  Les ressources stockées s'additionnent par OU (`StorageCompanyData.Combine`) : Garbage s'ajoute au
  stock de la gare.
- Hypothèses invérifiables sans le jeu : les bennes trouvent un accès au quai (celui des camions du
  terminal cloné) ; le traitement sur place dans une gare voyageurs est acceptable visuellement ;
  le `GarbageFacilityData` vide sur toutes les gares voyageurs n'a pas d'effet d'affichage (vue
  déchets, infobulle) ; quai fret + quai déchets sur la même gare cumulent bien les deux.
- Reste de la V2 : quais spécialisés par usage Tigon (charbon, pétrole, agricole, bois).

**Recette V2 (à fusionner dans la session « Gare mixte »)** :
1. Sur une gare voyageurs : « Quai déchets » proposé à côté de « Quai fret » ; le poser, raccorder sa
   voie. Des bennes viennent y décharger (stock de déchets au clic sur la gare).
2. Créer une ligne de train cargo entre ce quai et un autre quai déchets (ou un yard déchets Tigon) :
   des trains-poubelles chargent et déchargent ; la vue déchets montre la gare comme installation ;
   pas d'erreur dans le log.

### Étapes de V1

Dossier : `src/SolarpunkMod/Stations/`.

1. `FreightPlatformPrefabSystem` (sans phase, comme `EndResidentialParkingPolicySystem`),
   en `OnGamePreload`, une fois par session :
   - trouver la gare de marchandises vanilla source : prefabs `BuildingPrefab` avec
     `CargoTransportStation`, voie de type Train, pas issus d'un mod (contrôler la source de l'asset ;
     à défaut, la plus petite par lot), log du nom retenu ;
   - créer `ScriptableObject.CreateInstance<BuildingExtensionPrefab>()` nommé `SolarpunkFreightPlatform`,
     `m_ExternalLot = true`, lot et maillages repris de la source ;
   - copier depuis la source : `ObjectSubNets`, `ObjectSubObjects`, `ObjectSubLanes`,
     `CargoTransportStation` (avec `m_TradedResources`), `StorageLimit`, `Workplace` si présent ;
   - ajouter `ServiceUpgrade` (`m_Buildings` = gares voyageurs vanilla, `m_UpgradeCost` repris du coût
     de la source à l'échelle), `UIObject` (icône vanilla de gare de marchandises) ;
   - `PrefabSystem.AddPrefab`.
2. Textes en/fr : nom « Quai fret » / « Freight platform », description (« la gare accueille une ligne
   de marchandises sur son propre quai »).
3. `Mod.cs` : enregistrer le système et les textes.
4. `/simplify`, `dotnet build` vert.

## Sauvegarde

- Rien de nouveau n'est sérialisé par le mod lui-même.
- Avec le quai posé : l'extension est une entité dont le prefab vient du mod, et la gare porte les
  composants vanilla greffés (`StorageCompany`, `Resources`…).
- Retrait du mod : l'extension devient un prefab obsolète affiché « objet manquant »
  (`ResolvePrefabsSystem.cs:535-545`). **[S]** Les composants greffés restent sur la gare voyageurs
  tant que l'extension (obsolète) existe ; démolir la boîte devrait les retirer via
  `ServiceUpgradeSystem` (`:174-228`). À vérifier en recette : la gare charge et fonctionne, démolir la
  boîte rend une gare voyageurs normale.

## Risques

- **[S] Accès** : la voie et l'arrêt copiés doivent rester connectés à leur accès route
  (`NoCargoAccess` sinon). On copie l'ensemble cohérent sous-réseaux + sous-objets + sous-voies, sans
  rien déplacer.
- **[S] Lot externe** : la position de l'extension par rapport à la gare (`m_Position`) et le
  raccordement de sa voie au réseau du joueur ; si le lot externe ne se raccorde pas, repli : le joueur
  raccorde la voie du quai lui-même, comme une gare de marchandises.
- **[S] Archetype de la gare** : la gare voyageurs reçoit `StorageCompany` sans en avoir le prefab
  data (`StorageCompanyData`) ; vanilla le fait pour l'aéroport, mais à confirmer pour une gare.
- **Camions** : le stock attire des camions de livraison, contraire à l'esprit « sans camion » ; à
  combiner plus tard avec le Local Hub (plan 007).

## Recette de V1 (3 scénarios)

1. **Poser** : sur une gare voyageurs vanilla, le panneau d'amélioration propose « Quai fret » ; le
   poser, raccorder sa voie. Log : nom de la gare source retenue.
2. **Lignes** : créer une ligne voyageurs et une ligne de marchandises passant par cette gare ; chacune
   s'arrête sur son quai, des voyageurs montent, du fret est chargé (panneau de la gare : ressources).
3. **Retrait du mod** : sauvegarder, retirer le mod, recharger : la partie charge, la gare voyageurs
   fonctionne ; démolir la boîte du quai rend une gare normale.

## Hors périmètre

- Quai déchets (V2), tailles et souterrain (V3), tram et métro (V4).
- Nouveau modèle 3D : la V1 réutilise les maillages de la gare de marchandises vanilla.
- Filtrage des camions attirés par le stock (combinaison avec le Local Hub, plus tard).

## Écarts après audit du 05/10

- Base neutre greffée sur les gares (ressources aucune, stockage 0, transports 0, déchets vides) : les quais n'ajoutent que leurs propres données (plus de doublement, le quai déchets ne stocke plus le fret). Désinstallation : retirer les quais avant d'enlever le mod.
