# Code C# / ECS pour CS2 — notre style et les patterns sûrs

> 05/10/2026, jeu 1.6.2f1, plugin cs2-modding 1.2.0. Abréviations : voir `README.md`.
> [V] vérifié (source citée) · [S] supposé.

## 1. Style du dépôt

- **Nommage** : on suit le **style du jeu**, pas celui de Microsoft. Champs `m_PascalCase`
  (`m_PrefabSystem`), constantes `PascalCase` (`GracePeriodHours`), composants `struct` publics à
  champs `m_` (`StreetParkingBan.m_SinceFrame`), systèmes `public partial class XxxSystem : GameSystemBase`.
  Microsoft recommande `_camelCase` pour les champs privés et `s_` pour les statiques
  ([V] https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names) ;
  on s'en écarte volontairement pour que notre code se lise comme le décompilé qu'il imite. Ce qui
  reste commun : PascalCase types/méthodes, `I` des interfaces, **pas d'abréviation**
  (CLAUDE.md), noms qui disent l'intention.
- **`partial`** sur tout système : les générateurs Entities ajoutent du code (`TypeHandle`,
  `OnCreateForCompiler`) [V `TECH/ecs-in-this-game/ecs-in-this-game.md` § type handles].
- **Une classe = une responsabilité, un dossier = un module** (`Hub/`, `Station/`, `Rail/`,
  `Policies/`, `Info/`) ; textes dans un `XxxLocale.cs` statique par module (`Policies/PolicyLocale.cs`).
- **Commentaire `<summary>`** sur chaque système : ce qu'il fait, **quand** il tourne, et ce qu'il
  laisse dans la sauvegarde (modèle : `Station/StationPlatformSystem.cs`, `Hub/LocalHubBuildingSystem.cs`).
- **Log** : un seul logger, `Mod.Log`, créé avec `.SetShowsErrorsInUI(false)` (`Mod.cs`). Sans ça,
  tout `Error` ouvre une boîte de dialogue modale et **met la simulation en pause** [V
  `TECH/diagnostics/diagnostics.md` § *An Error from a logger*]. `UnityEngine.Debug.LogError` ne
  peut pas être rendu silencieux : ne jamais l'utiliser [V même §].

## 2. Point d'entrée `Mod.cs`

- `IMod` = `OnLoad(UpdateSystem)` + `OnDispose()`. La classe `Mod` est **allouée sans
  constructeur** : ses initialiseurs de champs d'instance ne tournent pas (les statiques oui) [V
  `PLUGIN/cs2-modding/SKILL.md`]. D'où `static` pour `Log`, `Settings`.
- Une exception dans `OnLoad` ou dans l'`OnCreate` d'un système **tue le mod en silence**, trace
  seulement dans `Logs/Modding.log` [V même source].
- `OnDispose` est aussi appelé quand `OnLoad` a planté à mi-chemin → **chaque champ peut être
  null** (`Settings?.UnregisterInOptionsUI()`) [V `TECH/mod-lifecycle-and-ordering/mod-lifecycle-and-ordering.md` § *OnDispose hygiene*].
- Les prefabs du jeu **ne sont pas encore chargés** pendant `OnLoad` : on ne peut pas y chercher un
  modèle à cloner [V `TECH/prefabs-and-assets/prefabs-and-assets.md` § *When you may do it*].

## 3. Où et quand un système tourne

- Ordre **impératif** : `updateSystem.UpdateAt<T>(phase)`, `UpdateBefore<T, Ancre>(phase)`,
  `UpdateAfter<T, Ancre>(phase)`. Les attributs `[UpdateInGroup]`, `[UpdateBefore]` compilent et
  **ne font rien** [V `PLUGIN/cs2-modding/SKILL.md`]. Quand tourne un système vanilla : un grep dans
  `DEC/Game.Common/SystemOrder.cs` (ex. `ModifiedSystem` en `Modification4`, `:146` ;
  `ResourceBuyerSystem` en `GameSimulation`, `:395`) [V].
- **Système sans phase** (créé par `World.GetOrCreateSystemManaged<T>()` dans `OnLoad`, `OnUpdate`
  vide) : le bon support des **hooks de chargement** (`OnGamePreload`, `OnGameLoadingComplete`)
  [V `TECH/mod-lifecycle-and-ordering/...` § *Not every mod system needs a phase*]. Exemples chez
  nous : `EndResidentialParkingPolicySystem`, `LocalHubBuildingSystem`, `StationPlatformSystem`,
  `NonElectricRailCategorySystem`, `ElectricTrainsOnlySystem`.
