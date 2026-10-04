# Mods existants — état de l'art (CS2 1.6)

> Balayage du 03/10/2026 via l'API Paradox Mods (`api.paradox-interactive.com/mods?modId=<ID>&os=Windows`)
> et GitHub. Colonne « 1.6 » = `requiredVersion` déclaré par l'auteur, pas un test en jeu.
> Fiche : `https://mods.paradoxplaza.com/mods/<ID>/Windows`.

## Ce qui n'existe pas (terrain libre, vérifié)

- **Cargo tram / métro de marchandises** : rien sur Paradox Mods ni GitHub. Seul précédent : un
  reskin CS1 retiré (Workshop 2897409721).
- **Vélo-cargo** : rien.
- **Climat urbain** (chaleur, eaux pluviales, inondations) : rien.
- **Zone sans voiture / interdiction des camions par district** : rien (Road Rules = par voie,
  ULEZ = péage).
- **Demande de la communauté** : non démontrée pour CS2 (aucune demande indexée de cargo tram ou de
  train-poubelle) — ne pas la présenter comme acquise.

## ⚠️ Déchets par rail : doublon partiel

**Tigons Rail Infrastructure Pack (133250)**, Tigon Ologdring, 1.6, MAJ 2026-08-18 : « **Train
Garbage Yards** and new garbage waggon to handle the rubbish being exported ». Remplace le 90794
(déprécié).

Supposé : gare de fret filtrée sur `Garbage` → **export** par rail, pas collecte urbaine par train.
**À tester en jeu avant le plan du module 1** : si elle exporte déjà les vrais déchets de la ville,
le module 1 change d'objet (gare de transfert urbaine, dispatch, incinérateur embranché…).

## Inspirations techniques

| ID | Mod | Source | Licence | Pour quoi |
|---|---|---|---|---|
| 156605 | Dedicated Cargo Facilities (theoubre, ALPHA) | fermé | — | Tue les demandes de transfert entrantes non autorisées **avant** que le jeu n'envoie un camion : le hook visé pour le Local Hub (logique décrite dans sa fiche) |
| 150547 | Local Logistics: Warehouse Deliveries (AmicusDeus) | [GitHub](https://github.com/AmicusDeus/WholesaleLogistics) | **MIT** | Entrepôt le plus proche → commerces avec de vrais véhicules du jeu. Modèle pour le Local Hub ; risque de conflit de dispatch |
| 124159 | Magic Garbage (River-mochi) | [GitHub](https://github.com/River-Mochi/MagicGarbage) | **MIT** | Jobs ECS Burst sur les déchets, `GarbageTransferProbe`. Incompatible avec nous si Auto-Clean actif |
| 146817 | ICS (GreenBlorg49301) | [GitHub](https://github.com/xiaoxuan19911030-commits/IntelligentCommodityScheduling) | aucune | Entrepôt virtuel (téléportation — l'inverse de notre fret visible). Lecture seule |
| 157113 | Vehicle Control Framework (DreamRebel) | — | — | Choix du modèle de véhicule par flotte (dont « garbage transfer ») → piste vélo-cargo « faux » |
| 141632 | Transport Dynamic Scaling | — | — | Nombre de véhicules des lignes cargo selon remplissage (1.5) |
| 141755 | Industrial Freight Optimizer | fermé | — | Réduit les demandes avant dispatch. **1.5.10f1, probablement cassé** |
| 75613 | Water Features (yenyang) | [GitHub](https://github.com/yenyang/Water_Features) | — | Base pour les eaux pluviales |

**Licences** : MIT (MagicGarbage, Local Logistics) = code réutilisable. GPL-3.0 (Parking Control) =
copier contaminerait le mod. Sans licence (RoadRules, ICS, Realistic Parking, Change Internal Roads,
Vehicle Controller) = lecture seule, aucune copie.

## Mods compagnons à jour (1.6)

**Anti-voiture** : Realistic Parking 87313 · **Parking Control 155806** (River-mochi, GPL-3.0 —
interdit le stationnement sur voirie par district ou route) · Road Rules 155436 (aussi dans
[CS2-Mod-Suite](https://github.com/willmakesrandomshit/CS2-Mod-Suite)) · Parkify 155651 · Extra
Networks and Areas 77175 · **Realistic PathFinding 121226** (ruzbeh0, remplace Pathfinding
Customizer 86462 figé en 1.3) · Realistic Trips 77171 · Dummy Traffic Remover 107683 · Traffic
80095 · Road Builder 87190 · Vehicle Controller 104781 · Tourism Overhaul 153543 (touristes par
avion/ferry/croisière → moins de fuites de voitures, à vérifier).

**Fret ferroviaire** : Tigons Rail Infrastructure 133250 · Dedicated Cargo Facilities 156605 ·
CargoTrainTerminal01 151136 · All Transit + Trucks 138390 · Change Internal Roads 147332 ·
Resource Locator 99048 (outil de recette).

**Tram / métro (décor)** : Tigon's Tram Infrastructure 120050 · Tigon's Subway Pack 129291.

**Végétalisation** : Tree Controller 75993 · Natural Regrowth 151943 · Árvore Absorption 150254
**ou** Trees Defend Air, Noise Pollution 155838 (concurrents, un seul).

**Outillage joueur** : Skyve 75804 · Find It 77240 · Move It 74324 · Anarchy 74604 · 529 Tiles
74328 · InfoLoom 91433.

## Tracé de quartiers (vérifié le 04/10/2026)

**Grid Road Generator 151745** (Ti4goc, GitHub actif, **sans licence**) : remplit un périmètre
libre de rues, culs-de-sac, raquettes, jitter, collectrices courbes — ~80 % du « quartier
organique ». Routes par code via `CreationDefinition` + `NetCourse` (modèle à lire). Mert's ToolBox
(MIT) : grilles, îlots arrondis (géométrique). Copaste 154371 et ctrlC (MIT) : copier-coller /
tampons de réseaux. Road Builder 87190 : profils, pas tracés. **Manque** : génération par
croissance (boucles, arborescence), gabarits paramétrés, préréglage « quartier sans voiture ».

## Non déclarés 1.6 (à tester ou à éviter)

Industrial Freight Optimizer 141755 · ULEZ 138746 · MagicTaxi 78966 · terminaux 141917, 138341,
126997 · AreaBucket 81157 · Vibrant Foliage 94265 · Pathfinding Customizer 86462 · Tigon's 90794
(déprécié).

## À signaler aux joueurs

**Weather Aware Citizens 161229** (nouveau, 1.6) : la météo annule les trajets à pied et à vélo —
effet anti-vélo.
