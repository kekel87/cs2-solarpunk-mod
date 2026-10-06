---
status: in-progress
created: 2026-10-05
updated: 2026-10-05
---

# Plan 009 — Déchets bout en bout (jalon 3)

## Ce que tu verras en jeu (lot A)

- Un incinérateur ou une décharge du jeu de base propose l'amélioration **« Embranchement
  ferroviaire »** : une voie et un quai fret accolés au bâtiment.
- Une ligne de train de marchandises relie un yard déchets de Tigon (côté ville) à l'installation
  embranchée : **les trains-poubelle déchargent directement à l'incinérateur**, sans bennes entre les deux.
- Au clic sur l'incinérateur, les déchets stockés montent quand un train arrive et baissent au
  rythme du traitement.
- Retirer le mod : l'embranchement devient un « objet manquant », l'incinérateur continue de
  fonctionner avec ses bennes.

## Contexte

- Jalon 3 de `docs/design.md` : « incinérateur / centre de tri reliés au rail, à l'autre bout des
  yards de Tigon ; indicateur déchets collectés par train au panneau ».
- Décision 010 (graphe) : on s'appuie sur Tigons Rail Infrastructure (133250). Ses « Train Garbage
  Yards » font déjà circuler des trains de déchets **entre deux yards de la ville** (testé en jeu le
  03/10, `question-ouverte-tigons-garbage-yards`) ; ce qui manque, c'est **l'autre bout** :
  l'installation de traitement embranchée (`feedback-traitement-dechets-embranche`).
- Plan 008 (gare mixte) : même technique d'extension (`ServiceUpgrade` + `CargoTransportStation`),
  même écart découvert en V1 (données de stockage à copier sur le bâtiment principal). Le lot A
  réutilise `src/SolarpunkMod/Station/` plutôt que de le refaire.
- Règle de sauvegarde assouplie (CLAUDE.md, 05/10/2026) : une extension du mod peut devenir un
  « objet manquant » au retrait.

## Faits vérifiés dans le décompilé (1.6.2f1) et dans Tigon

- **[V] Le yard déchets de Tigon = un seul bâtiment à deux composants** :
  `MediumTrainTrashYard.Prefab` porte `CargoTransportStation` (`m_TradedResources` = Garbage) **et**
  `GarbageFacility` (`m_GarbageCapacity` 500 000, `m_ProcessingSpeed` 25 000, `m_TransportCapacity` 1,
  `m_VehicleCapacity` 5), plus `StorageLimit`. Wagon : `TO_TrainTrashCar01` (Garbage, Fuel).
- **[V] Stock cargo et stock déchets sont le même buffer** : `GarbageFacilityAISystem` lit et écrit
  les déchets dans `Game.Economy.Resources` à la ressource `Resource.Garbage`
  (`Game.Simulation/GarbageFacilityAISystem.cs:239-263`). Un train qui décharge des déchets dans le
  stock cargo du bâtiment les met donc **directement à traiter**.
- **[V] Le train décharge dans le premier `StorageCompany` en remontant les propriétaires de
  l'arrêt** (`TransportTrainAISystem.cs:1200-1215`, `GetStorageCompanyFromStop`) : un arrêt posé par
  une extension charge et décharge dans le bâtiment principal.
- **[V] Les deux composants sont des extensions possibles** : `GarbageFacility` et
  `CargoTransportStation` implémentent `IServiceUpgrade` (`Game.Prefabs/GarbageFacility.cs:18`,
  `CargoTransportStation.cs:74-86` : `GetUpgradeComponents` greffe `StorageCompany`, `Resources`,
  `TradeCost`, `StorageTransferRequest`, `TransportCompany` sur le bâtiment).
- **[V] Les transferts de déchets passent déjà par le rail** : `GarbageTransferDispatchSystem`
  cherche les chemins en `PathMethod.Road | PathMethod.CargoLoading` (`:270, :276, :286`) et
  n'accepte que des destinations qui portent `GarbageFacility` (`:206`). Un incinérateur embranché
  (GarbageFacility + arrêt cargo relié à une ligne) devient donc une destination atteignable par train.
- **[V] Même écart qu'au plan 008** : `StorageCompanySystem` lit `StorageCompanyData` sur le prefab
  du bâtiment **avant** d'additionner les extensions (`StorageCompanySystem.cs:264-269`). Un
  incinérateur vanilla n'en a pas : il faut copier les données cargo (`StorageCompanyData`,
  `StorageLimitData`, `CargoTransportStationData`, `TransportCompanyData`) sur les prefabs
  d'incinérateur/décharge acceptés, comme `StationPlatformSystem` le fait déjà pour les gares.
