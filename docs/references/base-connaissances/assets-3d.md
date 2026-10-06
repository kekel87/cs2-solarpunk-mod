# Assets 3D — Blender → FBX → Asset Importer

> 05/10/2026, jeu 1.6.2f1. [V] vérifié (source citée) · [S] supposé.
> Sources du dépôt : `assets/vehicles/README.md`, `assets/buildings/README.md`,
> `docs/modding-linux.md` § *Assets et import*. Recherche « import véhicules » du 05/10 intégrée
> ci-dessous (sources : code de l'importeur `Colossal.AssetPipeline/*` dans le décompilé, wiki
> Paradox *Asset Pipeline: Buildings*, *Importing*, *Assets: Setting Up Color Variations*).
> Plugin : `TECH/prefabs-and-assets/assets-and-resource-hosts.md` (côté code) ;
> `PLUGIN/cs2-modding-setup/references/mod-catalog.md` § *Extra Assets Importer*.

## Pipeline tel qu'appliqué

1. **Génération** par script Python Blender (`assets/<type>/<nom>/build.py`, kit commun
   `assets/vehicles/common/vehicle_kit.py`, `assets/buildings/common/building_kit.py`). Aucune
   géométrie tierce (dépôt public). Blender 5.2 flatpak utilisateur :
   `flatpak run --filesystem="$REPO" org.blender.Blender --background --python "$REPO/…/build.py"` [V].