- **Section du panneau sélectionné** : pas de phase non plus, `SelectedInfoUISystem.AddMiddleSection`
  la pompe (`Mod.cs`, `DEC/Game.UI.InGame/SelectedInfoUISystem.cs:207`) [V].
- **Intervalle** `GetUpdateInterval` : **puissance de 2** sinon le mod entier échoue
  (`DEC/Game/UpdateSystem.cs:303-306`) ; lu **une fois** à l'enregistrement ; **ignoré hors des
  phases de simulation** (UIUpdate, Modification…) [V `TECH/mod-lifecycle-and-ordering/...` § *The
  update interval*]. Un jour de jeu = 262 144 frames (`TimeSystem.kTicksPerDay`).
- **Ancrage et décalage** : un système ancré (`UpdateBefore/After`) sur un système de **même
  intervalle** et sans décalage explicite **hérite du décalage de l'ancre**
  (`DEC/Game/UpdateSystem.cs:406-409`) [V 05/10]. C'est ce qui garantit que `HubSaleSystem`
  (intervalle 16, avant `ResourceBuyerSystem`, intervalle 16 `ResourceBuyerSystem.cs:1179-1181`)
  voit chaque passe du jeu. Sans ancrage, ne **jamais** supposer sur quelle frame on tombe.
- **Garde de pertinence** : `RequireForUpdate(query)` (plusieurs appels = ET logique) saute
  `OnUpdate` sans coût si la requête est vide [V `TECH/performance-and-memory/...` § *Throttling*].
  Pour l'UI : intervalle + sortie immédiate si le panneau est fermé (modèle :
  `Info/SolarpunkInfoUISystem.cs`, `IsPanelOpen` + `UIUpdateState`).

## 4. Hooks de chargement

