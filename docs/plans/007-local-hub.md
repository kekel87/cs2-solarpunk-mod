---
status: in-progress
created: 2026-10-05
updated: 2026-10-05
---

# Plan 007 — Local Hub (jalon 2 « Quartier sans camion »)

> **Mise à jour du 05/10/2026 (dev des lots A et B, à l'aveugle).**
> - **Bloquant levé par une décision de l'humain (piste 1)** : le jeu refuse une gare comme vendeur
>   d'un commerce (`ProcessResourceBuyer` exige `PropertyRenter`, `ResourceBuyerSystem.cs:587-589`,
>   sinon `CompanyStaleDestination`, `:692-698`). **Le mod fait donc la vente lui-même** :
>   `Hub/HubSaleSystem.cs` intercepte l'achat d'un commerce *avant* que le jeu ne cherche un vendeur
>   (`ResourceBuyer` sans `PathInformation`), reproduit le volet argent de `BuyJob` pour un vendeur
>   stockage (prix industriel + `TradeCost`, mise à jour des deux coûts, `m_LastTradePartner`,
>   statistiques), pose `CurrentTrading` et `Target` comme le jeu, puis le vrai trajet : `TripNeeded`
>   `CompanyShopping` (camion du jeu) ou, à moins du rayon, petit véhicule créé par le mod. La
>   marchandise quitte le hub au chargement du véhicule, comme en vanilla. Plus de réécriture de
>   `m_Destination` ni de `m_Distance` : la distance est à vol d'oiseau commerce → hub.
> - **Règle de sauvegarde assouplie par l'humain (CLAUDE.md)** : un bâtiment du mod peut devenir un
>   « objet manquant » au retrait, comme tout asset. **Option 1 retenue** : le Local Hub reste un
>   clone autonome de `CargoTrainTerminal01` ; plus d'échange de `PrefabRef` ni de composant
>   `LocalHub` sérialisé. Un hub est reconnu par son prefab.
> - **Lot B** : le petit véhicule est, pour chaque ressource, le véhicule de livraison du jeu de plus
>   petite capacité qui la transporte (moto de livraison pour les marchandises légères), créé comme
>   `TripNeededSystem.SpawnDeliveryTruck` avec un conducteur habitant du jeu. Quantité vendue alignée
>   sur sa capacité. Rayon réglable (Options, défaut 600 m). Le panneau district le compte à part.
> - **Écart** : le hub stocke les ressources de la gare vanilla (`m_TradedResources` d'origine) ;
>   l'élargissement proposé par le game-designer n'est pas fait (à décider après recette).

## Ce que tu verras en jeu (lot A)

- Un nouveau bâtiment **« Local Hub »** dans le menu Transport → fret : une gare de marchandises
  de quartier (même modèle 3D qu'une gare vanilla au départ), avec son propre nom et sa description.
- Tu le poses dans un district et tu le relies au rail : les commerces **de ce district**
  s'approvisionnent au hub tant qu'il a du stock (trajets courts dans le quartier), sinon comme avant.
- Le train remplit le hub comme n'importe quelle gare de marchandises.
- Encore des camions à ce stade : les petits véhicules arrivent aux lots B et C.
- Retirer le mod : le hub redevient une gare de marchandises vanilla ordinaire, la partie charge.

## Objectif

Créer une gare de marchandises « Local Hub » dédiée à un district, qui centralise l'approvisionnement
des commerces locaux via une seule livraison (au lieu de camions individuels), en réutilisant l'API
existante du jeu (pas de Harmony).

## Contexte

- Jalon 2 de `docs/design.md` : « District livré sans camion ». Précédent : *Plazas & Promenades* de
  CS1, avec ses points de service qui livrent les zones piétonnes avec de vrais véhicules.
- Les véhicules prévus sont les 4 modèles de `assets/vehicles/` (plan 006). Leur import en jeu se
  fera plus tard.
- La mesure vient du panneau district (plan 004).
- Modèles de code : *Local Logistics* (150547, licence MIT) pour le dispatch entrepôt → commerces,
  *Dedicated Cargo Facilities* (156605) pour l'interception des transferts.
- Décisions de l'humain du 05/10/2026 : nouveau bâtiment « Local Hub » (pas une politique sur une
  gare existante), petits véhicules créés par le mod (option B2, lot B).
- Ce plan corrige `capacites-vanilla.md` : la piste `SaleFlags.Virtual` (vente sans véhicule) est
  écartée, à la demande de l'humain (« pas de magie »).

## Faits vérifiés dans le décompilé (1.6.2f1)

**Comment un commerce s'approvisionne**

1. `BuyingCompanySystem` (`Game.Simulation/BuyingCompanySystem.cs:180-208`) choisit d'abord un
   camion pour la quantité manquante (`DeliveryTruckSelectData.TrySelectItem`). Il pose ensuite un
   `ResourceBuyer` avec `m_AmountNeeded = min(besoin, capacité du camion)` et les drapeaux
   `Industrial | Import`. **[V]**
2. `ResourceBuyerSystem` lance un pathfinding vers un vendeur (`SetupTargetType.ResourceSeller`,
   `:785`, `:900`). Le résultat arrive dans `PathInformation.m_Destination` du **commerce acheteur**.
   **[V]**
3. Au passage suivant, `ProcessResourceBuyer` (`ResourceBuyerSystem.cs:580-680`) :
   - lit `m_Destination` ;
   - vérifie le stock du vendeur (au moins la moitié de la demande, `:597-605`) ;
   - émet la vente (`SalesEvent`) ;
   - ajoute un `TripNeeded { m_TargetAgent = vendeur, m_Purpose = CompanyShopping }` au **commerce**
     (`:664-670`). **[V]**
4. `TripNeededSystem.SpawnDeliveryTruck` (`TripNeededSystem.cs:124-245`, appelé en `:382`) fait
   partir le camion **du commerce** : il appartient au commerce (`Owner = commerce`) et a pour cible
   le vendeur, avec le drapeau `Buying` (`:181-186`).
   - Le véhicule est choisi par `TrySelectItem(ressource, quantité)` (`:221`), dans une liste
     **globale** : le bâtiment de départ n'entre pas en compte.
   - Le conducteur est créé d'après les `ActivityLocation` (`Driving` / `Biking`) du prefab
     (`:226-229`, `:252-300`). **[V]**
   - Conséquence : **le commerce va chercher la marchandise**. Ce n'est pas le vendeur qui livre.
     Un « hub qui livre » se traduit donc par « le commerce va chercher au hub, tout près, avec un
     petit véhicule ».

**Qui peut être vendeur**

- La requête des vendeurs inclut `StorageCompany`, `CargoTransportStation` et `ResourceSeller`
  (`ResourcePathfindSetup.cs:662-683`). Les gares avec arrêt de train (`TrainStop`) en sont
  exclues, mais le bâtiment de gare lui-même ne l'est pas. **[V]**
- Avec le drapeau `Import`, que portent les commerces, une gare de marchandises
  (`CargoTransportStation`) est un vendeur valide pour toute ressource de ses `m_StoredResources`
  (`:126`, `:148-158`). **[V]**

**Le hub côté bâtiment**

- Une gare de marchandises vanilla est déjà « un entrepôt avec un arrêt dessus »
  (`Game.Prefabs/CargoTransportStation.cs`). Elle porte `StorageCompany`, `Resources`,
  `StorageTransferRequest` et `TradeCost`, et liste ses ressources échangées dans
  `m_TradedResources`. **[V]**
- Elle se remplit par les lignes de train cargo (le chargement transfère du stock, d'après
  `capacites-vanilla.md` §1) et par les transferts entre entrepôts (`StorageTransferSystem`).
- **Le hub existe donc déjà en vanilla** : c'est la gare de marchandises. Il manque seulement que les
  commerces du district la choisissent.
- À noter : la 1.6 a ajouté un service `GoodsDelivery` (`GoodsDeliveryDispatchSystem`,
  `GoodsDeliveryFacilityAISystem`), mais il sert à l'**entretien des bâtiments de service**
  (`BuildingUpkeepSystem.cs:159`), pas aux commerces. On ne le réutilise pas. **[V]**

**Où intercepter, sans Harmony**

- Un système à nous `HubSellerSystem`, placé `UpdateBefore<ResourceBuyerSystem>` (phase
  `GameSimulation`, `SystemOrder.cs:395`), réécrit `PathInformation.m_Destination` = le hub du
  district :
  - pour les acheteurs qui ont un `ResourceBuyer` et un chemin trouvé (`PathFlags.Pending` absent) ;
  - **si le hub existe dans le même district** que le commerce (`CurrentDistrict` : hub et commerce
    au même endroit) ;
  - **et** si la hub a la ressource en stock (au moins la moitié de la demande, même règle que le
    jeu) ;
  - sinon, `ResourceBuyerSystem` utilise la cible vanille (pas de réécriture, comportement normal).
- `ResourceBuyerSystem` fait ensuite tout le reste normalement : vente, prix, trajet. **[V]** pour le
  point d'entrée.
- **[S]** à confirmer : aucun chemin n'est copié vers le véhicule pour un achat (`entity2 = Null`,
  `TripNeededSystem.cs:139-141`). Le véhicule recalcule donc lui-même son chemin vers la cible
  réécrite.

