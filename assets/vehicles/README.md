# Véhicules du dernier kilomètre

Modèles low-poly des 5 véhicules du Local Hub, **générés par script Blender** (plan 006).

| Dossier | Véhicule | Conducteur |
|---|---|---|
| `cargo-bike-box/` | Vélo-cargo à caisse | `RiderAnchor` (place `Biking`) |
| `cargo-bike-pallet/` | Vélo + remorque palette | `RiderAnchor` (place `Biking`) |
| `electric-trike/` | Triporteur électrique | `DriverAnchor` (place `Driving`) |
| `kei-truck/` | Kei truck électrique | `DriverAnchor` (place `Driving`) |
| `covered-cargo-bike/` | Vélo-cargo couvert (quadricycle à pédalage électrique, caisse fermée arrière, capote et pare-brise, 4 roues ; **marqué vélo**, capacité prévue 1 500) | `RiderAnchor` (place `Biking`) à (0, −0,28, 0,95) en Blender |

Chaque dossier contient `build.py` et ce qu'il génère :

| Fichier | Rôle |
|---|---|
| `<Nom>.fbx` | LOD0 : un seul mesh `<Nom>`, un seul matériau `<Nom>` |
| `<Nom>_LOD1.fbx` | LOD1 : même mesh décimé (≈ 35 % des triangles) |
| `<Nom>_BaseColor.png` | Couleurs : gris neutre sur les zones recolorables, couleurs fixes ailleurs |
| `<Nom>_ControlMask.png` | Zones recolorables : R = carrosserie / caisse, G = cadre, B = accents |
| `<Nom>_MaskMap.png` | R métal, G vernis (coat), B noir, A brillance |
| `<Nom>_Normal.png` | Normale plate (OpenGL) |
| `_preview_34`, `_side`, `_back` | Aperçus couleur, avec le mannequin d'échelle |
| `_preview_mask` | Aperçu du `_ControlMask` appliqué (R/G/B vifs, noir = non recolorable) |

Textures 1024 × 1024 (toutes de la même taille) : un atlas de 4 × 4 cases de 256 px de couleur
unie, une case par teinte de la palette (`Palette.swatches()`) ; 8 × 8 cases en 2048 px si une
palette dépasse 16 teintes. Chaque face est projetée à plat, sur son axe dominant (aire UV jamais
nulle), dans les 15 % centraux de sa case : les mipmaps ne mélangent les cases voisines qu'aux tout
derniers niveaux. Le code commun est dans
`common/vehicle_kit.py` (dont le pédalier, la selle et le mannequin des deux vélos).

## Régénérer

Blender 5.2 en flatpak utilisateur. Le bac à sable flatpak demande `--filesystem` et un chemin absolu :

```sh
REPO=$(git rev-parse --show-toplevel)
flatpak run --filesystem="$REPO" org.blender.Blender --background \
  --python "$REPO/assets/vehicles/cargo-bike-box/build.py"
```

Le script réécrit tous les fichiers ci-dessus, puis affiche `TRIANGLES <nom> LOD0 <n> LOD1 <n>`
(cible LOD0 ≤ 4 000) et `UV_CHECK <nom> … <n>`, le nombre de faces dont les UV sortent de leur case
ou ont une aire nulle (doit valoir 0, LOD0 et LOD1).

## Conventions

