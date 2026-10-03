---
name: next
description: Reconstitue l'état du projet depuis le graphe de mémoire, propose 2-3 candidats et une recommandation.
---

**D'abord** : `git fetch origin`, puis compare `main`, `origin/main` et les branches locales — ne
jamais repartir d'un `main` en retard.

## L'état vient du graphe de mémoire

### 🔴 Commence TOUJOURS par le pointeur, jamais par une recherche

```bash
node scripts/memory/query.mjs --open agenda-prochaine-etape-courante
```

Nom stable, réécrit à chaque fin de session : c'est le seul endroit qui dit où on en est. « Le plus
récent » n'est pas un mot, aucune recherche plein-texte ne le trouvera.

### Ensuite seulement, pour creuser

2 à 4 mots-clés distinctifs, jamais une phrase :

```bash
node scripts/memory/query.mjs --stats
node scripts/memory/query.mjs "déchets rail dispatch"
node scripts/memory/query.mjs --open <nom-d-entité>
```

🔴 Le mode recherche **tronque** : dès qu'une entrée compte, relis-la avec `--open`.

Puis, si le sujet l'exige : `docs/design.md` (modules) et le plan en cours dans `docs/plans/`.

## Présente

1. **À faire maintenant** — 2-3 candidats, une ligne chacun, puis **une recommandation**.
2. **Reporté** — entités `agenda` / `backlog` ouvertes. Rien → le dire.
3. **Fait récemment** — 3 à 5 items, croisés avec `git log -5`.
4. **Bloquants** — questions à trancher avant de démarrer.

Concis : 10-15 lignes.

## Après le choix

Confirmation directe ou choix issu d'une discussion : **même suite**. Plan rédigé
(`docs/plans/NNN-nom.md`) avec « Ce que tu verras en jeu » affiché **en chat**, puis le menu de plan.
Ne jamais partir coder directement. Détail : « Le workflow » dans `CLAUDE.md`.