**Imposer le petit véhicule (lot B)**

- Option B1, remplacement après création : un système après `TripNeededSystem` remplace
  `PrefabRef` sur les nouveaux `DeliveryTruck` dont `Target` = un hub et `Owner` = un commerce du
  district. **[S]** Limites :
  - les camions avec remorque ont une `LayoutElement` (`DeliveryTruckSelectData.cs:106-135`) ;
  - le conducteur a déjà été créé d'après le prefab d'origine (pose « conduite », alors qu'un vélo
    demande « pédalage »).
- Option B2, création par nous : avant `TripNeededSystem`, on retire du buffer `TripNeeded` les
  trajets vers un hub, puis on crée le véhicule avec la surcharge publique
  `CreateVehicle(…, DeliveryTruckSelectItem, …)` (`DeliveryTruckSelectData.cs:106`). C'est plus
  propre (bon prefab, bon conducteur), mais il faut recopier environ 60 lignes de `SpawnDeliveryTruck`
  et la création des passagers. **[V]** pour l'API, **[S]** pour le reste.
- Pour que nos petits prefabs ne servent qu'au hub, on les clone au chargement avec une exigence
  jamais remplie, ce qui les sort de la liste globale (`UpdateDeliveryTruckSelectJob.cs:62`). **[S]**
