---
status: in-progress
created: 2026-10-05
updated: 2026-10-05
---

# Plan 010 — Local Hub : notre modèle 3D et ses variantes (train)

## Ce que tu verras en jeu (V1)

- Le menu Transport propose « Local Hub » avec **notre bâtiment** (halle bois, toit végétalisé et
  solaire, 4×5 cellules) au lieu du grand terminal de fret.
- Une voie ferrée traverse l'arrière du lot, sous l'auvent du quai ; on la raccorde au réseau des deux
  côtés. Une ligne de train cargo s'y arrête et remplit le stock du hub.
- Les commerces du district s'approvisionnent au hub comme aujourd'hui (plan 007), motos depuis la rue.
- Sans le pack d'assets (ou sans Tigon, voir décision 1), « Local Hub » reste disponible avec l'ancien
  modèle (grand terminal) : rien ne casse, le log dit pourquoi.

## Objectif

Remplacer l'apparence du Local Hub (clone de `CargoTrainTerminal01`, 38×17, plan 007) par notre modèle
`SolarpunkLocalHub01`, puis décliner : **V1** petit / surface, **V2** petit / souterrain, **V3** grand
(bâtiment urbain pour grand district). Train seulement.

## Contexte

- Plan 007 (lots A+B codés à l'aveugle) : `Hub/LocalHubBuildingSystem.cs` clone `CargoTrainTerminal01`
  sous le nom `SolarpunkLocalHub` ; `HubSaleSystem` reconnaît un hub **par son prefab**.
- Modèle : `assets/buildings/local-hub/` (script `build.py`), importé et visible en jeu le 05/10
  (préréglage Building). Lot **4×5 cellules (32×40 m)**, rue côté −Y Blender (= **+Z jeu**, avant du
  lot), axe de voie prévu à Blender y = +14 (= **z = −14 jeu**), quai à hauteur de wagon (1,10 m).
- Décisions humain déjà prises : pack d'assets **séparé**, déclaré en dépendance ; règle de sauvegarde
  assouplie (un bâtiment du mod peut devenir « objet manquant ») ; tram/métro **plus tard**.
- Base de connaissances lue : `docs/references/base-connaissances/` (README, `code-csharp-ecs.md`
  §5 prefabs créés par code, `assets-3d.md`, `jeu-mecaniques-utiles.md`) ; plugin
  `TECH/prefabs-and-assets/prefabs-and-assets.md` § *Cloning* (lignes 374-409).

## Faits vérifiés

- **Mesh d'un bâtiment** : `ObjectGeometryPrefab.m_Meshes : ObjectMeshInfo[]`
  (`DEC/Game.Prefabs/ObjectGeometryPrefab.cs:11`), chaque entrée = `RenderPrefabBase m_Mesh`,
  `m_Position`, `m_Rotation`, `m_RequireState` (`ObjectMeshInfo.cs:12-20`). `BuildingPrefab`
  hérite de `StaticObjectPrefab` (`BuildingPrefab.cs:19`). [V]
- **Lot** : `BuildingPrefab.m_LotWidth` / `m_LotDepth` (défaut 4, `BuildingPrefab.cs:24-27`),
  `m_AccessType` (`:21`). [V]
- **Clonage** (plugin § *Cloning*) : `Clone(nom)` copie tous les composants (objets neufs), pas
  l'asset → identité = type + nom ; **retirer `ObsoleteIdentifiers`** ; pour un objet « différent »
  plutôt qu'une variante, le plugin conseille de **dériver sélectivement** et de **partager le tableau
  de meshes par référence**. Ne jamais assigner `component.prefab` sur la source. [V]
- **Retrouver notre asset** : il s'appelle comme son dossier → `prefabSystem.TryGetPrefab(new
  PrefabID(nameof(BuildingPrefab), "SolarpunkLocalHub01"), out var asset)` (`PrefabID(string, string)`
  confirmé, recherche import du 05/10). Ses meshes = `((BuildingPrefab)asset).m_Meshes`. [V code, S que
  l'asset sauvegardé localement par l'éditeur soit chargé en partie : sinon le **packager** (`.cok`)]
- **Gabarit de fret compact** : Tigon `Mini Train Oil Station` (dézippé, scratchpad `tigon/`) =
  `BuildingPrefab` **5×5**, composants `CargoTransportStation`, `StorageLimit`, `CompanyObject`,
  `CityServiceBuilding`, `ServiceObject`, `Workplace`, `PlaceableObject`, `UIObject`, `Unlockable`,
  `UnlockOnBuild`, `ObjectSubObjects` (30 sous-objets : arrêts, repères, décor), `ObjectSubAreas`,
  `ObjectSubNets` (2 : une **voie de train droite à z = −16**, de x = −24 à +24, donc 4 m hors lot de
  chaque côté pour se raccorder ; un **accès route** du bord avant z = +20 vers z = +5,3 à x = −11,9),
  `BuildingTerraformOverride`, `AssetPackItem`, `ObsoleteIdentifiers`. Convention : **avant du lot = +Z**,
  comme notre modèle. Les GUID des sous-objets pointent vers des prefabs vanilla (non résolubles hors
  jeu). [V fichier]
- **Souterrain** : Tigon `TO_UndergroundRailStation01` (3×2) = voie à **y = −20** sur 200 m
  (x −100 → +100, net `f0ddda…`, différent de la voie de surface `5421f…` : variante tunnel) + une rampe
  piétonne vers −6,3 m (net `934e…`) + `ObjectSubLanes`, `NetPieceRequirements`. [V fichier]
- **Pas de petite gare de fret vanilla** : seul `CargoTrainTerminal01` (38×17) existe côté train [S fort,
  aucune autre gare cargo train trouvée dans les locales le 05/10].

## Décisions

1. **Gabarit = Tigon `Mini Train Oil Station` si chargé** (Tigon est dans la playset, décision 010), sinon
   repli sur le hub actuel (clone `CargoTrainTerminal01`). *À valider par l'humain* : ça rend Tigon
   nécessaire pour avoir **notre** modèle (sans Tigon, le hub marche avec l'ancien modèle).
2. **Même nom de prefab** `SolarpunkLocalHub` quel que soit le chemin (modèle à nous ou repli) : les
   sauvegardes restent valides si le pack ou Tigon manquent. Variantes = noms propres
   (`SolarpunkLocalHubUnderground`, `SolarpunkLocalHubLarge`).
3. **Mesh** : `hub.m_Meshes = asset.m_Meshes` (partage par référence, comme le conseille le plugin) ;
   lot 4×5 ; la voie du gabarit est **remplacée** par la nôtre à z = −14 (x −20 → +20, 4 m hors lot) ;
   accès route conservé mais recentré sur notre zone de chargement côté rue.
4. **Sous-objets** : ne garder que les **fonctionnels** (prefabs `TransportStopPrefab` — l'arrêt de train
   cargo —, repères de spawn / stationnement), retirer le **décor** (citernes, props) ; repositionner
   l'arrêt cargo le long de notre voie. Liste exacte établie **par log au premier chargement** (étape 2).
5. Retirer `AssetPackItem` et `ObsoleteIdentifiers` du clone (appartenance au pack Tigon, ids Tigon).

## Lots

- **V1** — Petit / surface / train : notre modèle sur le gabarit Tigon 5×5 recomposé (voie z = −14).
- **V2** — Petit / souterrain / train : même bâtiment en surface, voie tunnel à y = −20 (net tunnel pris
  sur `TO_UndergroundRailStation01`), arrêt cargo à −20 ; les petits véhicules restent en surface.
- **V3** — Grand / surface / train : nouveau modèle Blender (bâtiment urbain ~8×8, rez-de-chaussée
  logistique, étages), deux voies ; même composition que V1.

### Étapes de V1

1. **`Hub/LocalHubBuildingSystem.cs`** — `OnGamePreload` :
   1. Chercher l'asset `SolarpunkLocalHub01` (`BuildingPrefab`) et le gabarit `Mini Train Oil Station`
      (`BuildingPrefab`). Si l'un manque → chemin actuel (clone `CargoTrainTerminal01`) + log
      `Local Hub: using the cargo terminal model (missing: …)`.
   2. Sinon `hub = template.Clone(PrefabName)` ; `Remove<ObsoleteIdentifiers>()`,
      `Remove<AssetPackItem>()` ; `hub.m_Meshes = asset.m_Meshes` ; `m_LotWidth = 4`,
      `m_LotDepth = 5` ; icône et nom propres (`UIObject`, comme aujourd'hui, `m_Group` conservé =
      onglet fret de Tigon/vanilla — vérifier, sinon le groupe du terminal vanilla).
   3. `ObjectSubNets` : garder l'entrée dont le `m_NetPrefab` porte `TrackData` de type Train (ne pas se
      fier au GUID) ; lui donner une courbe droite `a = (-20, 0, -14)`, `d = (20, 0, -14)`, `b`/`c` aux
      tiers ; `m_NodeIndex` inchangé. Garder l'accès route, `x` ramené dans le lot (|x| ≤ 12).
   4. `ObjectSubObjects` : **d'abord logguer** chaque sous-objet du gabarit (nom du prefab, type,
      position) ; garder ceux de type `TransportStopPrefab` et les repères (`SpawnLocation` / marqueurs),
      décaler les arrêts de Δz = +2 (voie −16 → −14) ; retirer les `StaticObjectPrefab` de décor.
   5. `ObjectSubAreas` : vider (surfaces du gabarit, sans rapport avec notre lot).
   6. `AddPrefab` avec retour vérifié (comme aujourd'hui).
2. **Diagnostic** : log unique au pré-chargement « Local Hub built from <gabarit> + <asset>: kept N sub-objects
   (liste), track at z=-14 » pour la recette.
3. **`HubSaleSystem`** : inchangé (reconnaît le prefab `SolarpunkLocalHub`).
4. **Pack d'assets** : `Properties/PublishConfiguration.xml` → **ne pas** déclarer la dépendance tant que
   le pack n'est pas publié (structurel : à faire avec l'humain au moment de la publication).
5. `/simplify`, `dotnet build` vert.

### Étapes de V2 (après V1 recetté, ou à l'aveugle si l'humain le veut)

1. Même construction, nom `SolarpunkLocalHubUnderground`, gabarit de voie : la sub-net à y = −20 de
   `TO_UndergroundRailStation01` (son `m_NetPrefab` tunnel), courbe x −40 → +40 à z = −14, y = −20 ;
   arrêt cargo à y = −20 ; recopier `NetPieceRequirements` si présent sur le gabarit souterrain.
2. Pas de rampe piétonne (le fret n'en a pas besoin) ; vérifier en jeu l'accès des trains au tunnel.

### Étapes de V3

1. Modèle Blender `SolarpunkLocalHubLarge01` (assets/buildings/local-hub-large/, même kit, ~8×8, deux
   voies à z = −24 et −30), import en jeu par l'humain.
2. Même construction que V1, deux sub-nets de voie, deux arrêts cargo, `StorageLimit` ×2.

## Sauvegarde

Rien de nouveau n'est sérialisé ; le prefab garde le nom `SolarpunkLocalHub`. Hub posé + mod retiré →
« objet manquant » (règle assouplie). Pack retiré, mod présent → repli sur l'ancien modèle, même nom :
la partie charge.

## Risques

- [S] L'asset sauvegardé localement par l'éditeur n'est pas chargé en partie → **packager** l'asset
  (`.cok`) ou le publier ; sinon V1 tombe sur le repli (le log le dit).
- [S] Sous-objets du gabarit : l'arrêt cargo et les repères attendus ; si le tri par type écarte un objet
  utile, plus de chargement de train → la liste logguée permet de corriger.
- [S] Accès route du gabarit qui traverse notre façade : visuel seulement, à recentrer.
- Le modèle n'a pas de pistes de chargement dessinées pour les camions : les véhicules se garent sur
  l'accès route du gabarit.
- Tigon renomme `Mini Train Oil Station` → repli automatique (log).

## Recette (V1, 2 scénarios ; V2 +1)

1. **Pose** : « Local Hub » montre notre bâtiment ; raccorder la voie des deux côtés ; log « built from
   Mini Train Oil Station + SolarpunkLocalHub01 » avec la liste des sous-objets gardés. Une ligne cargo
   s'arrête au quai, le stock monte.
2. **Ventes et motos** : comme la session 5 du cahier (plan 007), avec notre modèle ; repli : désactiver le
   pack → l'ancien modèle revient, la partie charge.
3. (V2) **Souterrain** : la voie est en tunnel, raccordable à une voie souterraine ; ligne cargo, stock.

## Hors périmètre

- **Tram et métro** (fret) : après le fret tram/métro (jalon 4).
- Roues/animations, détails du modèle (itération « modèles plus propres »).
- Publication du pack d'assets et déclaration de la dépendance (avec l'humain).

## Décisions pour l'humain

1. D'accord pour que **notre modèle de hub demande Tigon** (gabarit de petite gare de fret) ? Sans Tigon,
   le hub marche avec l'ancien modèle.
2. Pour tester ce soir, l'asset doit sans doute être **packagé** dans l'éditeur (bouton Package), pas
   seulement sauvegardé. OK pour l'ajouter à la session 0 ?

## Écarts au code (05/10/2026, V1 + V2 + V3 codés à l'aveugle, build vert, non recettés)

- Code : `Hub/LocalHubBuildingSystem.cs` (3 variantes), `Hub/LocalHubLocale.cs` (3 noms),
  `Hub/HubSaleSystem.cs` (reconnaît les 3 prefabs via `IsHub`).
- **V3 = grand hub urbain SOUTERRAIN** (décision humain : immeuble mitoyen 4×4 aligné sur la rue,
  quai à −20 m), pas « deux voies en surface » : voie tunnel selon X à z = +5 (Blender y ≈ −5),
  une seule voie, stockage ×2.
- V2 : lot 3×4, voie tunnel à z = 0, y = −20, ±40 m. V1 : lot 4×5, voie surface à z = −14, ±20 m.
- Assets retrouvés **par nom de prefab** parmi les `BuildingData` chargés (PrefabID compare aussi le
  hash d'asset). Sans Tigon ou sans pack : V1 retombe sur le clone du terminal vanilla (même nom),
  V2/V3 ne sont pas créés ; le log dit ce qui manque.
- Tigon `Mini Train Oil Station` n'échange que 2 ressources (pétrole) : le hub reprend les
  `m_TradedResources` et le `storageLimit` de `CargoTrainTerminal01` (×2 pour V3).
- Sous-objets gardés : arrêts (`TransportStop`) décalés sur la nouvelle voie (et à −20 m en souterrain)
  + marqueurs (`MarkerObjectPrefab`) ; décor retiré ; `ObjectSubAreas`, `BuildingTerraformOverride`,
  `AssetPackItem`, `ObsoleteIdentifiers` retirés. Liste loggée au pré-chargement.
- [S] : arrêt cargo à −20 m raccordé à la voie tunnel ; accès route du gabarit qui croise notre façade ;
  marqueurs de stationnement hors de notre lot plus étroit ; `m_Upgrades` (pièces de réseau) de la voie
  tunnel recopiés tels quels ; un prefab de modèle importé « Building » chargé en partie (sinon
  packager).

### Recette regroupée (à reporter dans le cahier, session 5)

1. **Log au chargement** : 3 lignes « Local Hub: … built from Mini Train Oil Station + … » (ou « not
   built, missing … »), avec les sous-objets gardés. Menu Transport : « Local Hub », « Local Hub
   souterrain », « Local Hub urbain », chacun avec notre modèle.
2. **Pose et train** : poser chaque variante, raccorder la voie (surface pour V1, tunnel pour V2/V3) ;
   une ligne cargo s'arrête au quai, le stock monte (au clic). V3 se pose entre deux immeubles.
3. **Ventes** : comme la session 5 (commerces du district, petits véhicules), pour au moins une variante.
