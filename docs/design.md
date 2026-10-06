# Solarpunk Mod — vision et modules

> Origine : post forum Paradox « Modern urban eco-planning » (Kekel87, 07/12/2023,
> [thread 1615871](https://forum.paradoxplaza.com/forum/threads/modern-urban-eco-planning.1615871/)),
> puis note de recherche du 16/09/2026 (reprise ici le 03/10/2026).
> Jeu ciblé : Cities: Skylines II **1.6.\*** (sept. 2026), maintenu par Iceflake Studios.

## La vision

Une ville **sans voiture ni camion**. Tout le fret par rail, jusqu'au métro de marchandises.
Dernier kilomètre en vélo-cargo. Rues piétonnes végétalisées. Déchets par train.
Plus tard : le **climat urbain** — îlots de chaleur, eaux pluviales, renaturation.

**Un seul mod**, organisé en modules. Ce que d'autres mods font déjà bien, on le déclare en
dépendance au lieu de le refaire.

## Intention (03/10/2026, réponses de l'humain)

- **Public** : d'abord pour la partie de l'humain, mais **publié** sur Paradox Mods.
- **Propos** : **montrer** aux gens qu'un avenir sans voiture personnelle ni camion est possible.
  Démonstratif plus que punitif.
- **Climat** : second temps, mais ambition réelle — montrer comment les villes résolvent le
  réchauffement (pas seulement de l'ambiance).
- **Priorité : le POC.** Prouver vite que le mod fonctionne et qu'un futur sans pollution ni
  réchauffement est jouable. Chaque module commence par sa version minimale démontrable.
- **Visuel : pas une priorité.** Aucun modèle 3D créé : **réemploi des meshes vanilla** (prefabs
  clonés) ou formes neutres (« carrés verts »). L'humain (dev frontend) ne fait ni code de jeu ni
  asset : Claude fait tout.
- **Contexte** : l'humain hésitait à faire un jeu from scratch ; le projet explore aussi ce qu'on
  peut faire avec Claude + CS2.

## Ton

Optimiste (« on y est arrivé à temps »), infrastructure **visible et belle**, routines matérielles
(le train-poubelle du mardi), coûts réels (transbordement, exceptions assumées). Éviter le moralisme
(récompenser plutôt que punir la voiture), le techno-solutionnisme, le solarpunk d'enclave, la
nostalgie anti-ville. Sources : `references/fiction-et-pensee-urbaine.md`.

## Modules

| # | Module | État | Faisabilité |
|---|---|---|---|
| 1 | **Déchets par rail** (train-poubelle) | Jalon 3 | Bien supporté par le moteur, surtout de l'authoring de prefab — voir `references/dechets-par-rail-code-du-jeu.md`. ✅ Testé le 03/10/2026 : les « Train Garbage Yards » de Tigons Rail Infrastructure (133250) font déjà circuler des trains de déchets entre gares de la ville. **On s'appuie sur 133250** (décision 010 ; entrées non électrifiées à masquer si possible) → notre module se recentre sur ce que Tigon ne fait pas |
| 2 | **Cargo tram / métro de marchandises** | Idée | Deux cases vides d'une matrice que le jeu remplit déjà 3 fois (Cargo Train/Ship/Airplane). Piste : pas de nouvel enum, `TransportType.Tram` + ligne/arrêt/véhicule clonés en mode cargo — voir `references/bonnes-pratiques-modding.md` |
| 3 | **Local Hub** (point de service + rail) | Codé non recetté (05/10/2026, plan 007 lots A+B) ; variantes petit/grand, surface/souterrain, train seulement (plan 010, en rédaction) | Brique existante : ICS (146817) transfère des ressources sans camion ni gare. Hook possible : intercepter avant dispatch, comme Industrial Freight Optimizer |
| 4 | **Usine branchée au rail** | Idée | Asset + composant gare de fret. Change Internal Roads (147332) prouve que les voies ferrées internes existent |
| 5 | **Vélo-cargo** | Idée ; décidé hybride (livre et circule sur pistes cyclables, trottoirs, rues piétonnes), recherche à faire | Aucun précédent. Version « fausse » (prefab vélo sur rue interdite aux voitures) facile et visuellement suffisante |
| 6 | **Panneau d'info** (part modale, camions, fret, électricité renouvelable) | ✅ Fait (jalon 1) | Lecture seule de l'ECS. Aucun mod ne donne la part modale ville (InfoLoom non plus) |
| 7 | **Énergie citoyenne** (toits solaires, solaire de balcon) | Idée | Politique de district clonée d'*Energy Consumption Awareness* — à confirmer dans le décompilé |
| 8 | **Chaleur et énergie** (carte de chaleur, clim/chauffage, réseau de chaleur) | Lointain | Rien n'existe. Gros chantier |
| 9 | **Eaux pluviales** (renaturation simulée) | Lointain | Réveiller `SoilWaterSystem` |
| 10 | **Stations mixtes voyageurs + fret** : gare ferroviaire (petite / moyenne / grande, surface / souterraine) avec **quais dédiés** (voyageurs / fret / déchets), puis arrêts tram et métro mixtes | V1 « Quai fret » + quai déchets codés non recettés (05/10/2026, plan 008) ; gare mixte autonome = objectif à terme | Prolonge le Local Hub (plan 007) et la « porte de ville » ; rejoint le module 2 pour tram/métro. **Faisable par clone de prefab, sans système maison** (vérifié dans le décompilé le 05/10/2026) : un bâtiment peut porter `TransportStation` + `CargoTransportStation` (le code vanilla prévoit le cas, `BuildingInitializeSystem.GetRestrictionFlags`) ; il faut **deux arrêts** (un voyageurs, un fret : un arrêt n'a qu'un type d'accès). Variante la plus proche du vanilla : gare voyageurs + **extension fret**, modèle `Airport01 Cargo Terminal` (V1 du plan 008, choisie le 05/10/2026). **Objectif à terme gardé** : gare mixte autonome à quais dédiés (voyageurs / fret / déchets / usages Tigon). Tram/métro fret : la sim l'accepte (sélection véhicule, dépôt, pathfinding agnostiques du type), manquent la vue d'ensemble des transports et les stats ; un terminal avec stockage est nécessaire. Souterrain = sub-nets à y = −20 (`TO_UndergroundRailStation01`). Tigon : aucune gare mixte, gares de fret de 5x5 à 38x17 réutilisables. Risques : accès piéton/fret des quais, sauvegarde (comme plan 007), stockage qui attire des camions |
| 11 | **Gare fret unique à sélecteur** : une seule gare de fret, et le joueur choisit dans son panneau ce qu'elle traite (marchandises, déchets, ressources : charbon, pétrole, bois, agricole…), au lieu d'une gare par ressource comme Tigon | Idée (05/10/2026) | Plus simple pour le joueur (une gare, des voies communes) et **nous découple de Tigon**. Faisabilité à vérifier : les ressources échangées viennent aujourd'hui du prefab (`m_TradedResources`) → il faudrait un réglage par bâtiment posé (composant sauvegardé) relu par notre système, et une section de panneau pour le choix |
| 12 | **Sociétés privées de logistique douce** : des entreprises de transport (zonées ou à poser) qui exploitent vélos-cargo, triporteurs et kei trucks électriques pour livrer les commerces, à côté (ou à la place) des Local Hubs publics | Idée (05/10/2026) | Prolonge le Local Hub (plan 007) côté marché privé : une entreprise de transport du jeu (`TransportCompany`) dotée de nos véhicules. Faisabilité non vérifiée |
| 13 | **Hub modulaire** : Local Hub en 3-4 tailles (petit plat, moyen plat, moyen urbain mitoyen, grand urbain ; la taille fixe stockage et nombre d'extensions). **Base sans rail** : marchandises reçues en camion (usines, gares), livrées en vélo / petit véhicule, **et collecte des déchets du quartier**. **Extensions** : quai marchandises (train, puis tram / métro, surface / souterrain), quai déchets, arrêt de bus (« gare routière »), composteur (module 14) | Idée retenue (05/10/2026), à planifier après la recette du soir ; remplacera les 3 variantes à voie fixe du plan 010 | Extensions = `ServiceUpgrade`, déjà utilisé pour les quais de gare (plan 008). Déchets : le jeu transfère déjà des déchets entre installations (`GarbageTransferRequest`, drapeau `RequireTransport`, `GarbageFacilityAISystem.cs:588-641`) → hub = installation de déchets sans traitement qui collecte puis expédie en camion, ou en train avec un quai déchets. À confirmer dans le décompilé. Déchets séparés de la gare fret unique (module 11) écartés : l'humain veut que le hub collecte |
| 14 | **Compost et tri** : **points d'apport collectifs** (compost, verre, carton) collectés par la ville comme dans certaines villes réelles ; **composteur** (extension du hub ou bâtiment de parc) qui traite sur place une part des déchets du quartier ; **tri à la source** (politique de district) : moins de déchets, plus de recyclage, moins d'incinération | Idée retenue (05/10/2026), recherche à faire | Vanilla (décompilé 1.6.2) : **une seule ressource `Garbage`, aucun tri, aucun compost**. 4 installations : décharge (`m_LongTermStorage`), incinérateur (`ElectricityProducer`), centre de recyclage (`GarbageFacility` + `ResourceProducer` : déchets → ressources industrielles), déchets industriels (`m_IndustrialWasteOnly`) — `GarbageSection.cs:60-85`. Pistes : flux séparés simulés par le mod (part des déchets d'un bâtiment détournée vers compost / recyclage), produit du compost en ressource agricole ou bonus de quartier. Faisabilité non vérifiée |
| 15 | **Pods de location sur rail** : petits véhicules personnels loués à des « parkings » près des logements ; on conduit jusqu'à une voie ferrée (train / tram / métro) et le pod passe en mode automatique à la vitesse permise par l'infrastructure | Idée long terme (05/10/2026) | Gros chantier, risqué : un véhicule du jeu est soit routier soit sur rail, pas de changement de mode en route ; le calcul d'itinéraire devrait probablement être patché (Harmony = dernier recours). Recherche avant tout plan : un véhicule sur rail peut-il emprunter plusieurs types de voie (`TrackTypes`) ? |

Vérifié le 03/10/2026 : **aucun mod de cargo tram, métro de marchandises, vélo-cargo, climat urbain
ni zone sans voiture par district** — détail dans `references/mods-existants.md`. Précédents
réels, chiffres et échecs à ne pas reproduire : `references/monde-reel.md`.

## Ce qui est déjà dans le jeu (vanilla)

- **Vélos** depuis le patch 1.4.2f1 (nov. 2025) : pistes sur route / séparées / élevées / tunnel,
  parkings vélo (arceaux → hall souterrain 452+ places), politique **Urban Cycling Initiative**,
  outil **Bicycle Restriction**. Choix modal = voiture dispo dans le ménage + parking vélo + coût de
  pathfinding.
- **Lignes Cargo Train** (types de lignes : Bus, Tram, Subway, Train, Ship, Airplane, Cargo Train,
  Cargo Ship, Cargo Airplane).
- **Astuce** : du rail classique posé en voirie = le CarGoTram de Dresde, le jeu ne fait pas la
  différence.

## Mods compagnons — candidats à la dépendance

À trancher module par module : dépendance **obligatoire** (déclarée dans
`PublishConfiguration.xml`), **optionnelle** (détectée au runtime) ou simple **recommandation**
dans la description.

### Anti-voiture

| Mod | ID | Rôle |
|---|---|---|
| Realistic Parking | 87313 | Pas de place = trajet voiture impossible. **Le levier le plus fort** |
| Road Rules — Lane & Vehicle Restrictions | 155436 | Interdit `PrivateCars`/`Trucks` par voie (presets `TruckBan`, `TransitOnly`, `LocalAccessOnly`), écrit dans le pathfinding natif. [C# ouvert](https://github.com/willmakesrandomshit/RoadRules) |
| Extra Networks and Areas | 77175 | Routes invisibles → rues piétonnes où les bâtiments poussent |
| Parking Control | 155806 | Interdit le stationnement sur voirie par district ou route (GPL-3.0) |
| Realistic PathFinding | 121226 | Coûts de pathfinding (ruzbeh0) ; remplace Pathfinding Customizer 86462, figé en 1.3 |
| Realistic Trips | 77171 | Comportement de déplacement (ruzbeh0) |
| Dummy Traffic Remover | 107683 | Retire le faux trafic décoratif |
| ULEZ - Ultra Low Emission Zone | 138746 | Péage par district (⚠️ pas déclaré 1.6) |
| Traffic | 80095 | Successeur TM:PE (krzychu124) |
| Road Builder | 87190 | Routes sur mesure : piéton + tram + piste cyclable |

Fuites connues : visiteurs et touristes en voiture (Tourism Overhaul 153543 les fait arriver par
avion/ferry, à vérifier) ; véhicules de service incompressibles (normal). Weather Aware Citizens
(161229) annule les trajets à pied et à vélo par mauvais temps : effet anti-vélo.

### Tracé de quartiers

| Mod | ID | Rôle |
|---|---|---|
| Grid Road Generator | 151745 | Remplit un périmètre libre de rues organiques (culs-de-sac, raquettes, collectrices courbes). Dépendance pressentie du jalon 5 bis ; [GitHub](https://github.com/Ti4goc/GridRoadGenerator) sans licence |
| Copaste | 154371 | Copier-coller et tampons de réseaux routiers |

### Fret ferroviaire

Tigons Rail Infrastructure (133250, remplace 90794 déprécié) · Dedicated Cargo Facilities (156605,
alpha) · CargoTrainTerminal01 (151136) · All Transit + Trucks (138390) · Local Logistics (150547,
MIT — modèle du Local Hub). Pas déclarés 1.6 : Multi-access Cargo Train Terminal (141917),
CargoTrainTerminal Raw Resources (138341), One Way Cargo Terminal (126997), Industrial Freight
Optimizer (141755, probablement cassé).

### Végétalisation

Tree Controller (75993) · Natural Regrowth (151943) · **Árvore Absorption System (150254)** — les
arbres nettoient air et bruit (concurrent : Trees Defend Air, Noise Pollution 155838, un seul des deux) · Parkify (155651) · AreaBucket (81157) · Vibrant Foliage Pack
(94265) · Water Features (75613)

### Outillage joueur

Skyve (75804) · Find It (77240) · Move It (74324) · Anarchy (74604) · 529 Tiles (74328) ·
InfoLoom (91433, démographie et emploi — **pas** de part modale).

Liens : `https://mods.paradoxplaza.com/mods/<ID>/Windows`

## Banque d'idées (03/10/2026)

Toutes retenues par l'humain (« j'aime toutes ces idées »), **non ordonnées** : l'ordre se décide au
`/next`. Faisabilité : `references/capacites-vanilla.md`. Précédents : `references/monde-reel.md`,
`references/fiction-et-pensee-urbaine.md`.

**Fret et dernier kilomètre**
- **Local Hub + vélo-cargo** : micro-dépôt alimenté par rail ou tram, rayon ≈ 1,5 km, ≈ 50 % des
  marchandises éligibles (CycleLogistics). Leviers : vente `SaleFlags.Virtual`, interception
  `ResourceBuyer`, `DeliveryTruckSelectData`.
- **Cargo tram** : raccourci = ligne Cargo Train sur voies mixtes Train|Tram en voirie ; complet =
  ligne, arrêt, véhicule tram clonés en mode cargo.
- **Cargo tram de nuit** : voyageurs le jour, fret la nuit (Francfort, Karlsruhe).
- **Train de tournée** : ligne cargo en boucle par micro-hubs (Always Coming Home).
- **Métro de marchandises façon Chicago** : tunnels jusqu'aux sous-sols ; fin de partie, très cher.
- **Usine embranchée** : flux réguliers, peu d'origines (Dresde, Chicago).
- **Rail plus attractif par donnée** : coûts de pathfinding du fret.

**Déchets**
- **Gare de compactage + train-poubelle → incinérateur embranché qui chauffe la ville** (Staten
  Island, binliners, Spittelau). Mâchefer réexpédié par train.
- **Installations de traitement embranchées** (demande de l'humain, 03/10/2026) : incinérateur,
  centre de tri, décharge **directement reliés au rail** — l'autre bout de la ligne qui part des
  Train Garbage Yards de Tigon (133250, qui ne fait que l'export). Technique : cloner le prefab
  vanilla et lui ajouter un composant `CargoTransportStation` (`m_TradedResources = [Garbage]`) avec
  sa voie. Même technique que l'**usine embranchée**.
- **Versions souterraines** (idée de l'humain) : centre de collecte urbain avec gare souterraine — la gare disparaît de la rue, rejoint le métro de marchandises.
- **Tram-collecte de quartier** (Cargo-Tram Zurich).
- **Décharge-mine** : décharge pleine = gisement exporté par train (Walkaway).

**Mobilité des personnes** (idées de l'humain — `references/mobilite-personnes.md`)
- **Voitures partagées bimodes rail-route** (« on verra », idée gardée sans priorité) : roulent en ville, montent sur les rails, passent en
  automatique et s'intercalent entre les trains (RUF, PRT). Vrai bimode très dur en CS2 (aucun
  véhicule ne change de réseau) → POC : **navette-capsule automatique** (véhicule métro/tram cloné,
  4-10 places, intervalle très court) depuis un **parking-relais** en lisière (Heathrow ULTra).
- **Trains porte-voitures** (Autozug, Le Shuttle) : littéral dur et sans effet de sim → traduit en
  parking-relais en gare d'entrée de ville ; version fret = autoroute ferroviaire à la connexion
  extérieure (coefficients `OutsideTradeParameterData`).
- **Trains et métros automatiques** (ligne 14, Copenhague 24 h/24, AutoHaul) : amélioration de dépôt
  « GoA4 » (moins de personnel, intervalle et arrêts plus courts, service de nuit). Peu visible →
  amélioration transversale (y compris train-poubelle automatique).
- **Porte de ville** : un même ensemble gare + Local Hub + parking-relais en lisière, vélo-cargo et
  capsules en aval — les camions s'arrêtent au Hub, les voitures au parking-relais.

**Déchets (suite)**
- **Collecte pneumatique** (Envac, Wembley -90 % de bennes) : un terminal absorbe les déchets d'un
  rayon sans véhicule, puis train.
- **Bateau-poubelle / barge-entrepôt + vélo-cargo** (Amsterdam, Fludis sur la Seine).

**Énergie et chaleur**
- **Coopérative solaire / éolienne citoyenne** (Middelgrunden).
- **Chauffage par eaux usées ou incinérateur** (False Creek, Spittelau) — chauffage non simulé en vanilla.
- **Politique « Ombrières obligatoires »** (loi APER française) ; **batterie de sable** (absorbe le
  surplus solaire, Polar Night).
- **Réacteur de fusion de démonstration** : tardif, cher, capricieux — morale « la ville a été
  décarbonée avant la fusion ».
- **Monument ARES** (stockage par wagons sur pente) avec infobulle honnête.
- **Toits solaires de district** (Green Cities « self-sufficient ») et **solaire de balcon**
  (Allemagne) : politiques qui baissent la conso des bâtiments zonés — vanilla n'en a pas.
- **Rénovation Energiesprong** : conversion progressive d'un district, creux de chantier.
- **Clim et chauffage** (idée de l'humain, 04/10/2026) : la carte de chaleur pilote la demande de
  clim (pic électrique, rejets chauds qui réchauffent la rue) ; l'hiver, le chauffage. Leviers
  passifs (canopée, cool roofs, isolation) et collectifs (réseau de chaleur relié à
  l'incinérateur, anergie chaud et froid, stockage saisonnier Drake Landing). Le solaire produit
  quand la clim tourne.
- **Sortir du gaz quartier par quartier** (Pays-Bas, *wijkaanpak*) : mode de chauffage abstrait
  par bâtiment (gaz → pompe à chaleur / réseau de chaleur), biométhane issu des déchets ;
  hydrogène de chauffage au Chirper anti-hype.
- Micro-hydro en conduite d'eau, solaire flottant, agrivoltaïsme, autoconsommation collective.
- Détail et sources : `references/energie-et-politiques.md`.

**Innovations qui marchent** (`references/innovations-climat-logistique.md`)
- **Cargo à voile** (Neoline, TOWT, Grain de Sail — en service) : Cargo Ship cloné, pollution nulle.
- **Navettes de fret autonomes à batterie** (Parallel Systems) : habillage du cargo tram.
- **Micro-hub conteneur + quadricycle** (Fernhay).
- **Démolir l'autoroute, rouvrir la rivière** (Cheonggyecheon) : événement.
- **Dirigeable cargo** (Flying Whales, promesse ≈ 2029) : spectacle, niche.
- **Chirper anti-hype** : tube à colis, route solaire, camion hydrogène, captage du CO₂, tours à
  gravité, fusion « l'an prochain ».

**Voiture et espace public**
- **Secteurs à la gantoise** : politique de district « filtre modal » + vraies restrictions de voies
  posées par notre système (les `Forbid*` vanilla ne sont que des surcoûts).
- **Exception honnête** : « zéro camion » par district avec exemptions visibles (Ecotopia).
- **Assemblée de quartier** : piétonniser un district = creux de bonheur pendant le chantier, puis
  hausse, commerces +30 % (Poblenou).
- **Rues aux écoles / cours Oasis**.
- **Transformer la rue, pas seulement le district** (inspiration [Streetmix](https://streetmix.net), outil de profils de rue
  aimé par l'humain) : profils de rue solarpunk (voie voiture → piste cyclable, arbres, voie
  bus/tram, bacs plantés) via **Road Builder (87190)** en dépendance ; requalifier une rue existante
  en place, avec l'avant/après comme moment pédagogique.

**Climat**
- **Carte de chaleur** (`CellMapSystem<T>`) : bitume et voiture la montent, canopée et eau la
  baissent ; effet santé en été. Pas d'effet linéaire « +1 arbre » : seuil ≈ 30 % de canopée.
- **Canicule fondatrice** : événement qui frappe les districts minéraux (Ministry for the Future).
- **Eaux pluviales** : réveiller `SoilWaterSystem` (inondation par la pluie, code mort) ; rue-éponge,
  places-bassins (Copenhague), **rue-ruisseau** (Ecotopia).
- **Leviers chaleur** : cool roofs (politique de district, -1,2 à -2 °C généralisés), micro-forêts
  Miyawaki (≈ -6 °C local), ombrières.
- **Friche qui dépollue** (Nausicaä) ; **eau comme plafond de croissance** (Pacific Edge).

**Lecture et pédagogie** (`references/projets-dans-la-veine.md`)
- **Panneau « Ville solarpunk »** : part modale, camions et bennes évités, CO₂ transport évité,
  verdure — fil rouge de toutes les démos (la part modale et le CO₂ n'existent pas en vanilla).
- **Scénario « transformer une ville existante »** (voulu par l'humain dès son projet de jeu) : sauvegarde voiture-centrée, objectifs chiffrés,
  paliers qui débloquent (`TransportRequirementData`) ; format défi en série (YouTube) et atelier
  participatif (Block by Block).
- **Panneau donut** par district : plancher social (proximité des 6 fonctions, bonheur) / plafond
  écologique (pollution, part voiture).
- **Chirper** : « le saumon est revenu dans la rivière » ; incinérateur à la Hundertwasser.
- Idée lointaine : **téléphérique de fret** (Low-tech Magazine).

## Pourquoi un mod et pas un jeu

Une conversation du 11/06/2026 avait conclu « pas de mod » sur des faits périmés (« pas d'outils
officiels, BepInEx, reverse engineering »). Faux aujourd'hui : modding officiel, 1254 code mods,
frameworks, refontes profondes, Harmony standard.

Reste vrai pour le **GDD complet** (projet claude.ai *Vibe games*) : temporalité de 50-100 ans,
deux trajectoires, équité et climat comme systèmes de premier rang — ça ne rentre pas dans CS2.
**Position** : la mobilité et le fret sont ce qui amuse *et* ce qui est modable → le mod. Le
prototype web reste la maison du GDD si la flèche du temps redevient le sujet.

## Roadmap (04/10/2026)

Fil : **mesurer → retirer les camions → retirer les voitures → chaleur et énergie**. Chaque jalon
= une démo publiable. v0.1 = jalons 1 + 1 bis + 2 (« un quartier sans camion, mesuré »).

| Jalon | Contenu | En jeu |
|---|---|---|
| 0 | ✅ Socle : décompiler 1.6.2f1, mod chargé, logs | — |
| 1 | ✅ **Panneau d'info** (fait, recetté le 04/10/2026) : part modale, voitures, camions de livraison, bennes, trains de marchandises, part d'électricité renouvelable — instantané ville, lecture seule | Des chiffres qui bougent |
| 1 bis | **Politique « Fin du stationnement résidentiel »** : places de rue fermées, fourrière après préavis, moins de voitures prises pour le travail et l'école ; met en place la création de politiques de district (réutilisée au jalon 5). « Toits solaires » reporté au jalon 6 | Rues sans voitures garées |
| 2 | **Quartier sans camion** : Local Hub (bâtiment autonome, vente faite par le mod, petits véhicules créés par le mod, rayon district + 600 m réglable ; plan 007, lots A+B codés non recettés) + panneau par district (plan 004). « Rail attractif par données » et `SaleFlags.Virtual` abandonnés (jugés magiques) | District livré sans camion |
| 3 | **Déchets bout en bout** : incinérateur / centre de tri reliés au rail, à l'autre bout des yards de Tigon ; indicateur « déchets collectés par train » au panneau ; recyclage et déchets industriels ; à terme une **grande usine intégrée recyclage + incinération + déchets industriels** (« top tier », idée du 05/10/2026, plan 009 draft : amélioration « Embranchement ferroviaire » sur incinérateur/décharge, train + bennes) | Train-poubelle jusqu'au traitement |
| 4 | **Cargo tram** : raccourci voies mixtes, puis version clonée | Trams de marchandises en voirie |
| 5 | **Quartier sans voiture** : filtre modal par district + vélo-cargo | District piéton livré à vélo |
| 5 bis | **Générateur de quartier sans voiture** : boucles piétonnes / cyclables autour d'un arrêt de tram, hiérarchie collectrice → desserte, tracé organique. D'abord tester Grid Road Generator (151745) en dépendance, coder seulement le manque | Un quartier sans voiture posé en quelques clics |
| 6 | **Chaleur et énergie** : carte de chaleur, clim/chauffage par bâtiment, leviers passifs, réseau de chaleur depuis l'incinérateur | Îlots de chaleur, la canopée les réduit |
| 7 | **Eaux pluviales** | Inondations, rues-éponges |
| Final | **Scénario « transformer une ville existante »** : objectifs tirés du panneau, paliers | Partie guidée |

Panneau v1 : pas de « camions évités » ni de CO₂ (contrefactuel douteux), pas de vue par district
(v2, au jalon 2). Limite connue : « à pied » est gonflé (toute personne dehors sans véhicule compte,
ex. du parking à la porte). Ratio fret rail/camion écarté (rail compté au chargement, camion à chaque
livraison : trompeur). Hors roadmap, sans date : bimodes/capsules, métro de marchandises Chicago,
versions souterraines, fusion, dirigeable, ARES, cargo à voile, sortie du gaz.

## Ajouts du 05/10/2026 (codés à l'aveugle, non recettés)

- **Rail non électrique** (plan 003) : onglet dédié après Train (repérage par nom), **remplacé en recette (05/10, 21h) par le masquage complet** des voies, ponts et gares diesel de la barre d'outils (choix de l'humain : « osef dans mon mod ») ; onglet Train badgé d'un éclair (icônes du jeu composées au lancement) ; option « Trains
  électriques seulement » décochée par défaut (Tigon n'a aucune loco fret électrique, wagon-poubelle
  marqué Fuel, dépôt unique voyageurs/fret) — décision 015.
- **Panneau district** (plan 004) : trafic présent, camions/km, seuils voiture 20/40 % avec
  hystérésis, effectif minimum 30 — décision 017.
- **Véhicules du dernier km** (plan 006) : 4 modèles + Local Hub importés en jeu, première version
  (« plus propres/détaillés » à une prochaine itération) ; franges de texture sur vélos/triporteur.
- Règle de sauvegarde assouplie : bâtiments du mod = « objets manquants » au retrait, jamais de
  partie qui ne charge pas — décision 014.