- Le panneau district (plan 004) compte tout `DeliveryTruck` comme un camion, motos comprises. Le
  lot B doit lui faire distinguer les petits véhicules.

## Ce que fait le jeu d'un bâtiment dont le prefab a disparu (mod retiré)

- Au chargement, un prefab de la sauvegarde introuvable devient un prefab **obsolète** : son entité
  est gardée, `PrefabData` désactivé, et il est inscrit par `AddObsoleteID`
  (`Game.Serialization/ResolvePrefabsSystem.cs:535-545`). Les objets posés gardent leur `PrefabRef`
  vers lui. **[V]**
- `InitializeObsoleteSystem` lui donne alors un archétype minimal et, faute de maillage, le
  **maillage « objet manquant »** (`m_MissingObjectMesh`, `InitializeObsoleteSystem.cs:140-150`). **[V]**
- Conséquence : la partie charge (chemin prévu par le jeu, le même que pour un asset de Paradox Mods
  désinstallé), mais le hub devient une **boîte « objet manquant » inerte** à démolir. Pas de
  corruption, mais une trace visible. **[V]** pour le mécanisme, **[S]** pour l'absence d'effet de
  bord sur l'entreprise de stockage logée dans le bâtiment.
- Une gare vanilla ne peut pas « réclamer » l'ID de notre prefab (`ObsoleteIdentifiers` sert à
  l'inverse : un prefab présent qui reprend d'anciens IDs, `Game.Prefabs/ObsoleteIdentifiers.cs`).

## Options pour le bâtiment « Local Hub »

