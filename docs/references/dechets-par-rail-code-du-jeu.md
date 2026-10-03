# Déchets par rail — ce que dit le code du jeu

> Recherche du 16/09/2026, reprise de la note de mise en veille. À revérifier contre la version 1.6 du jeu.


Source décompilée consultée : `github.com/bworthy89/roadmod`, ref
`5b49a4fc0c572f2b5133df83083ebb4afe2f76a6`, dossier `New folder/`.

### Les déchets sont une ressource transportable

`Game.Economy.Resource` :

```csharp
Chemicals, Pharmaceuticals, Beverages, Textiles, ...
Garbage = 549755813888uL,
Fish    = 1099511627776uL,
```

Même enum que l'acier ou le blé. Tout ce qui transporte une ressource transporte des déchets.

### Le transfert installation → installation est natif

`Game.Buildings.GarbageFacility` :

```csharp
public Entity m_GarbageDeliverRequest;
public Entity m_GarbageReceiveRequest;
public float  m_AcceptGarbagePriority;
public float  m_DeliverGarbagePriority;
public int    m_ProcessingRate;
```

`Game.Prefabs.GarbageFacility` :

```csharp
public int  m_VehicleCapacity  = 10;
public int  m_TransportCapacity;      // distinct des camions
public bool m_IndustrialWasteOnly;
public bool m_LongTermStorage;
```

Plus un flag `GarbageTransferRequestFlags.RequireTransport`.

### Le rail est déjà dans le chemin possible

`Game.Simulation.GarbageTransferDispatchSystem` :

```csharp
m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
```

`CargoLoading` — le pathfinding des transferts de déchets accepte déjà les points de
chargement fret. Sans doute utilisé pour l'export vers connexions extérieures
(`m_OutsideConnectionData` référencé juste à côté).

### La gare de fret filtre par données, pas par code

`Game.Prefabs.CargoTransportStation` :

```csharp
public ResourceInEditor[] m_TradedResources;
...
componentData.m_StoredResources |= EconomyUtils.GetResource(m_TradedResources[i]);
```

Mettre `Garbage` dans la liste suffit à ce que la gare stocke et expédie des déchets.

### Architecture cible

- **Hub de collecte** = bâtiment portant **deux composants** :
  - `GarbageFacility` avec `m_TransportCapacity > 0`, `m_LongTermStorage = true`,
    `m_ProcessingSpeed = 0`, priorité d'acceptation haute
  - `CargoTransportStation` avec `m_TradedResources = [Garbage]`
- **Rail** : ligne Cargo Train vanilla entre hub et site de traitement
- **Traitement** (incinérateur / tri / déchèterie) : `GarbageFacility` normal, avec composant
  gare de fret ou collé à une gare réceptrice
- **Camions** : seulement la collecte locale ; zéro si couplé au Local Hub

### À vérifier en jeu avant d'écrire du C#

1. Le dispatch exige le composant `GarbageFacility` sur la destination
   (`if (!m_GarbageFacilityData.HasComponent(...)) return;`) → d'où le bâtiment à deux
   composants. C'est la condition, pas un détail.
2. Le train charge-t-il bien `Garbage` depuis le `StorageCompany` de la gare ?
3. Équilibrage : `m_ProcessingRate` et les priorités accept/deliver.

À lire d'abord : [MagicGarbage](https://github.com/River-Mochi/MagicGarbage), fichier
`Systems/GarbageTransferProbe.cs` — quelqu'un instrumente déjà ce système, en open source.

---

