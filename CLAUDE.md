# CLAUDE.md

## Projet

**Solarpunk Mod** : code mod C# pour **Cities: Skylines II** (Unity DOTS/ECS, Harmony), publié sur
Paradox Mods. Un seul mod, qui s'appuie sur les mods existants comme dépendances.

Cap : une ville **sans voiture ni camion** (fret par rail, cargo tram / métro de marchandises,
déchets par train, vélo-cargo au dernier kilomètre), puis le **climat urbain** (chaleur, eaux
pluviales, renaturation). Le nom est volontairement large : chaque thème = un module du même mod.

## Humain

**Ne code pas en C#/Unity.** Directeur créatif, architecte, reviewer. Dev web Angular/TS
expérimenté, clean code, temps limité. Joue sous **Fedora Linux** (CS2 via Proton).
Continuité : peut revenir après des semaines → graphe de mémoire et `docs/plans/` à jour.

Claude = dev principal, autonome sur l'implémentation, valide le design avec l'humain.

## Docs — quoi lire quand

| Où lire | Trigger |
|---------|---------|
| **Graphe de mémoire** — `node scripts/memory/query.mjs "mots clés"` | Reprise (« on en était où ? »), décisions passées, dette, agenda, retours de l'humain. 2-4 mots-clés, jamais une phrase ; `--open <entité>` pour le détail ; `--stats` pour l'inventaire. Pointeur de reprise : `--open agenda-prochaine-etape-courante` |
| `docs/design.md` | Vision, modules, ce que fait chaque mod compagnon. Avant toute mécanique |
| `docs/architecture.md` | Avant créer un fichier/projet, changer la structure |
| `docs/modding-linux.md` | Toolchain sous Fedora/Proton, chemins, build, logs |
| `docs/references/` | Recherches : code décompilé du jeu, mods existants |
| `docs/plans/` | Plan en cours avant coder |

Pas tout charger. Lire le fichier pertinent au moment pertinent.

## Principes

- **Lire le code du jeu avant de supposer.** Les DLL de `Cities2_Data/Managed` font foi ; une
  hypothèse sur la sim se vérifie dans le code décompilé, puis en jeu
- **Réutiliser la sim vanilla** : brancher sur les systèmes existants (ressources, dispatch,
  lignes cargo) plutôt que les réécrire. Harmony en dernier recours, patch minimal
- **Dépendre plutôt que refaire** : si un mod existant fait le travail, on le déclare en
  dépendance (`Properties/PublishConfiguration.xml`, balise `<Dependency Id=… />`) ; dépendance
  optionnelle = détection au runtime via `GameManager.instance.modManager`
- **Sauvegarde sacrée** : retirer le mod ne doit jamais corrompre une partie
- **Petit, incrémental** : 1 changement = 1 chose. Pas de sur-ingénierie

## Conventions

- **Commits** : conventional commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`) — **titre
  seul**, court. **1 scope max**, sinon aucun. Détails → graphe ou plan en cours
- **Langue** : code anglais, doc et communication français
- **Nommage** : pas d'abréviations (`vehicleCapacity` pas `vehCap`)
- **Écriture code** : Edit > Write. Petits Edit successifs
- **Code mort** : zéro tolérance
- **Plans** : `docs/plans/NNN-nom.md` numérotés, statut en en-tête

## Interdits

- **Git** : commit/add/push autorisés sans validation du message, mais seulement au menu de
  finalisation. Destructeurs interdits (checkout, reset, restore, clean, rm, branch -d, tag -d) —
  hook `block-forbidden-commands.sh`. `git rebase` et `git merge --ff-only` autorisés
- **Infra** : pas d'install globale (`dotnet tool install -g`, `npm -g`…), pas de `sudo`
- **Structurel** : consulter l'humain AVANT de toucher au `.csproj`, aux dépendances de mods, à la
  structure du dépôt
- **Assets** : rien de non libre de droits dans le dépôt (il est public)
- **Mémoire** : recherches/décisions → graphe ou `docs/`, pas la mémoire auto de Claude

## Agents

Tu lances, l'humain ne demande pas : `code-reviewer`, `plan-reviewer`, `doc-keeper`,
`game-designer` (équilibrage de la sim), `commit-message`. `session-closer` : seulement à la fin
(« fin », `/status`).

Deux agents en parallèle seulement si leurs ensembles d'écriture sont **disjoints** ; le prompt de
chacun dit ce qu'il ne doit pas toucher.

## Le workflow — DEUX arrêts, pas un de plus

Entre le moment où un choix est fait et le moment où l'humain teste, Claude ne s'arrête **jamais**.

1. **`/next`** — résumé court, 2-3 candidats, **une recommandation**. Un choix issu d'une
   discussion suit **exactement** le même chemin : on enchaîne sur le plan, jamais sur le code.
2. **Plan** — `docs/plans/NNN-nom.md`, avec en tête **« Ce que tu verras en jeu »** (3-5 lignes du
   résultat observable). 🔴 Ces lignes s'affichent **en chat** avec un seul `AskUserQuestion`
   (`plan-reviewer` coché ; `game-designer` si mécanique de sim). Jamais d'option « commiter le plan ».
3. **Dev d'un trait** — tout le plan, sans point d'étape : code, `/simplify` en fin de dev, build
   (`dotnet build`) vert. **Seule exception** : dérive sérieuse du plan (hypothèse démentie par le
   code du jeu, dépendance nouvelle) → s'arrêter et remonter.
4. **ARRÊT 1 — « Tu testes ? »** — résumé court + **une** question oui/non, avec le **nombre de
   scénarios** (jamais de durée). `oui` → skill `/recette`.
5. **ARRÊT 2 — le menu** — skill `/menu` : commit WIP, `code-reviewer` ∥ `/code-review`, puis
   multi-select (`doc-keeper`, `build + commit`).

**Trouvailles en route** : besoin d'une décision de l'humain → on en parle tout de suite ; sinon
Claude avance. 🔴 **Rien au backlog ni à l'agenda du graphe sans accord explicite** (hook
`block-backlog-write.py` ; sortie de clôture : préfixe `SP_CLOTURE=1`).

**Exceptions** : changement purement config/doc → pas de recette, commit direct.

**Vocabulaire des revues** : jamais « ✅ tout est bon » — « aucune anomalie détectée par les règles
connues, non exhaustif ».
