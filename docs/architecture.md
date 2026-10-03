# Architecture

> Squelette. Le projet C# n'existe pas encore : il sera généré par le template de la toolchain
> une fois celle-ci installée (voir `modding-linux.md`), puis cette page sera remplie.

## Dépôt

```
CLAUDE.md                 consignes Claude
docs/design.md            vision, modules, mods compagnons
docs/architecture.md      ce fichier
docs/modding-linux.md     toolchain Fedora/Proton
docs/references/          recherches (code du jeu, mods existants)
docs/plans/               plans en cours (NNN-nom.md)
scripts/memory/           outils du graphe de mémoire
src/                      le mod (à venir)
```

## Principe d'organisation du mod (cible)

Un seul assembly, un dossier par module (`Garbage/`, `CargoTransit/`, `Climate/`…), chacun avec
ses systèmes ECS et ses prefabs créés par code. Un point d'entrée `Mod.cs` (`IMod.OnLoad`) qui
enregistre les systèmes et détecte les mods compagnons optionnels.