| Hook | Usage chez nous | Si exception |
|---|---|---|
| `OnGamePreload(Purpose, GameMode)` | **enregistrer nos prefabs** (après la base d'assets, avant la lecture de la sauvegarde) | système désactivé pour la session |
| `OnGameLoaded(Context)` | (non utilisé) après tout `Deserialize` | système désactivé |
| `OnGameLoadingComplete(Purpose, GameMode)` | **ajuster des données de prefab** quand la ville est prête (`ElectricTrainsOnlySystem`, `NonElectricRailCategorySystem`, `HubSaleSystem` → `Refresh`) | loggué, le système **continue** |

[V `TECH/mod-lifecycle-and-ordering/...` § *When a lifecycle hook throws* ; `DEC/Game/GameSystemBase.cs`]

Chaque hook **se répète** à chaque partie ouverte dans la session : toujours idempotent. Notre
garde : un champ (`if (m_Prefab != null) return;`). Le plugin recommande aussi un `TryGetPrefab`
sur l'identifiant qu'on va créer [V `prefabs-and-assets.md` § *When you may do it*].

## 5. Prefabs créés par code

- **Cloner** : `PrefabSystem.DuplicatePrefab(template, nom)` = `Clone` + `Remove<ObsoleteIdentifiers>()`
  + `AddPrefab` (`LocalHubBuildingSystem`). À la main (`StationPlatformSystem.AddPlatform`) : ne pas
  oublier `Remove<ObsoleteIdentifiers>()`, sinon une vieille sauvegarde peut résoudre l'original
  vers notre clone [V `prefabs-and-assets.md` § *Cloning*].
- **Nom neuf obligatoire** (le clone n'a pas d'asset, son identité = type + nom) ; préfixe
  `Solarpunk`, sans `_`.
- **`UIObject.m_Group = null`** sur un clone qui ne doit pas rejoindre l'onglet de l'original
  (`StationPlatformSystem` le fait ; `LocalHubBuildingSystem` garde volontairement le groupe de la
  gare fret) [V même §].
- **`AddPrefab` renvoie `bool` et avale toutes les exceptions** : tester et logger (fait partout).
  Doublon d'identifiant : log d'avertissement, index sauté, **renvoie quand même `true`** [V
  `prefabs-and-assets.md` § *Prefab identity*].
- **Nouveau prefab de zéro** : `ScriptableObject.CreateInstance<T>()` + composants
  (`EndResidentialParkingPolicySystem`, `NonElectricRailCategorySystem`). Dériver de la classe
  vanilla la plus proche.
- **Modifier un prefab vanilla** : l'effet sur les bâtiments **posés** dépend du champ — lu à chaque
  passe (immédiat, cas fréquent), copié à la création (périmé jusqu'à reconstruction), ou archétype
  (jamais) [V `prefabs-and-assets.md` § *Editing a prefab*]. Lire les systèmes qui touchent le
  champ avant de conclure.
- **Données de prefab = reconstruites à chaque lancement** : ce qu'on y écrit ne va pas dans la
  sauvegarde. Si l'effet est réversible en cours de partie (option), mémoriser l'original et le
  restaurer (`ElectricTrainsOnlySystem.Restore`).
- `Locked` des prefabs **est** sauvegardé (`BeginPrefabSerializationSystem.cs:83`) : ne jamais
  verrouiller un prefab vanilla (plan 003) [V 05/10].

## 6. Corps d'un système

- **Requêtes** : toujours `Exclude<Deleted>()` et `Exclude<Temp>()` (`Temp` = aperçu de l'outil,
  `Deleted` = entité mourante pendant une frame) [V `PLUGIN/cs2-modding/SKILL.md`]. Notre aide :
  `LiveQuery(...)` (`SolarpunkInfoUISystem`, `SolarpunkDistrictSection`).
- **Lookups** (`ComponentLookup`, `BufferLookup`) : **acquis dans `OnCreate`**, rafraîchis par
  `.Update(this)` en tête d'usage (modèle : `HubSaleSystem`, `ImpoundSystem`). Le premier
  `GetComponentLookup` d'un type **complète les jobs** du système : dans `OnUpdate` il coûte un
  arrêt à la première frame [V `TECH/performance-and-memory/...` § *Taking a lookup mid-frame*].
- **Lecture sur le thread principal** pendant que des jobs de simulation écrivent : les gardes de
  sécurité sont **compilées hors** du jeu → aucune erreur, juste une course silencieuse. D'où
  `CompleteDependency()` avant de lire (`HubSaleSystem`, `ImpoundSystem.TowCars`,
  `SolarpunkDistrictSection.OnProcess`) [V même référence].
  **Coût** : attend tous les jobs en vol sur les types enregistrés par le système, amont compris =
  potentiellement une tranche de la frame de simulation. Acceptable à intervalle 16+ ou quand le
  panneau est ouvert ; **à éviter à chaque frame** [V même référence § dependency graph].
- **`EntityManager.Add/RemoveComponent`, `CreateEntity`** = changement structurel = point de
  synchronisation qui vide tous les jobs. `SetComponentData` complète les jobs de ce type [V
  `performance-and-memory.md` § structural change].
- **Command buffers** : ceux du jeu sont des **barrières nommées**. Règles [V
  `TECH/ecs-in-this-game/ecs-in-this-game.md` § *Command buffers*] :
  1. `CreateCommandBuffer()` une fois par `OnUpdate`, **jamais mis en cache** dans un champ
     (le tampon meurt à la lecture de la barrière) ;
  2. depuis un job : `AddJobHandleForProducer(Dependency)` après le `Schedule` ;
  3. utiliser **la barrière de sa phase** : `ModificationBarrier4` en `Modification4`
     (`ParkingBanToggleSystem`) ; `EndFrameBarrier` depuis `GameSimulation` (`ImpoundSystem`) —
     **interdit** depuis les phases Modification / Tool / Deserialize (exception
     « Trying to create EntityCommandBuffer when it's not allowed! ») ;
  4. ECB maison `new EntityCommandBuffer(Allocator.Temp)` + `Playback(EntityManager)` + `Dispose()`
     quand l'effet doit être immédiat (`HubSaleSystem`) ; un ECB de barrière ne se dispose jamais.
- **Jobs / Burst** : on n'en écrit pas encore. Si on en écrit : `JobChunk` + `ScheduleParallel`,
  tri des commandes par `unfilteredChunkIndex`, `base.Dependency = handle`, combiner (jamais
  remplacer) les handles, `Dispose(handle)` des conteneurs [V `performance-and-memory.md` § *job-handle
  discipline* ; https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/systems-entity-command-buffer-playback.html].
  `SystemAPI.*` ne fonctionne que réécrit par le générateur ; une forme non réécrite **lève à
  l'exécution** [V `ecs-in-this-game.md`].
- **Collections natives** : `using var` sur tout `ToEntityArray(Allocator.Temp)` ;
  `Persistent` → `Dispose` dans `OnDestroy` [V https://cs2.paradoxwikis.com/How_To_Avoid_Memory_Leaks].
- **Abonnements** à un événement C# (`Mod.Settings.onSettingsApplied`) : se désabonner dans
  `OnDestroy` (`ElectricTrainsOnlySystem`).

## 7. Sauvegarde et retrait du mod

Règle CLAUDE.md : retirer le mod ne corrompt jamais une partie.

| Besoin | Mécanisme | Chez nous |
|---|---|---|
| Rien à garder | données de prefab, état en mémoire | rail, panneau, Local Hub, quais |
| Un bit par entité | composant vide `IEmptySerializable` | — |
| Une valeur par entité | `IComponentData, ISerializable`, **entier de version écrit en premier** | `Policies/StreetParkingBan.cs` |
| Un état pour toute la ville | section de système (`ISerializable` sur le système + `SetDefaults`) | — |
| Rien dans la save mais un état vivant | replier dans les champs vanilla avant `Serialize`, restaurer après | — (modèle Water Features) |

[V `TECH/save-serialization/save-serialization.md` § *Choosing where your data lives*]

- Composant **sans** `ISerializable`/`IEmptySerializable` = **perdu sans prévenir** à la sauvegarde.
- Mod retiré : nos types ne se résolvent plus → **sautés**, la ville charge, une ligne de log par
  type [V § *Type identity, renames and uninstalls* ; `ObsoleteComponentSerializer.cs`].
- Nos prefabs posés (Local Hub, quais) deviennent des **placeholders** (« objet manquant »), la
  partie charge [V même § ; `DEC/Game.Serialization/ResolvePrefabsSystem.cs`]. [S] Les composants
  greffés sur la gare voyageurs par l'extension restent sur l'instance (plan 008) : à recetter.
- Renommer un type sérialisé : `[FormerlySerializedAs]` de **`Colossal.Serialization.Entities`**
  (pas celui de Unity).
- `context.purpose` (`NewGame`, `LoadGame`, `SaveGame`…) distingue les cas dans tous les hooks.

## 8. Harmony (dernier recours)

Ordre à essayer : insérer un système → désactiver + forker un vanilla → réécrire une requête vanilla
→ réflexion mise en cache → **patch** [V `TECH/patching/patching.md`]. Aujourd'hui : **zéro patch**
dans le mod (vérifié par grep le 05/10).

Si un jour nécessaire [V `patching.md` ; https://harmony.pardeike.net/articles/patching.html] :
id unique (`"SolarpunkMod"`), `PatchAll` dans `OnLoad`, `UnpatchAll(id)` (jamais sans id) ;
méthodes de patch `static` ; un patch sur l'`Execute` d'un job **Burst** ne s'exécute jamais ;
un préfixe qui renvoie `false` saute l'original **et** les préfixes suivants des autres mods ;
méthodes inlinées non patchables.

## 9. Anti-patterns

- Chercher un prefab vanilla dans `OnLoad` (pas encore chargé).
- `UpdateAt` pour remplacer un système vanilla (atterrit en fin de phase) : ancrer le fork
  (`UpdateBefore<Fork, Vanilla>`) puis `Enabled = false` sur le vanilla ; piège : type renommé →
  rien désactivé, sans erreur [V `mod-lifecycle-and-ordering.md` § *Disabling a vanilla system*].
- `GetUpdateInterval` sur un système UI ou de modification (code mort qui ressemble à un frein).
- Intervalle calculé depuis une option (figé à l'enregistrement).
- Requête sans `Exclude<Temp>` (compte l'aperçu du joueur) ou sans `Exclude<Deleted>`.
- `GetComponentLookup` dans `OnUpdate` ; lookup lu sans `CompleteDependency` ni job.
- ECB de barrière gardé dans un champ ; `EndFrameBarrier` depuis une phase de modification.
- Changer un composant visible sans `BatchesUpdated` (le rendu garde l'ancien) [V `PLUGIN/cs2-modding/SKILL.md`].
- `DestroyEntity` direct sur une entité de simulation (utiliser le tag `Deleted`, ou
  `VehicleUtils.DeleteVehicle` pour un véhicule, comme `ImpoundSystem`).
- Lire une valeur d'équilibrage dans l'initialiseur C# d'une classe prefab au lieu du `*Data` du
  prefab (d'autres mods ou un patch la changent).
- Patch Harmony par réflexe.