| | Option 1 — prefab cloné, gardé tel quel | Option 2 — prefab cloné pour la pose, sauvé en gare vanilla + marqueur (recommandée) |
|---|---|---|
| Barre d'outils | « Local Hub » (notre clone) | « Local Hub » (notre clone) |
| Après la pose | le bâtiment référence notre clone | un système remplace aussitôt `PrefabRef` par la gare vanilla d'origine et ajoute un composant `LocalHub` sérialisé |
| Sauvegarde | référence notre prefab | ne référence que du vanilla + un composant à nous |
| Mod retiré | boîte « objet manquant » inerte | le composant `LocalHub` est perdu sans dégât (précédent `StreetParkingBan`, jalon 1 bis) : il reste une gare de marchandises vanilla qui marche |
| Nom affiché | celui du clone | nom personnalisé « Local Hub » posé à la création (`NameSystem`, chaîne sauvegardée, inoffensive sans le mod) |
| Risque | faible en dev, trace au retrait | **[S]** l'échange de `PrefabRef` juste après la pose : clone et original ont les mêmes composants, sous-objets et sub-nets (`PrefabSystem.DuplicatePrefab` → `PrefabBase.Clone`, `PrefabSystem.cs:269-275`, `PrefabBase.cs:399-414`), donc même archétype ; à valider en jeu |

## Décisions

