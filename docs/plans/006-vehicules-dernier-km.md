---
status: in-progress
created: 2026-10-05
updated: 2026-10-05
---

# Plan 006 — Véhicules du dernier kilomètre (modèles 3D)

## Ce que tu verras

- **Aujourd'hui (sans le jeu)** : 4 images d'aperçu par véhicule (3/4, profil, arrière, masque de
  couleur) — vélo-cargo à caisse, vélo + remorque palette, triporteur électrique, kei truck
  électrique — blancs, low-poly, à valider à l'œil.
- **Plus tard, en jeu** (quand le Local Hub existe) : ces véhicules livrent les commerces depuis le
  hub ; couleurs variées tirées par le jeu ; un habitant du jeu de base pédale ou conduit.

## Contexte

- Gamme validée par l'humain : 4 niveaux de capacité pour le futur Local Hub (jalon 2).
- Aucun vélo-cargo ni petit utilitaire électrique n'existe pour CS2 (recherche Paradox Mods du
  2026-10-05). Références de l'humain (Sketchfab) : Urban Arrow parcel post, Amazon delivery bike,
  bicylift-with-pallet, Three Wheeled Truck, Mini Truck Rickshaw → licences Standard/Editorial :
  **inspiration seulement**, on recrée. Asian Mini Truck (evan.hiltz) et Simple Kei Truck
  (spookyghostboo) : **CC BY 4.0**, réutilisables avec crédit.
- Outil : Blender 5.2 (flatpak utilisateur), piloté par script Python, sans interface :
  `flatpak run --filesystem=<dossier> org.blender.Blender --background --python <script>.py`
  (le bac à sable flatpak exige `--filesystem` et un chemin absolu).
- **Prototype fait** (scratchpad, hors dépôt) : vélo-cargo à caisse, 408 triangles, FBX exporté,
  aperçu rendu en Workbench → la chaîne script → FBX → image marche.

## Faits vérifiés dans le code du jeu (1.6.2f1)

- **Cycliste / conducteur vanilla : oui.** À la création d'un véhicule de livraison,
  `TripNeededSystem.cs:226-229` appelle `CreatePassengers(..., driver: true)` ; celui-ci
  (`:252-300`) lit le buffer `ActivityLocationElement` du prefab du véhicule et, pour une place
  `Driving` **ou `Biking`**, crée un habitant factice (`ResidentFlags.InVehicle | DummyTraffic`)
  assis à la position/rotation de la place. Le rendu choisit alors la pose : `MeshGroupSystem.cs:247-268`
  (`Biking` → `RequireBicycle`, `Driving` → `RequireMotorcycle`). Donc : le modèle **ne contient pas
  de cycliste** ; le prefab porte un composant `ActivityLocation` (`Game.Prefabs/ActivityLocation.cs`,
  autorisé sur `VehiclePrefab`) avec une place `Biking` au niveau de la selle.
