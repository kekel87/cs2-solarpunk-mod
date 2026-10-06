---
name: recette
description: Mode interactif de recette humaine en jeu — tout le cahier affiché d'un coup, joué et validé par l'humain dans l'ordre qu'il veut. À invoquer quand l'humain répond `oui` à l'arrêt 1 (« Tu testes ? »).
---

**Tout le cahier d'un coup** (demande de l'humain, 05/10/2026) : il voit ce qu'il doit valider et
va plus vite ; il répond scénario par scénario, dans l'ordre qu'il veut.

1. **Avant de te déranger** : build Release vert, mod déployé dans le dossier `Mods/` local
   (chemins : `docs/modding-linux.md`), et le log du mod relu après le dernier lancement — pas la
   peine de te faire lancer le jeu sur un mod qui plante au chargement.
2. Analyse `git diff HEAD` → scénarios observables en jeu.
3. **Tous les scénarios en un seul message**, numérotés, chacun en 3 lignes courtes :
   - **Préparation** : sauvegarde à charger ou ville minimale à poser (le moins d'étapes possible) ;
   - **Quoi faire** : 1-2 lignes ;
   - **Résultat attendu** : ce que tu dois voir (bâtiment, ligne, train, chiffre d'une info-vue).
   Puis pause. Tu joues.
4. Ta réponse par numéro (`2 ok`, `3 ❌ …`). Bug ou retour → je lis les logs (`Player.log`, logs du
   mod), je corrige ou je note, et je réaffiche seulement ce qui reste à valider.
5. Tous validés → arrêt 2 (skill `/menu`).
