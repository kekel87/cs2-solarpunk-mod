# Bâtiments

Modèles low-poly des bâtiments du mod, **générés par script Blender**, comme les véhicules
(`../vehicles/README.md` : atlas de couleurs, textures, LOD, recoloration, pièges, licence).

| Dossier | Bâtiment | Emprise |
|---|---|---|
| `local-hub/` | Local Hub de quartier : halle à sheds, quai rail couvert, rampe vélos-cargo | 4 × 5 cellules (32 × 40 m) |
| `local-hub-large/` | Grand Local Hub urbain **d'insertion** : immeuble mitoyen aligné sur la rue, rez logistique (2 portes de chargement, vitrine « LOCAL HUB », arcade vélos-cargo), 3 étages logements/bureaux à balcons plantés, attique bois en retrait, toit végétalisé + pergola solaire ; quai ferroviaire **souterrain** à −20 m | 4 × 4 cellules (32 × 32 m) |
| `local-hub-underground/` | Local Hub souterrain : pavillon vitré, 2 monte-charges vers le quai à −20 m, abri d'escalier, auvent solaire côté vélos | 3 × 4 cellules (24 × 32 m) |

`common/building_kit.py` réutilise `../vehicles/common/vehicle_kit.py` (maillages, atlas, export,
LOD1) et ajoute la palette des bâtiments, des formes (dalles par bornes, panneaux inclinés, arbres,
texte) et des aperçus adaptés (3/4 avant, 3/4 arrière, dessus, masque).

## Régénérer

```sh
REPO=$(git rev-parse --show-toplevel)
flatpak run --filesystem="$REPO" org.blender.Blender --background \
  --python "$REPO/assets/buildings/local-hub/build.py"
```