- **[V] Compter les déchets transportés par train** : les wagons portent un buffer `Resources`
  (lu par `StorageCompanySystem.cs:318`) ; on somme `Resource.Garbage` sur les trains `CargoTransport`
  en circulation (hors `DummyTraffic`), comme le panneau compte déjà les trains de marchandises.

## Décisions proposées

1. **Lot A = extension « Embranchement ferroviaire »** sur les incinérateurs et décharges vanilla,
   clonée d'une gare de marchandises vanilla (comme le quai fret du plan 008), `m_TradedResources`
   réduit à **Garbage**. Pas de nouveau bâtiment de traitement.
2. **Réutiliser `Station/`** : un clone de plus du `StationPlatformSystem` (paramétré par la liste de
   bâtiments cibles et les ressources), pas un second système parallèle.
3. **Tigon reste optionnel** : sans Tigon, l'embranchement reçoit quand même des déchets depuis un
   autre embranchement, ou depuis le quai déchets de la gare mixte (plan 008 V2) ; avec Tigon, depuis
   ses yards.
4. **Indicateur au panneau** (lot B) : « Déchets en train » = tonnes de déchets à bord des trains en
   circulation, plus le nombre de trains-poubelle ; dans le panneau ville, et dans la section
   district si le train y circule.

## Lots

- **A** — Extension « Embranchement ferroviaire » sur incinérateurs et décharges vanilla (déchets
  reçus par train, traités sur place).
- **B** — Indicateur « Déchets en train » au panneau ville (et section district).
- **C** — Centre de tri / recyclage embranché (si le jeu a un bâtiment de recyclage vanilla : même
  extension ; sinon, plus tard, avec un bâtiment dédié).

### Étapes du lot A

1. Généraliser `Station/StationPlatformSystem` : cible (prédicat sur le prefab) + ressources
   échangées + nom/description en paramètres ; le quai fret du 008 devient une instance.
