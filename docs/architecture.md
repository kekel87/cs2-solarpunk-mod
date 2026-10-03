# Architecture

> Projet C# généré le 03/10/2026 par le template officiel du jeu (`ColossalOrder.ModTemplate`,
> sans écran d'options), import `Mod.props`/`Mod.targets` via `$(CSII_TOOLPATH)` (correctif Linux).
> Vignette du template non reprise (dépôt public) : `Properties/Thumbnail.png` à créer avant publication.
> `PublishConfiguration.xml` : `AccessLevel` Private tant que le mod n'est pas prêt ; balises vides retirées.

## Dépôt

```
CLAUDE.md                 consignes Claude
docs/design.md            vision, modules, mods compagnons
docs/architecture.md      ce fichier
docs/modding-linux.md     toolchain Fedora/Proton
docs/references/          recherches (code du jeu, mods existants)
docs/plans/               plans en cours (NNN-nom.md)
scripts/memory/           outils du graphe de mémoire
src/SolarpunkMod/         le mod : SolarpunkMod.csproj, Mod.cs (IMod), Properties/ (publication Paradox Mods)
```

## Principe d'organisation du mod (cible)

Un seul assembly, un dossier par module (`Garbage/`, `CargoTransit/`, `Climate/`…), chacun avec
ses systèmes ECS et ses prefabs créés par code. Un point d'entrée `Mod.cs` (`IMod.OnLoad`) qui
enregistre les systèmes et détecte les mods compagnons optionnels.