Sortie : `<Nom>.fbx`, `<Nom>_LOD1.fbx`, `_BaseColor`, `_ControlMask`, `_MaskMap`, `_Normal`,
aperçus `_preview_34`, `_back`, `_top`, `_mask`, puis `TRIANGLES <nom> LOD0 <n> LOD1 <n>` et
`UV_CHECK` (faces hors de leur case d'atlas, doit valoir 0).
Les aperçus montrent un mannequin de 1,75 m et le vélo-cargo `SolarpunkCargoBikeBox01` pour
l'échelle : ni l'un ni l'autre n'est exporté.

## Conventions bâtiment

D'après le wiki Paradox, *Asset Pipeline: Buildings* (consulté le 05/10/2026) :
- **Échelle** : mètres ; 1 cellule = 8 × 8 m.
- **Pivot** : en bas au centre du lot. **Sol** : exactement à 0 sur l'axe vertical. Des faces tournées
  vers le bas à 0 annulent la fondation automatique : la dalle du lot (`Lot`) en a.
- **Noms** : l'objet mesh porte exactement le nom du fichier ; un seul matériau, du même nom.
  LOD : `_LOD1` (moins de 50 % des triangles du LOD0), un FBX par niveau. FBX version 2018.
- **Textures** : `_BaseColor`, `_ControlMask` (R/G/B = couleurs 1/2/3, A = retrait de la neige),
  `_MaskMap` (R métal, G vernis, B inutilisé, A brillance), `_Normal` (OpenGL), `_Emissive`
  (pas encore fourni). PNG carrées, puissance de 2, 512 à 4096, toutes de la même taille (ici
  1024 px, 256 px par case d'atlas ; 2048 px si la palette dépasse 16 teintes).
- **LOD** : le jeu n'en génère pas par défaut (`LODPostProcessor` sur `Skip`), d'où notre `_LOD1`.
- **Recoloration** : Color Properties à poser dans l'éditeur sur le RenderPrefab (voir le README
  véhicules) ; le Virtual Texturing n'est appliqué qu'au packaging.
- **Budget** : pas de chiffre pour les services ; les bâtiments zonés basse densité visent environ
  8 000 triangles (15 000 maximum). Le Local Hub en fait environ 4 100.
- **Orientation [S]** : côté rue vers −Y dans Blender, comme l'avant des véhicules. Le wiki dit
  seulement que le côté route fait face à la route. Export `-Z` avant, `Y` haut, axes et unités
  intégrés au maillage : import confirmé en jeu le 05/10/2026 (taille juste) ; le côté rue reste à
  vérifier face à une route.

Zones recolorables : murs (R), structure acier (G), habillages, fascia et arceaux vélo (B). Le bois,
la végétation, le verre et les panneaux solaires gardent leur couleur.

## Local Hub — ce qu'il reste à faire à l'import

- La **voie** n'est pas modélisée : le quai est à hauteur de plancher de wagon (1,10 m) le long de
  y = 11 m, l'axe de voie attendu est à y = 14 m sous l'auvent. Elle s'ajoute comme sous-réseau du
  prefab (comme les gares vanilla).
- Pas d'`_Emissive` : les lampes sont des aplats clairs.

## Grand Local Hub — à l'import

Refait le 05/10/2026 à la demande de l'humain (« un petit building collé aux bordures, qui s'insère
dans un groupe d'immeubles moyens ») : l'ancienne halle isolée 8 × 10 cellules avec voies en surface
est abandonnée. Inspiration : *Creator Pack: Urban Promenades* (Feindbold) — immeubles mixtes à rez
commercial et toits-terrasses, et ses 5 services urbains compacts (accès véhicules intégrés à la
façade) ; aucun asset du pack réutilisé.
- **Gabarit** : bloc de 32 m de large (lot entier : murs mitoyens aveugles en x = ±16 m pour coller
  aux voisins), façade **sur l'alignement** y = −16 m (bord avant du lot), 22 m de profondeur
  (y = −16 à 6), cour arrière jusqu'à y = 16 (abri vélos, arbre, local poubelles). Rez 5 m, 3 étages
  de 3,2 m, attique en retrait de 2,5 m, ≈ 19 m + pergola.
- **Accès côté rue** : deux portes de chargement (4 m de large, 3,8 m de haut) centrées en
  x = −9 et x = −4 m ; vitrine de x = −1 à 6 m ; **arcade vélos-cargo** de x = 6,5 à 15,4 m, rez en
  retrait de 2,4 m (arceaux).
- **Trémie vers le quai souterrain à −20 m** (non modélisé : sous-réseau souterrain, comme le Local
  Hub souterrain) : deux monte-charges de 3,4 × 3,4 m derrière les portes, centrés en x = −9 et
  x = −4 m, y = 1,0 à 4,4 m (têtes de machinerie visibles sur le toit). Axe de voie prévu selon X,
  sous le bâtiment (y ≈ −5).
- Environ 4 000 triangles en LOD0 (1 100 en LOD1). Aperçu `_preview_situation` : le bloc entre deux
  voisins gris (non exportés) pour juger l'insertion.

## Local Hub souterrain — à l'import

- **Quai à −20 m** (non modélisé : sous-réseau souterrain, comme les gares de métro vanilla), sous le
  lot, axe de voie prévu selon X vers y ≈ 0.
- Liaisons verticales à raccorder aux sous-voies piétonnes/de service : **deux monte-charges** de
  3,2 × 3,2 m centrés en x = −4 et x = +4 m, y = 4,1 m (portes côté rue) ; **trémie d'escalier** de
  x = −10,5 à −6,5 m, y = 9 à 14,5 m (sol sombre sous l'abri vitré) ; ventilation en x = 7,5…10,5 m,
  y = 9,5…13,5 m.
- Environ 2 250 triangles en LOD0 (650 en LOD1).

## Pièges des bâtiments

- **LOD1 décimé avant l'atlas** (`building_kit.build`) : décimer après coup fusionnait des faces de deux
  couleurs, à cheval sur deux cases (2 faces sur le grand hub). Les deux niveaux sont projetés chacun
  dans les mêmes textures, et partagent le matériau du LOD0.
- Les véhicules d'aperçu sont importés depuis leurs FBX « espace jeu » (Y haut, centimètres) :
  `import_preview_vehicle` les remet à l'échelle (×0,01) et debout (+90° X).
