---
name: menu
description: Affiche le menu de finalisation (multi-select des étapes de chaîne) à tout moment. Arrêt 2 du workflow de CLAUDE.md.
user-invocable: true
---

Pop le menu de finalisation **maintenant**. Déclencheurs : `/menu`, ou le mot **`menu`** seul.

⚠️ Arrêt 2 du workflow. Si la recette humaine n'a pas eu lieu et que le changement est observable
en jeu, pose d'abord l'arrêt 1 (« Tu testes ? ») — sauf si l'humain demande le menu explicitement.

## Étapes

1. `git status --porcelain` et `git diff --name-only HEAD`. Rien → préviens, propose le menu réduit.
2. **Sans rien demander** :
   - **commit WIP** si des changements non commités existent (point de restauration) ;
   - en parallèle (lecture seule) : `code-reviewer` en arrière-plan, `/code-review` dans le tour.
   Corriger les bloquants avant de continuer.
3. **Un seul** `AskUserQuestion`, une question multiSelect :

   | Option | Pré-coché si |
   |--------|--------------|
   | `doc-keeper` | `docs/` impactés, décision ou fait à consigner au graphe, nouvelle mécanique |
   | `build + commit` | **toujours** — `dotnet build` (Release, post-process compris) vert, puis commit + push |

   - **Plan en rédaction** : menu remplacé par `[x] plan-reviewer`, `[ ] game-designer` (si
     mécanique de sim). 🔴 Jamais d'option « commiter le plan ».
   - **Fin de session** (« fin », `/status`) : ajoute `session-closer`.
4. Ordre fixe : `doc-keeper → [résumé du diff WIP→final] → build → commit + push (amende le WIP)`.
5. **Stop sur fail bloquant** (revue Critical, build rouge).

## Résumé du diff WIP→final

Si la chaîne a retouché du code (correction de revue), montrer en 3 lignes ce qu'elle a changé
(`git diff <commit WIP>..`) et **laisser l'humain décider** s'il veut y jeter un œil.

## Règles

- Commit + push **directement**, sans proposer le message.
- Jamais « ✅ tout est bon » : « aucune anomalie détectée par les règles connues, non exhaustif ».
- 🔴 Rien au backlog / à l'agenda du graphe sans accord explicite de l'humain.