- **Couleurs** : `ColorProperties` (`Game.Prefabs/ColorProperties.cs:17-120`) = liste de jeux de
  **3 couleurs** (`VariationSet.m_Colors[3]`), liés aux canaux 0/1/2 par `m_ChannelsBinding`. Le jeu
  tire un jeu par instance. Côté texture : un `_ControlMask` indique quelle zone suit quel canal
  [S : canal R/G/B du masque ↔ couleur 0/1/2, à confirmer à l'import].
- **Import** (wiki, partiel) : 1 unité = 1 m, échelle 1:1 ; suffixes `_BaseColor`, `_Normal`,
  `_MaskMap`, `_ControlMask`, `_Emissive` ; `_LOD1`, `_LOD2` optionnels mais conseillés pour un
  véhicule ; les parties à moins de 5 cm du sol sont détectées comme roues. Axe avant et pivot non
  documentés [S : avant = −Y Blender, export `axis_forward=-Z, axis_up=Y`, à confirmer à l'import].

## Décisions

1. Modèles **générés par script** (formes simples, low-poly, ≤ 4 000 triangles LOD0), pas de
   sculpture ni de texture peinte. Les 4 véhicules, kei truck compris,
   sont **recréés** par script (style homogène, licence à nous) ; les modèles CC BY ne servent que de référence.
2. Base **blanche + `_ControlMask`** : caisse/carrosserie = canal 0, cadre = canal 1, accent = canal 2.
   Aucune marque ni logo.
3. **Pas de cycliste modélisé** : place `Biking` (vélos) ou `Driving` (triporteur, kei truck) via
   `ActivityLocation`. Repli silhouette seulement si l'import le refuse.
4. Roues = objets séparés, bas à z = 0 ; carrosserie à plus de 5 cm du sol.
5. **Rangement et commits** : chaque véhicule dans `assets/vehicles/<nom>/`, avec script `.py`,
   FBX et aperçus PNG tous commités (aucun `.gitignore` pour les générés).

## Étapes

### Lot 1 — sans le jeu (fait le 05/10/2026)

Avancement : étapes 1 à 7 faites. Noms d'assets **`SolarpunkCargoBikeBox01`,
`SolarpunkCargoBikePallet01`, `SolarpunkElectricTrike01`, `SolarpunkKeiTruck01`** (pas de `_` dans
le nom : l'importeur découpe sur `_`, `Colossal.AssetPipeline/AssetUtils.cs` `ParseName`). Par
véhicule : `<Nom>.fbx` (LOD0), `<Nom>_LOD1.fbx` (décimation 0,35), textures 512 px `_BaseColor`,
`_ControlMask` (R/G/B = couleurs 1/2/3), `_MaskMap`, `_Normal`, aperçus `_preview_34/_side/_back/_mask`.
Triangles LOD0 / LOD1 : vélo-cargo 3 112 / 1 067, vélo + palette 3 928 / 1 349, triporteur
2 024 / 690, kei truck 2 284 / 752. Conventions vérifiées vs supposées : `assets/vehicles/README.md`.

1. Script commun (matériaux, roue, tube, export FBX, rendu d'aperçu) + un script par véhicule.
2. Vélo-cargo à caisse (reprendre le prototype ; caisse 1,2 × 0,9 × 1,1 m, 2 roues avant, 1 roue
   arrière de vélo).
3. Vélo + remorque palette (vélo simple, remorque 2 roues, europalette 1,2 × 0,8 m + cartons).
4. Triporteur électrique façon Ape (cabine arrondie, plateau bâché, 1 roue avant, 2 arrière).
5. Kei truck électrique recréé par script (cabine avancée, plateau à ridelles, 4 petites roues).
6. Pour chaque véhicule : UV simples, `_BaseColor` blanc/gris, `_ControlMask` 3 zones, `_MaskMap`,
   `_Normal`, LOD1 par décimation, aperçus PNG (3/4, profil, arrière, masque). ✅
7. Ancre de selle / siège notée pour `ActivityLocation`. ✅

### Lot 2 — plus tard, en jeu avec l'humain (après le Local Hub)

8. Import du vélo-cargo seul dans l'Asset Importer : vérifier axe avant, échelle, roues détectées,
   masque de couleur.
9. Prefab : base `DeliveryTruck` (capacité faible), `ColorProperties` (3-4 jeux de couleurs
   solarpunk), `ActivityLocation` place `Biking`. Le véhicule est **créé par le mod** pour les trajets
   du Local Hub (plan 007, décision 3 : pas de remplacement de modèle après coup).
10. Publication de l'asset (voir point ouvert 2), puis les 3 autres véhicules.

## Risques

- Axe/pivot/masque non documentés pour les véhicules → découverts seulement à l'import (lot 2).
- Place `Biking` sur un véhicule de livraison : le code l'accepte, mais l'animation de pédalage sur
  un véhicule non-vélo est non testée [S].
- L'Asset Importer tourne dans le jeu sous Proton : chemins Windows/Linux à caler.
- Le hub n'existe pas encore : sans lui, les véhicules ne servent à rien en jeu.

## Recette

- **Lot 1** : l'humain valide les aperçus (dont `_preview_mask` : zones recolorables cohérentes) (silhouette reconnaissable, proportions, rien de
  « jouet »). Aucun scénario en jeu.
- **Lot 2** (futur) : véhicule visible dans l'éditeur ; il roule, roues qui tournent ; couleur
  différente d'une instance à l'autre ; un habitant assis/pédalant ; retrait de l'asset → la partie
  charge.

## Points ouverts pour l'humain

1. **Publication** : asset séparé sur Paradox Mods (déclaré en dépendance) ou embarqué dans le mod ?
   (Lot 2, après Local Hub)

## Hors périmètre

- Le Local Hub lui-même (bâtiment, dispatch) : plan séparé.
- Animation de pédalage spécifique, textures détaillées, rider modélisé.

## Décisions de l'humain (05/10/2026, après le premier import en jeu)

- **Roues fixes** pour l'instant ; roues animées (os `RollingTire` / `SteeringTire`) dans l'itération
  « modèles plus propres et détaillés ».
- **Livraison** : pack d'assets séparé publié sur Paradox Mods, déclaré en dépendance du mod
  (`<Dependency Id=…/>`), retrouvé par code via `PrefabID(nameof(CarPrefab), "<NomAsset>")`.
- **Vélos-cargo hybrides** : un vélo qui **livre** et qui circule comme un vélo (pistes cyclables,
  trottoirs, rues piétonnes). Recherche à faire avant le lot C : quelles voies un véhicule peut
  emprunter (type de route, `BicyclePrefab` + composant de livraison ?), comment le hub l'envoie.
  En attendant, gabarit `EU_DeliveryVan01` (circule sur la route).
- Atlas corrigé (4×4 cases, 1024 px, UV resserrées) contre les franges de loin ; réimport à faire.