2. Instance « Embranchement ferroviaire » : cibles = prefabs avec `GarbageFacility` et sans
   `CargoTransportStation` (incinérateurs, décharges ; exclure les yards Tigon qui l'ont déjà),
   ressources = Garbage. Copier les données cargo sur ces prefabs (écart vérifié ci-dessus).
3. Textes en/fr (nom, description : « reçoit les déchets par train »).
4. Log au chargement : nombre d'installations qui proposent l'embranchement.
5. `/simplify`, `dotnet build` vert.

### Étapes du lot B

1. Dans `SolarpunkInfoUISystem` : requête trains `CargoTransport` + buffer `Resources`, somme de
   `Resource.Garbage`, nombre de trains qui en portent. Deux bindings.
2. UI : deux lignes dans la section « Véhicules » du panneau ville.

## Sauvegarde

- Lot A : l'extension est un prefab du mod → « objet manquant » au retrait (accepté). Les déchets
  déjà dans le stock restent dans le buffer `Resources` de l'incinérateur, que le jeu traite
  normalement. Données de prefab copiées : non sauvegardées.
- Lot B : lecture seule.

## Risques

- [S] **Copie des données cargo sur un incinérateur** : effets de bord possibles (l'incinérateur
  devient aussi « entrepôt » et pourrait recevoir ou vendre autre chose que des déchets si
  `m_StoredResources` déborde). Limiter `m_StoredResources` à Garbage.
- [S] **Concurrence bennes / train** : la priorité d'acceptation de l'incinérateur
  (`m_AcceptGarbagePriority`) n'est pas modifiée ; les bennes continuent de livrer, le train s'ajoute.
- [S] **Raccordement** de la voie du clone (gabarit de la gare de fret, grand) : même incertitude
  qu'au plan 008 V1.
- [S] Le wagon déchets de Tigon est marqué Fuel : avec l'option « trains électriques seulement »
  (plan 003), il est neutralisé, rien à faire ici.

## Recette du lot A (3 scénarios)

1. Cliquer un incinérateur : l'amélioration « Embranchement ferroviaire » est proposée ; la poser,
   raccorder sa voie. Log : `Rail siding offered on N garbage facilities`, N > 0.
2. Ligne de train de marchandises entre un yard déchets Tigon et l'embranchement : un train-poubelle
   part plein du yard, arrive à l'incinérateur et repart vide ; le stock de déchets de l'incinérateur
   monte puis baisse au rythme du traitement.
3. Retrait du mod : la partie charge, l'embranchement est un « objet manquant », l'incinérateur
   traite toujours les déchets apportés par bennes.

## Hors périmètre

- Bâtiment de traitement dédié, modélisé par nous (centre de tri solarpunk).
- Tri / recyclage simulé finement (le jeu traite les déchets comme une seule ressource).
- Gare de transbordement souterraine en centre-ville.
- Compteur « bennes évitées » (contrefactuel douteux, comme le CO₂ écarté au panneau v1).

## Décisions pour l'humain

1. L'embranchement est une **amélioration posée sur un incinérateur ou une décharge existants**
   (pas un nouveau bâtiment de traitement) : d'accord ?
2. Les **bennes continuent de livrer** l'incinérateur en plus du train (on ne les interdit pas) :
   d'accord, ou faut-il réserver l'incinérateur au train ?

## Décisions de l'humain (05/10/2026)

- Lot A en **amélioration** « Embranchement ferroviaire » sur incinérateur / décharge vanilla : validé.
- **Train + bennes** : les bennes continuent de livrer l'installation en plus du train.
- **Manquent au plan** : le **centre de recyclage** et les **déchets industriels** (à couvrir : lot C
  élargi aux bâtiments de recyclage vanilla et aux déchets industriels — vérifier comment le jeu les
  modélise).
- **Nouveauté « top tier »** : une **grande usine intégrée recyclage + incinération + déchets
  industriels**, nouveau bâtiment du mod, reliée au rail (lot D, après A–C ; modèle 3D à faire).

## Lot A — codé à l'aveugle le 05/10/2026 (non recetté)

- `Waste/RailSidingSystem.cs` + `Waste/RailSidingLocale.cs` : amélioration
  `SolarpunkWasteRailSiding` (« Embranchement ferroviaire », 50 000), clone de `CargoTrainTerminal01`
  limité aux déchets, proposée sur **tout prefab de bâtiment avec `GarbageFacilityData`, sans
  `CargoTransportStationData` et sans être une amélioration** (incinérateurs, décharges, installations
  de déchets industriels, et le centre de recyclage s'il est un `GarbageFacility` — [S], aucun
  composant « recyclage » dans `Game.Prefabs` ; la liste des installations retenues est écrite dans le
  log au chargement). Les yards Tigon (déjà `CargoTransportStation`) sont exclus.
- `Station/CargoUpgradeCloner.cs` : factorisation commune (clone d'amélioration, base cargo neutre
  greffée sur le prefab hôte — même logique que l'audit, pas de doublement) ; `StationPlatformSystem`
  l'utilise, comportement des quais inchangé.
- Écart au plan : pas de « Rail siding offered on N garbage facilities » sur un compteur seul, le log
  liste aussi les noms. Pas de `GarbageFacility` ajouté par l'amélioration : l'hôte en a déjà un.
- [S] Même mécanique que le yard déchets de Tigon (GarbageFacility + CargoTransportStation Garbage),
  validé en jeu le 03/10 : le stockage cargo peut aussi échanger des déchets avec d'autres stockages
  (`StorageCompanySystem`), comme le yard.
- Risque au retrait (accepté, décision humain) : l'installation garde les composants de stockage
  sans les données de prefab → retirer les embranchements avant de désinstaller.

### Recette du lot A (regroupée)

1. Log : `Rail siding offered on N garbage facilities: …` (relire la liste). Cliquer un incinérateur
   ou une décharge : « Embranchement ferroviaire » proposé ; le poser à moins de 200 m, raccorder sa
   voie.
2. Ligne cargo entre un yard déchets Tigon (ou un quai déchets de gare) et l'embranchement : un
   train-poubelle part plein, décharge à l'installation, repart vide ; au clic, les déchets stockés
   montent puis baissent au rythme du traitement ; les bennes livrent toujours.

## Lot B — Indicateur « Déchets en train » (codé à l'aveugle le 05/10/2026, non recetté)

- Panneau ville : nouvelle section « Déchets en train » : nombre de trains-poubelle et déchets à bord.
  Lecture seule, rien en sauvegarde. Code : `Info/SolarpunkInfoUISystem.UpdateCargoTrains`.
- Mesure : trains cargo en circulation (véhicule de tête, transit `DummyTraffic` exclu) ; chaque
  wagon porte son propre buffer `Resources` (`TransportTrainAISystem.cs:975`), on somme
  `Resource.Garbage` sur tout le `LayoutElement` du train. Un train-poubelle = un train qui en porte.
- Unité : montant brut affiché avec `Unit.Weight`, comme la section déchets vanilla
  (`ui/index.js` ~120904, `GarbageSection.cs:109`).
- Écart : pas ajouté à la section district (un train sur voie ferrée n'a pas de district via
  `BorderDistrict`, cf. plan 004).

Recette :
1. Panneau ville ouvert, une ligne de train-poubelle en service (yard Tigon ou quai déchets →
   embranchement) : « Trains-poubelle » ≥ 1 et « À bord » non nul quand le train roule plein, qui
   retombe quand il décharge.
2. Sans ligne de déchets par train : 0 et 0 ; « Trains de marchandises » inchangé par rapport à hier.