2. **Export FBX baké** [V, vu en jeu 05/10] : l'importeur lit les sommets bruts → rotation **−90° X**
   et échelle **×100** intégrées au maillage, `axis_up=Y`, `axis_forward=-Z`, `FBX_SCALE_ALL`
   (comme l'add-on CS2-Exporter-for-Blender, qui fait exactement notre export). Sinon : couché et
   100× trop petit (Bounds ±0,004).
3. **Dépôt** : Steam flatpak ne voit que `~/Jeux` → `~/Jeux/cs2-import/<NomAsset>/`
   (`Z:\home\kekel\Jeux\cs2-import` côté jeu). **Nom du dossier = nom du prefab** [V recherche 05/10].
4. **Import** : éditeur (`-startEditor`) → Asset Importer → « Prefab existant dans le projet » :
   véhicule → **`EU_DeliveryVan01`** (en-tête CAR PREFAB + DELIVERY TRUCK) ; **pas**
   `MotorbikeDelivery01` (ActivityPropPrefab à squelette). Bâtiment → préréglage Building [V 05/10].
   « Existing Prefab » copie les composants de l'**ObjectPrefab** mais **pas** ceux du
   **RenderPrefab** (Color Properties, ProceduralAnimation) → à refaire à la main [V recherche 05/10].
5. **Sauvegarde éditeur** : `…/LocalLow/Colossal Order/Cities Skylines II/ImportedData/` ;
   **Package** → `.cok` dans `.packages` ; **Share** → Paradox Mods (dépendances ajoutées
   automatiquement). Asset Packs Manager : obsolète [V recherche 05/10].
6. **Côté code** : déclarer le pack d'assets en `<Dependency Id=…/>`
   (`Properties/PublishConfiguration.xml`), puis retrouver le prefab **par son nom** parmi les
   prefabs chargés. 🔴 `new PrefabID(nameof(CarPrefab), "<Nom>")` ne marche **pas** pour un asset
   importé : l'égalité de `PrefabID` compare aussi le hash de l'asset (`PrefabID.cs:45-49`)
   [V code, plan 007 lot C, 05/10 ; non recetté en jeu].
   [S] Un `.cok` posé dans le dossier du mod de code serait peut-être chargé (`FileSystemDataSource`) :
   non testé. Rappel : une dépendance déclarée ne garantit rien au runtime d'un mod de code, il
   faut détecter (`TECH/mod-compatibility/mod-compatibility.md` § *No declared dependency*) [V].

## Conventions

- **Noms** : **aucun `_`** dans le nom d'asset — l'importeur découpe sur `_`
  (`<Thème>_<Nom>_<Niveau>_<Lot>_<Module>_<LOD>_<Matériau>_<Suffixe>`, `Colossal.AssetPipeline/AssetUtils.cs`
  `ParseName`) → `SolarpunkCargoBikeBox01`, pas `SP_CargoBikeBox01` [V].
- Un seul mesh et **un seul matériau nommés comme le fichier** (ou `_Mtl`), **casse sensible** [V recherche 05/10].
- **Suffixes de textures** (`Colossal.AssetPipeline/Constants.cs`) : `_BaseColor`, `_ControlMask`,
  `_MaskMap`, `_Normal`, `_Emissive` ; LOD : `<Nom>_LOD1.fbx` [V].
- **Textures** : PNG 8 bits, **carrées, puissance de 2, 512–4096, toutes à la même résolution** ;
  LOD0 et LOD1 **partagent** les textures [V recherche 05/10].
  - `_BaseColor` : A = opacité, seuil 50 %.
  - `_MaskMap` : R métal, G vernis (coat), B inutilisé, A brillance (smoothness). Linéaire.
  - `_ControlMask` : R/G/B = masques de couleur 1/2/3, mutuellement exclusifs (multiplication),
    A = retrait de la neige. Linéaire.
  - `_Normal` : convention **OpenGL**, compressée BC7.
- **Import** : mipmaps générées (trilinéaire, anisotrope ×16, Clamp, compression BC)
  (`Colossal.AssetPipeline/Settings.cs`, `Constants.cs`) ; **Virtual Texturing appliqué seulement au
  packaging** → **l'éditeur n'est pas le rendu final** [V recherche 05/10 ; wiki *Importing*].
- **UV** dans 0..1, **marge entre îlots** (wiki *Buildings*) [V].
- **LOD** : le jeu **ne génère pas** de LOD par défaut (`LODPostProcessor` = Skip pour LOD1/LOD2, sauf
  option d'import) → fournir `_LOD1` (< 50 % des triangles du LOD0) ; LOD2 optionnel ≤ 500 triangles
  [V recherche 05/10]. Le README véhicules disait « sans LOD fourni, le jeu en génère un » : **faux**,
  à corriger dans `assets/vehicles/README.md` (hors périmètre de cette base).
- **Recoloration** : composant **Color Properties** sur le **RenderPrefab** du mesh principal ; Color
  Sets de 3 couleurs (élément 0 = R, 1 = G, 2 = B du `_ControlMask`) ; l'alpha des couleurs règle
  métal / rugosité / coat ; « Can Be Modified By External » pour les couleurs d'entreprise ou de
  ligne [V wiki *Assets: Setting Up Color Variations*, recherche 05/10]. Zone recolorable peinte en
  gris clair dans `_BaseColor` (multiplication).
- **Roues** : **pas de détection automatique en CS2** (la règle « carrosserie à plus de 5 cm du sol »
  vient de CS1) → os d'un maillage skinné déclarés dans `ProceduralAnimationProperties.m_Bones`, type
  `RollingTire` / `SteeringTire` (`BoneType.cs` ; `AssetImportPipeline.cs` ≈ 2527-2571) [V recherche 05/10].
  Nos modèles actuels n'ont pas d'os : roues fixes [S conséquence].
- **Vélos** : `BicyclePrefab` existe [V recherche 05/10] ; place du conducteur : objet vide
  `RiderAnchor` (`Biking`) / `DriverAnchor` (`Driving`), habitant assis par `ActivityLocation`
  (plan 006) [V plan 006 ; S en jeu].
- **Bâtiments** : mètres, 1 cellule = 8 × 8 m, pivot en bas au centre du lot, sol à 0, faces vers le
  bas à 0 = pas de fondation auto ; budget zoné basse densité ≈ 8 000 triangles (max 15 000) ; FBX 2018
  (wiki *Buildings*) [V]. Orientation côté rue = −Y Blender [S, à confirmer au premier import].

## Problèmes rencontrés

- **Couché et minuscule** : export non baké → corrigé (étape 2) [V 05/10].
- **Aspect chromé bleuté, reflets** : `_MaskMap` trop brillant (vernis 0,5–0,8, brillance 0,6–0,9)
  amplifiait les reflets écran du jeu → palette plus mate [V 05/10].
- **Franges / stries verticales** sur vélos et triporteur (pas le kei truck ; un peu aussi sur le
  camion vanilla dans l'éditeur). Cause probable [S, recherche 05/10] : atlas **8×8 cases de 64 px**
  à 512 px → les cases fusionnent aux mips 3-4, et l'anisotrope ×16 étire la fusion en stries.
  Correctif en cours : atlas 4×4, UV resserrées au centre des cases, textures 1024. Les UV ne
  chevauchent pas deux cases (vérifié LOD0 et LOD1). Artefacts moteur connus sur véhicules (TAA,
  ghosting, oscillation de LOD) à distinguer. Rappel : l'éditeur n'applique pas le Virtual
  Texturing → juger en jeu après packaging.

## État de la connaissance communautaire

Pas de page wiki *Asset Pipeline: Vehicles* ; la communauté CS2 est quasi muette sur l'import de
véhicules (résultats surtout CS1) [V recherche 05/10]. Ce qui précède vient du code de l'importeur
et des pages Buildings / Importing / Color Variations : traiter les points véhicule-spécifiques comme
fragiles et les confirmer en jeu.

## Bâtiment urbain : partir du tissu

- Un bâtiment destiné à s'insérer en ville (alignement sur la rue, mitoyenneté, 4-5 niveaux) se
  conçoit à partir du **tissu** et des packs de référence du jeu, pas comme une halle isolée.
  Exemple : Creator Pack **Urban Promenades** (Feindbold : 60 immeubles mixtes + 5 services urbains
  compacts alignés sur la rue ; inclus dans l'Expansion Pass Waterfronts / Ultimate Edition).
  Retour humain sur un premier grand hub 8×10 isolé : « un petit building collé aux bordures qui
  s'insère dans un groupe d'immeubles moyens » (05/10, voir decision-025 du graphe).
