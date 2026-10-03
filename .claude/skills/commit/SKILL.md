---
name: commit
description: Génère un message de commit conventional court (titre seul) via l'agent commit-message, puis commit + push directement. Pas de validation du message.
user-invocable: true
---

1. `git diff --stat HEAD` et `git status --porcelain` — rien à commiter → stop.
2. Si du code C# a changé : `dotnet build` doit être vert dans le tour, sinon le lancer d'abord.
3. Agent `commit-message` (contexte de session dans le prompt : plan en cours, résumé).
4. `git add` + `git commit -m "<message>"` + `git push`. Stop sur fail.

## Règles

- 🔴 Pas de validation du message : commit + push directement.
- Titre seul, court, conventional commits. **1 scope max**, sinon aucun.
- Changements trop variés → plusieurs commits logiques.
- Destructeurs interdits (deny-list). `git rebase` autorisé.

Une ligne en chat après coup : `Commité + pushé : feat(garbage): add rail garbage hub prefab`
