---
name: recette
description: Mode interactif de recette humaine en jeu — un scénario à la fois, préparé par Claude, joué et validé par l'humain. À invoquer quand l'humain répond `oui` à l'arrêt 1 (« Tu testes ? »).
---

Je ne dump pas tout : **un scénario à la fois**, je prépare, tu joues, tu valides.

1. **Avant de te déranger** : build Release vert, mod déployé dans le dossier `Mods/` local
   (chemins : `docs/modding-linux.md`), et le log du mod relu après le dernier lancement — pas la
   peine de te faire lancer le jeu sur un mod qui plante au chargement.
2. Analyse `git diff HEAD` → scénarios observables en jeu.
3. **Par scénario**, en chat :
   - **Préparation** : sauvegarde à charger ou ville minimale à poser (le moins d'étapes possible) ;
   - **Quoi faire** : 1-2 lignes ;
   - **Résultat attendu** : ce que tu dois voir (bâtiment, ligne, train, chiffre d'une info-vue).
   Puis pause. Tu joues.
4. Ta réponse : `ok`/`suivant` → scénario suivant. Bug ou retour → je lis les logs
   (`Player.log`, logs du mod), on traite avant de continuer.
5. Tous validés → arrêt 2 (skill `/menu`).