**Noms** — vérifié dans le code de l'importeur (`Colossal.AssetPipeline/AssetUtils.cs`, `ParseName`) :
le nom est découpé sur `_` en `<Thème>_<Nom>_<Niveau>_<Lot>_<Module>_<LOD>_<Matériau>_<Suffixe>`.
Le nom d'asset ne doit donc **contenir aucun `_`** : d'où `SolarpunkCargoBikeBox01` (et plus
`SP_CargoBikeBox01`, qui serait lu comme l'asset `SP` avec un suffixe `_CargoBikeBox01`).

**Textures** — suffixes vérifiés dans `Colossal.AssetPipeline/Constants.cs` (`_BaseColor`,
`_ControlMask`, `_MaskMap`, `_Normal`, `_Emissive`). Sens des canaux d'après le wiki Paradox
(*Asset Pipeline: Buildings*) :
- `_ControlMask` : R, G, B = masques de couleur 1, 2, 3, mutuellement exclusifs ; A = retrait de la
  neige. **[S]** : documenté pour les bâtiments, supposé identique pour les véhicules ; A laissé à 1.
- `_MaskMap` : R métal, G vernis, B inutilisé (noir), A brillance.
- `_Normal` : convention OpenGL ; ici plate, **[S]** utile seulement si l'importeur l'exige.
- Résolution : PNG 8 bits, carrées, puissance de 2 entre 512 et 4096, **toutes de la même taille**
  ([Asset Pipeline: Props](https://cs2.paradoxwikis.com/Asset_Pipeline:_Props)).
- Mipmaps générées à l'import (trilinéaire, anisotrope ×16, wrap Clamp, compression BC), vérifié dans
  `Colossal.AssetPipeline/Settings.cs`. Le Virtual Texturing n'est appliqué qu'au **packaging** de
  l'asset : dans l'éditeur, les textures importées ne sont pas encore traitées, ce n'est pas le rendu
  final ([Assets: Importing](https://cs2.paradoxwikis.com/Assets:_Importing)).
- **[S]** : la zone recolorable est peinte en gris clair dans `_BaseColor` ; reste à voir en jeu si
  la couleur de variation multiplie ou remplace la couleur de base.

**LOD** — un FBX par niveau, `<Nom>_LOD1.fbx`, mesh et matériau nommés pareil (wiki ; le suffixe
`_LOD<n>` est aussi celui de `LOD.MakeName`). LOD1 < 50 % des triangles du LOD0 (ici ≈ 35 %),
mêmes textures que le LOD0 ([Props](https://cs2.paradoxwikis.com/Asset_Pipeline:_Props)). **Le jeu
ne génère pas de LOD par défaut** : `LODPostProcessor` est réglé sur `Skip`, la génération ne se fait
que si on l'active à l'import (`Colossal.AssetPipeline.PostProcessors/LODPostProcessor.cs`).

- Mètres, Z vers le haut, **avant = −Y** dans Blender ; les roues touchent z = 0.
- **Roues fixes** pour l'instant. Dans CS2, il n'y a **pas** de détection automatique des roues
  (la règle « parties à moins de 5 cm du sol » vient de CS1). Les roues qui tournent sont des **os**
  d'un maillage skinné (`ProceduralAnimationProperties.m_Bones`, types `RollingTire` /
  `SteeringTire`, `Game.Prefabs/BoneType.cs`), créés à l'import si le FBX a un squelette, réglés
  ensuite dans l'éditeur (`Game.AssetPipeline/AssetImportPipeline.cs`, l. 2527-2571).
- Export FBX `axis_forward=-Z`, `axis_up=Y`, axes et unités intégrés au maillage (voir Pièges) :
  confirmé en jeu le 05/10/2026 (taille et orientation justes).
- Aucun cycliste ni conducteur modélisé. L'objet vide `RiderAnchor` / `DriverAnchor` marque la place
  où le jeu assoit un habitant (`ActivityLocation`). Le mannequin gris des aperçus n'est pas exporté.
- Couleurs neutres : c'est le jeu qui recolore la carrosserie, le cadre et les accents. Pas de logo.
- **Recoloration à régler dans l'éditeur** : le composant **Color Properties** se met sur le
  RenderPrefab du maillage principal (pas sur les LOD), avec des « Color Sets » de 3 couleurs
  (0 = R, 1 = G, 2 = B ; « Can Be Modified By External » pour les couleurs d'entreprise). Le préréglage
  « Prefab existant » ne le copie **pas** (il est sur le RenderPrefab, pas sur l'ObjectPrefab). Selon le
  wiki, l'**alpha** de chaque couleur module métal / rugosité / vernis : une alpha à 1 peut rendre la
  carrosserie brillante quel que soit le `_MaskMap`
  ([Setting Up Color Variations](https://cs2.paradoxwikis.com/Assets:_Setting_Up_Color_Variations)).

## Pièges

- **Export FBX** : l'importeur lit les sommets bruts. Rotation −90° X et ×100 (centimètres) sont
  intégrés au maillage à l'export (`export_fbx`), comme l'add-on CS2-Exporter-for-Blender ; sinon le
  modèle arrive couché et 100 fois trop petit (vu en jeu le 05/10/2026).
- **Franges verticales sur les carrosseries** (vu dans l'éditeur le 05/10/2026, surtout de loin ;
  le camion vanilla en montre aussi un peu) :
  - **cause probable chez nous** : l'ancien atlas 512 px de 8 × 8 cases de 64 px, marge ≈ 10 px.
    Aux mips 3-4 une case ne fait plus que 8 puis 4 px : les cases voisines se mélangent, et
    l'anisotrope sur une face verticale vue en rasant étire ce mélange en **stries**. Ça touche aussi le
    `_ControlMask` et le `_MaskMap`. Corrigé : cases de 256 px, UV dans les 15 % centraux ;
  - **en plus** : `_MaskMap` trop brillant au départ (vernis 0,5 à 0,8, brillance 0,6 à 0,9, aspect
    chromé bleuté), rendu plus mat ;
  - **part du moteur** (non sourcée, plausible) : TAA, reflets écran, ombres ; à séparer en jeu en
    coupant SSR, en changeant l'AA et la qualité des ombres, et en testant l'asset **packagé** (VT).
  Les UV n'ont jamais chevauché deux cases (vérifié, LOD0 et LOD1 ; `UV_CHECK` le contrôle à chaque
  génération).

## Licence

Modèles créés par nous, sous la licence du dépôt. Les modèles Sketchfab montrés par l'humain
(Urban Arrow, EAV, bicylift, triporteurs, kei trucks) n'ont servi que de **références visuelles** :
aucune géométrie n'en est reprise.
