# Workflow et leçons

> Leçons de la journée du 05/10/2026 (5 plans codés, aucun recetté), complétant le workflow de
> `CLAUDE.md`. Sources : graphe de mémoire (`decision-016`, `implementation-*-plan-00x`), plans 003-009,
> plugin cs2-modding.

## 1. Chercher les pratiques établies AVANT de produire

- Premier réflexe sur un sujet technique : **invoquer la skill du plugin** concernée
  (`cs2-modding:cs2-modding`, `-ui`, `-mod-project`, `-setup`) puis ouvrir la référence indexée dans
  `README.md`. Le plugin le dit lui-même : le jeu diverge des formes ECS standard « exactement là où
  l'intuition semble sûre », et l'erreur compile, tourne et échoue en silence
  (`PLUGIN/cs2-modding/SKILL.md`) [V].
- Cas du 05/10 : l'export FBX (−90° X, ×100) et la règle « pas de `_` dans le nom » existaient déjà
  (add-on CS2-Exporter-for-Blender, `AssetUtils.ParseName`) ; on les a découverts **après** un
  import raté. Idem pour la recherche import véhicules, faite après les premiers modèles.
- Pour un problème déjà résolu par un mod : `PLUGIN/cs2-modding-setup/references/mod-catalog.md`
  et `docs/references/bonnes-pratiques-modding.md` (AdvancedTransitOperations, Road Builder, Water
  Features…) avant d'inventer.

## 2. Vérifier une hypothèse jusqu'au bout du code

- Lire **le chemin complet**, pas le premier `HasComponent` qui arrange. Exemple : « une gare peut
  vendre aux commerces » paraissait plausible (elle a un stock, `StorageCompany`) ; `ProcessResourceBuyer`
  rejette toute destination sans `PropertyRenter` (`ResourceBuyerSystem.cs:587-589`) → refonte en
  Local Hub avec vente faite par le mod (décision-016).
- Autres pièges de ce type relevés : données lues sur le prefab de l'**hôte** avant les extensions
  (`StorageCompanySystem.cs:264-269`) ; préférence **diesel** du sélecteur (`TransportVehicleSelectData.cs:460`) ;
  `Locked` sauvegardé.
- Un grep vide dans le décompilé ne prouve rien (constantes inlinées, question qui relève du bundle
  UI ou des assets) (`PLUGIN/cs2-modding/SKILL.md` § *The tree is searchable*) [V].
- Écrire dans le plan les faits avec `fichier:ligne` et [V]/[S] ; une hypothèse démentie = arrêt et
  remontée à l'humain (CLAUDE.md, dérive sérieuse).

## 3. Dev à l'aveugle → cahier de recette

- Le DLL ne se recharge pas à chaud et l'humain teste seul : tout code de simulation est **codé à
  l'aveugle** tant qu'il n'est pas recetté. Au 05/10, plans 003, 005, 007, 008 sont « build vert, NON
  recetté » → `docs/plans/recette-2026-10-05.md`.
- Chaque scénario de recette : état de départ (sauvegarde), action, résultat attendu observable, et
  **ligne de log attendue** (`Logs/SolarpunkMod.Mod.log`). Logger les décisions du mod en `Info`
  (compteurs agrégés, pas une ligne par entité).
- Ne pas empiler plus de lots non recettés : le coût de diagnostic croît avec le nombre de
  changements simultanés.
- Outils de vérification en jeu non installés au 05/10 : patch de débogage (`~/.cs2-modding/setup.md` :
  `Debug patch: (none)`), plugin `unity-devtools`, Scene Explorer. Les installer ferait passer des
  [S] en [V] sans attendre l'humain (`PLUGIN/cs2-modding-setup/references/debug-patching-linux.md`).

## 4. Agents en parallèle

- Deux agents seulement si leurs **écritures sont disjointes** (CLAUDE.md) ; le prompt de chacun dit
  ce qu'il ne touche pas.
- **Un seul `dotnet build` à la fois** : le build vide et redéploie `Mods/SolarpunkMod` (`DeployWIP`,
  puis `BuildUI`) et lance le post-processeur sous Proton ; deux builds concurrents se marchent dessus.
- Lecture seule (audit, recherche web) : parallélisable sans risque.

## 5. Utiliser les skills cs2-modding

| Moment | Skill |
|---|---|
| Écrire / déboguer du code de simulation, prefab, sauvegarde | `cs2-modding:cs2-modding` |
| Panneau, section, binding, build UI | `cs2-modding:cs2-modding-ui` |
| Build qui casse, mod absent du jeu, publication | `cs2-modding:cs2-mod-project` |
| Mise à jour du jeu, re-décompiler, mods à lire | `cs2-modding:cs2-modding-setup` |

## 6. Vocabulaire et traçabilité

- Revue : « aucune anomalie détectée par les règles connues, non exhaustif », jamais « tout est bon ».
- Toute affirmation technique d'un plan ou d'un doc : source (`fichier:ligne`, chemin du plugin ou
  URL) + [V]/[S] + date. Un [S] qui se confirme en recette passe [V] avec la date de recette.
- Écart doc / code à signaler quand on le voit (ex. 05/10 : le plan 009 parle de
  `FreightPlatformSystem`, le code s'appelle `StationPlatformSystem` ; `assets/vehicles/README.md`
  affirme que le jeu génère un LOD par défaut, la recherche du 05/10 dit le contraire).

## 7. Cahier de recette

- Tenir `docs/plans/recette-<date>.md` à jour **à chaque lot** (sessions 0 à 8 + 6 bis le 05/10),
  pas en fin de journée : la compaction du contexte fait perdre ce qui n'est pas consigné.
- Mémoriser au graphe au fil de l'eau, pas seulement en fin de session.

## Build en parallèle (05/10/2026)

Plusieurs agents qui compilent : `flock /tmp/claude-1000/solarpunk-build.lock dotnet build
-p:UseSharedCompilation=false`. Sans cette option, le serveur de compilation `VBCSCompiler` reste en
vie, garde le verrou et bloque les builds suivants (débloquer : `dotnet build-server shutdown`).
