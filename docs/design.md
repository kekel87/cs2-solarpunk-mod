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

## Modules

| # | Module | État | Faisabilité |
|---|---|---|---|
| 1 | **Déchets par rail** (train-poubelle) | Prochain | Bien supporté par le moteur, surtout de l'authoring de prefab — voir `references/dechets-par-rail-code-du-jeu.md`. ⚠️ Tigons Rail Infrastructure (133250) a des « Train Garbage Yards » d'export : à tester en jeu avant le plan |
| 2 | **Cargo tram / métro de marchandises** | Idée | Deux cases vides d'une matrice que le jeu remplit déjà 3 fois (Cargo Train/Ship/Airplane). Piste : pas de nouvel enum, `TransportType.Tram` + ligne/arrêt/véhicule clonés en mode cargo — voir `references/bonnes-pratiques-modding.md` |
| 3 | **Local Hub** (point de service + rail) | Idée | Brique existante : ICS (146817) transfère des ressources sans camion ni gare. Hook possible : intercepter avant dispatch, comme Industrial Freight Optimizer |
| 4 | **Usine branchée au rail** | Idée | Asset + composant gare de fret. Change Internal Roads (147332) prouve que les voies ferrées internes existent |
| 5 | **Vélo-cargo** | Idée | Aucun précédent. Version « fausse » (prefab vélo sur rue interdite aux voitures) facile et visuellement suffisante |
| 6 | **Climat urbain** (chaleur, eaux pluviales, renaturation simulée) | Lointain | Rien n'existe. Gros chantier |

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
InfoLoom (91433) pour vérifier la part modale.

Liens : `https://mods.paradoxplaza.com/mods/<ID>/Windows`

## Pourquoi un mod et pas un jeu

Une conversation du 11/06/2026 avait conclu « pas de mod » sur des faits périmés (« pas d'outils
officiels, BepInEx, reverse engineering »). Faux aujourd'hui : modding officiel, 1254 code mods,
frameworks, refontes profondes, Harmony standard.

Reste vrai pour le **GDD complet** (projet claude.ai *Vibe games*) : temporalité de 50-100 ans,
deux trajectoires, équité et climat comme systèmes de premier rang — ça ne rentre pas dans CS2.
**Position** : la mobilité et le fret sont ce qui amuse *et* ce qui est modable → le mod. Le
prototype web reste la maison du GDD si la flèche du temps redevient le sujet.

## Prochain pas : le train-poubelle

Plus faisable que le cargo tram, plus visible que le Local Hub, et l'image est forte : un train de
déchets qui sort de la ville au lieu d'une file de bennes. `Garbage` étant une ressource ordinaire,
le futur métro de marchandises la transportera gratuitement : un seul réseau de fret.