1. **Le hub est un nouveau bâtiment « Local Hub »** (décision de l'humain, 05/10/2026), créé par
   code au pré-chargement avec `PrefabSystem.DuplicatePrefab(gare de marchandises vanilla,
   "SolarpunkLocalHub")` : même modèle, nom, icône et textes propres, rangé dans la catégorie fret
   de la barre d'outils. **Option 2 recommandée** pour la sauvegarde ; repli sur l'option 1 si
   l'échange de `PrefabRef` pose problème en jeu.
2. **Exclusivité** (comportement attendu) : les commerces du district qui contient le hub
   (`CurrentDistrict` du bâtiment) achètent seulement au hub tant qu'il a le stock ; sinon
   comportement vanilla, pas de pénurie.
3. **Petits véhicules : le mod crée lui-même le véhicule** (option B2 ci-dessus, décision de
   l'humain) : bon prefab, cycliste en pose vélo, pas de remorque.

## Lots

| Lot | Contenu | Testable en jeu |
|---|---|---|
| **A** | Bâtiment « Local Hub » (clone de gare de marchandises, option 2) + réécriture du vendeur pour les commerces de son district. Encore des camions vanilla | Commerces du district servis par le hub, trajets courts |
| **B** | Le mod crée lui-même le véhicule des trajets hub (B2) : moto de livraison vanilla `MotorbikeDelivery01` d'abord. Le panneau district les compte à part | Motos au lieu de camions dans le district |
| **C** | Nos 4 modèles importés (Asset Importer, avec l'humain), choisis selon la quantité : vélo-cargo < remorque palette < triporteur < kei truck | Vélos-cargo dans les rues |

### Étapes du lot A

1. `Hub/LocalHubPrefabSystem.cs`, en `OnGamePreload` (une fois) :
   - trouver la gare de marchandises ferroviaire vanilla (prefab avec `CargoTransportStation` et
     arrêt de train) ;
   - `DuplicatePrefab(gare, "SolarpunkLocalHub")`, icône et `UIObject` dans la catégorie fret ;
   - garder la paire clone → original.
2. `Hub/LocalHubPlacementSystem.cs` (option 2), phase `Modification`, après les systèmes de création
   de bâtiments (`BuildingInitializeSystem`, `BuildingInitializeNamesSystem`, etc.) et **avant**
   les systèmes qui lisent les bâtiments (`ResourceBuyerSystem`, `BuildingEfficiencySystem`). Si
   position ambiguë, placer après `ObjectInitializeSystem` (qui crée la pose) — position exacte à
   valider dans `SystemOrder.cs` :
   - bâtiments `Created` sans `Temp` dont `PrefabRef` = le clone → `PrefabRef` = l'original.
   - `AddComponent(LocalHub)` (composant marqueur vide).
   - Lire ou créer un composant `Name` vanilla avec le texte « Local Hub » ; sinon laisser le jeu
     gérer le nom du prefab original. À valider en jeu (scénario 1 recette).
3. `Hub/LocalHub.cs` : composant marqueur `[GenerateSerializable]`, utilisé pour identifier les hubs
   en jeu (distinction vs gare vanilla). Zéro donnée : juste la présence du composant signifie "c'est
   un hub". Perdu sans dégât au retrait du mod.
4. `Hub/HubSellerSystem.cs`, `UpdateBefore<ResourceBuyerSystem>` (`GameSimulation`) :
   - faire un dictionnaire : pour chaque district, chercher les hubs (`LocalHub` composant) qui y
     sont situés (`CurrentDistrict`) ;
   - pour chaque acheteur actif (`ResourceBuyer` + `PathInformation` sans `Pending`) qui est un
     commerce (`ServiceAvailable`), vérifier son district :
     - s'il a un hub et que le hub a le stock (moitié de la demande, même règle que le jeu),
       réécrire `m_Destination` = le hub ;
     - sinon, laisser la destination vanille (pas de réécriture) ;
   - log par échantillon : achats redirigés (pour debug).
5. `Hub/HubLocale.cs` : nom et description du bâtiment, en/fr.
6. `Mod.cs` : enregistrer les systèmes dans l'ordre correct (`LocalHubPrefabSystem` en `OnGamePreload`,
   puis `LocalHubPlacementSystem`, puis `HubSellerSystem` avant `ResourceBuyerSystem`).

## Finition

- `/simplify` : refactorisation du code.
- `dotnet build` : compilation et vérification d'erreurs.

Les étapes des lots B et C seront détaillées dans leur propre plan, après la recette du lot A.

## Critères de complétion

- Build vert : `dotnet build` sans erreur ou warning.
- Les 3 scénarios de la recette du lot A passent (pose, comparaison, retrait).
- Aucune régression sur la livraison vanilla (mods tiers compatibles, partie charge, pas de
  corruption).

## Sauvegarde

- Lot A, option 2 : la sauvegarde ne référence que la gare vanilla + le composant `LocalHub` (perdu
  sans dégât au retrait). Le nom personnalisé est une chaîne. La réécriture de destination est
  éphémère : elle vit le temps d'un achat.
- Option 1 (repli) : le hub devient une boîte « objet manquant » inerte au retrait (voir plus haut).
- Lot B : véhicules à `PrefabRef` vanilla (moto) : aucun risque.
- Lot C : nos véhicules viennent d'un mod d'asset ; retirer l'asset rend des véhicules « objet
  manquant » (même mécanisme obsolète) : à tester avant publication.

## Dépendances

- **Version du jeu** : 1.6.2f1 (vérifiée dans les faits). Les API de `ResourceBuyerSystem`,
  `TripNeededSystem`, `PrefabSystem` peuvent changer à chaque patch.
- **Mods incompatibles** : *Local Logistics* (150547) et *Dedicated Cargo Facilities* (156605)
  interviennent aussi sur le dispatch des ressources ; risque de conflit ou duplication. À tester
  lors de la recette (scénario 1).
- **Assets de véhicules** : lot B dépend du mod `assets/vehicles/` (plan 006) pour les motos et les
  petits véhicules du lot C.

## Risques

- **[S] Prix et `TradeCost`** : le jeu calcule le prix et le coût de transport à partir du chemin
  trouvé, pas de notre destination (`m_Distance = pathInformation.m_Distance`, `:640`). La distance
  enregistrée sera celle du vendeur d'origine : léger biais économique, à mesurer.
- **[S] Rupture de stock de la gare** : si le train ne suit pas, les commerces retombent sur le
  vanilla (décision 2). Pas de pénurie, mais moins d'effet.
- **[S] Plusieurs hubs dans un district** : on prend le plus proche du commerce, ou le premier.
- **[S] Échange de `PrefabRef` (option 2)** : sous-objets, voies internes et arrêt de train créés à
  la pose d'après le clone ; identiques à l'original, mais à vérifier (scénario 1).
- Conflit avec *Local Logistics* ou *Dedicated Cargo Facilities* s'ils sont installés : ils
  interviennent aussi sur le dispatch.
- Le panneau district compte les véhicules qui roulent ; un commerce proche de la gare fait des
  trajets courts, donc moins de véhicules visibles à un instant donné.

## Recette du lot A (3 scénarios)

1. **Pose** : poser un « Local Hub » dans un district et le relier au rail. Il apparaît, porte le nom
   « Local Hub », reçoit des trains ; le log affiche des achats redirigés et un véhicule de livraison
   suivi (clic) va du commerce au hub.
2. **Comparaison** : panneau district avant / après la pose (camions par km, historique) ; les
   commerces ne signalent pas de manque de marchandises.
3. **Retrait** : sauvegarder, retirer le mod, recharger. La partie charge et le hub est une gare de
   marchandises vanilla qui fonctionne.

## Hors périmètre

- Vente virtuelle sans véhicule (`SaleFlags.Virtual`) : écartée, « pas de magie ».
- Les livraisons aux habitants (les ménages vont acheter eux-mêmes en magasin) et les livraisons
  d'entretien des bâtiments de service (`GoodsDelivery`).
- Un modèle 3D propre au hub (le clone reprend celui de la gare vanilla).
- La politique « Livraison par le hub » sur une gare existante (écartée par l'humain).
- Les rues interdites aux camions : jalon 5, « Quartier sans voiture ».

## Ajustements après revue game-designer (05/10/2026)

- **Repli décidé par l'humain** : si l'option 2 (clone pour la pose → gare vanilla + `LocalHub`) échoue
  en jeu, on passe à une **politique de district posée sur une gare existante**, jamais à un bâtiment
  orphelin.
- **Bloquant corrigé — stock réservé** : rediriger vers le hub seulement si
  `stock − VehicleUtils.GetAllBuyingResourcesTrucks(hub, ressource) ≥ max(besoin / 2, 1)`, comme le
  pathfinder (`ResourcePathfindSetup.cs:~160-170`) et `ProcessResourceBuyer`
  (`ResourceBuyerSystem.cs:597-615`) ; sinon le vendeur vanilla est gardé (sinon l'achat est perdu,
  `CompanyInsufficientStock`). Le commerce doit avoir un `PropertyRenter` valide, la destination doit
  passer `m_Properties.HasComponent` ; exclure `OutsideConnection`.
- **Réécrire aussi `PathInformation.m_Distance`** (et `m_TotalCost` s'il sert) vers le hub : sinon
  `SalesEvent.m_Distance` garde le trajet vers l'ancien vendeur (`ResourceBuyerSystem.cs:~640`).
- **Ressources du hub** : une gare ne stocke que ses `m_TradedResources`
  (`CargoTransportStation.cs:94-101`), plafond `StorageLimit / 2 / N` par ressource
  (`StorageCompanySystem.cs:501`). Le clone liste les ressources achetées par les commerces
  (à relever dans les `IndustrialProcessData` des commerces) ; `StorageLimit` doublé si N > 10.
  Un hub de quartier couvre une partie des ressources : la description le dit au joueur.
- **Lots B/C — quantité par voyage** : aligner `m_AmountNeeded` sur la capacité du petit véhicule
  (le commerce rachète plus souvent), pas de vente supérieure à la livraison. Capacités de départ
  (estimations à confronter aux camions vanilla relevés en jeu) : vélo-cargo 500, remorque palette
  1000, triporteur 2000, kei truck 4000. Seuil de commande vanilla 2000
  (`BuyingCompanySystem.cs:330`) → triporteur par défaut, vélos pour les petites ressources.
- **Rayon** : district du hub **et** distance max pour les petits véhicules (départ 600 m, réglable),
  sans limite pour les camions du lot A. **Décidé par l'humain (05/10/2026).**
- **Recette** : surveiller la file de véhicules à l'entrée du hub (une seule entrée de parking).

## Écarts après audit du 05/10

- `SetDefaults()` appelé dans le constructeur de `ModSettings` (sinon rayon = 0 à l'installation, aucun petit véhicule). Local Hub : clone + `AddPrefab` vérifié, icône `DeliveryVan.svg`.

## Lot C — nos véhicules (codé à l'aveugle le 05/10/2026, non recetté)

- `Hub/HubVehicleModels.cs` : retrouve nos 4 modèles importés **par nom de prefab** (un `PrefabID`
  construit depuis type + nom ne marche pas : son égalité compare aussi le hash d'asset,
  `PrefabID.cs:45-49`), fixe leurs données (capacité 500 / 1000 / 2000 / 4000, électrique, `Small`,
  vitesse 20 / 18 / 45 / 60 km/h, pollution de l'air nulle, bruit et usure faibles), transforme le
  siège copié de la camionnette en siège **Biking** (vélos) ou **Driving** (triporteur, kei truck)
  à l'ancre de chaque `build.py`, et les **verrouille** (`Locked`) pour que les livraisons vanilla ne
  les choisissent jamais (`VehicleCapacitySystem` exclut `Locked` ; sa liste est reconstruite après
  la désérialisation, `PostDeserialize` → `m_RequireUpdate`, donc après notre passage en fin de
  chargement).
- Choix : le plus petit modèle qui prend toute la quantité, sinon le plus grand ; quantité vendue
  plafonnée à sa capacité. Repli : plus petit véhicule vanilla (lot B) si nos modèles ne sont pas
  chargés. Log au chargement : `Local Hub: N own delivery vehicles loaded`.
- Dépendance au pack d'assets **non déclarée** (`PublishConfiguration.xml` intact, décision
  structurelle à venir) : détection au runtime.
- [S] à vérifier en jeu : pose du cycliste (siège Biking sur un véhicule de livraison) ; position des
  sièges ; un prefab `Locked` reste utilisable par un véhicule déjà créé ; données de prefab non
  écrasées par la sauvegarde ; nos modèles réimportés gardent leur nom exact.

## Lot D — vélo-cargo hybride (décision 021, codé à l'aveugle le 05/10/2026, non recetté)

### Ce que tu verras en jeu

- Les deux vélos-cargo (`SolarpunkCargoBikeBox01`, `SolarpunkCargoBikePallet01`) partent **du hub,
  chargés**, et livrent le commerce : le hub livre, en aller simple.
- Ils roulent comme des vélos : pistes cyclables, rues mixtes, allées où le vélo est permis ; une
  politique de district qui interdit les vélos est respectée. Pas sur les trottoirs de route (le jeu
  ne les rend pas cyclables).
- Après la livraison, le vélo disparaît (comme rentré au hub). Si aucun chemin vélo n'existe vers le
  commerce, il repasse en mode route et finit sa livraison comme une camionnette.
- Triporteur et kei truck : inchangés (le commerce vient chercher au hub).

### Mécanique (vérifiée dans le décompilé 1.6.2f1)

- Un vélo du jeu = une `Car` + le composant vide `Bicycle` (`Game.Vehicles/Bicycle.cs`) ; la
  navigation (`CarNavigationSystem`) et l'apparition (`Game.Vehicles/InitializeSystem.cs:265-280,
  :907`) lisent ce composant. `BicycleData` (prefab) ne sert qu'à la sélection des voitures
  personnelles (`PersonalCarSelectData.cs:47`) : rien à ajouter à nos prefabs.
- `DeliveryTruckAISystem.FindPathIfNeeded` code en dur un chemin route (`:1104-1132`, Burst) mais ne
  demande un chemin que si `RequireNewPath` (`VehicleUtils.cs:199-206`) : un système à nous, juste
  avant lui, pose un chemin vélo (`SetupPathfind` met `Pending`, `:220-231`) et l'IA saute le sien.
- Livraison : `DeliverCargo` ajoute la marchandise à la destination ; avec `Owner` = commerce, le
  paiement se compense (+prix / −prix, `:395-403`) : le commerce a déjà payé à la vente.
- Après livraison, l'IA met `Returning` (`:793`) : on supprime alors le vélo.

### Étapes

1. `HubVehicleModels` : repérer les modèles « vélo » (siège `Biking`).
2. `HubDeliveryVehicles.Spawn` : pour un vélo, départ du hub (`TripSource` = hub, transform du hub),
   `Loaded | Delivering`, `Target` = commerce, `Owner` = commerce, `Bicycle` ajouté dans le même ECB.
3. `HubSaleSystem` : pour un vélo, la marchandise quitte le stock du hub à la vente.
4. `Hub/CargoBikeSystem.cs`, avant `DeliveryTruckAISystem` (GameSimulation et LoadSimulation comme
   lui, `SystemOrder.cs:333, 620`) : chemin vélo si besoin ; chemin `Failed` → retrait de `Bicycle` et
   `Obsolete` (repli route) ; `Returning` → suppression.
5. Build vert.

### Risques

- [S] Un commerce sans point d'accès piéton/vélo : repli route.
- [S] Le chemin route que l'IA demande en même temps que `Returning` est demandé pour rien (vélo
  supprimé avant de l'utiliser).
- [S] Pose du cycliste correcte sur un véhicule de livraison.

### Recette (2 scénarios, session 5)

1. Hub + vélos-cargo importés, commerces à moins de 600 m reliés par pistes cyclables ou rues mixtes :
   des vélos-cargo sortent **du hub**, chargés, roulent sur les pistes et disparaissent au commerce ;
   le commerce ne signale pas de manque.
2. District avec la politique qui interdit les vélos, ou commerce sans accès vélo : le vélo passe en
   mode route (ou un triporteur part), aucune livraison perdue.

## Lot E — choix par distance et découpage (05/10/2026, codé, non recetté)

Décision de l'humain : « le jeu doit pouvoir faire de plus petites commandes ; par distance ; 2 000,
ça peut être 4 vélos-cargo ; les magasins commandent moins mais plus souvent ».

**Ce que tu verras en jeu**
- Commerces à **moins de 300 m** du hub (réglage « Portée des vélos-cargo ») : le hub envoie des
  **vélos-cargo**, plusieurs par commande (au plus 4, réglage « Vélos-cargo par commande ») : vélo à
  caisse 500, vélo + remorque 1 000, **vélo-cargo couvert 1 500** (5ᵉ modèle,
  `SolarpunkCoveredCargoBike01`, siège à vélo).
- Entre 300 et 600 m : le commerce envoie **un** utilitaire (triporteur 2 000, kei truck 4 000), ou le
  plus petit véhicule du jeu si nos modèles manquent.
- Au-delà, ou hub sans stock : comportement du jeu.

**Mécanique (vérifiée dans le décompilé)**
- Le jeu ne commande jamais moins de 2 000 (`BuyingCompanySystem.cs:330`, `:182-204`) : on ne change
  pas la commande, on **ne vend que ce que les vélos portent**. Le reste n'est pas vendu : le commerce
  garde son manque, et au passage suivant `CalculateResourceNeeded` (`:215-245`) relance une commande →
  « moins mais plus souvent ».
- Les vélos portent `Loaded | Delivering | Buying` : `Buying` fait compter leur charge comme « en
  route » (`VehicleUtils.GetBuyingTrucksLoad`, `:732-763`), donc pas de double commande ; le drapeau ne
  change rien d'autre pour un camion chargé (seul lecteur de l'IA : `DeliveryTruckAISystem.cs:1053`,
  qui le pose ; l'interface l'affiche).
- Une ligne `CurrentTrading` par vélo, à son montant : l'IA retire la ligne dont le montant égale la
  livraison (`DeliveryTruckAISystem.cs:322-357`).
- Le stock du hub est retiré à la vente (les vélos partent chargés) : les ventes suivantes du même
  passage voient le stock à jour.

**Correctifs de revue**
- `IsSmallVehicle` ne reconnaît plus que **nos** modèles. Le plus petit véhicule du jeu (repli quand
  nos modèles manquent) sert aussi aux livraisons ordinaires : il n'est pas distingué (assumé).
- `CargoBikeSystem` ne regarde que la tranche `UpdateFrame` de la frame (comme `DeliveryTruckAISystem`,
  `:1327-1330`), toujours juste avant lui : mêmes camions, plus de synchronisation inutile.
- `Bicycle` est sauvegardé (`IEmptySerializable`) : sans le mod, un vélo en route au moment de la
  sauvegarde roule comme un vélo mais reçoit des demandes de chemin routier — il peut recalculer en
  boucle jusqu'à abandon. La partie charge.
- Verrou `Locked` posé après la construction de la liste de livraison du jeu : on redemande sa
  reconstruction (`VehicleCapacitySystem.PostDeserialize`, qui ne fait que lever `m_RequireUpdate`).
- **Nom de prefab du petit hub** : `SolarpunkLocalHub` désigne soit le clone de la mini gare Tigon
  avec notre modèle (plan 010), soit, sans Tigon ou sans notre pack, le clone du terminal vanilla.
  Choix assumé pour que les sauvegardes restent valides ; mais une partie sauvegardée avec l'un et
  rechargée avec l'autre garde un lot et des sous-réseaux incohérents (pas de plantage identifié).

**Recette**
1. Hub + nos 5 véhicules : un commerce à < 300 m reçoit plusieurs vélos-cargo pour une commande
   (jusqu'à 4), son stock remonte, il recommande ensuite plus souvent ; un commerce à 300-600 m reçoit
   un triporteur ou un kei truck. Log : `Local Hub: 5 own delivery vehicles loaded (3 cargo bikes)`.
2. Options : portée vélos à 100 m, vélos par commande à 1 → moins de vélos, plus de triporteurs ; aucun
   commerce en manque durable, aucun double achat (pas de vélos qui arrivent en double pour le même
   besoin).
