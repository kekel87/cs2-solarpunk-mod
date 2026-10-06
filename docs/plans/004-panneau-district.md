---
status: ready
created: 2026-10-05
updated: 2026-10-05
---

# Plan 004 — Panneau par district

## Ce que tu verras en jeu

- Tu cliques sur un district : son panneau d'info vanilla affiche, en plus, une section « Solarpunk ».
- On y trouve la part modale du district (voiture / vélo / TC / à pied), ainsi que les voitures, camions de livraison et bennes qui y circulent.
- La part voiture et les camions s'affichent en vert, orange ou rouge selon des seuils.
- Une petite courbe montre l'évolution de la part voiture et des camions depuis que le panneau est ouvert. Elle repart de zéro au rechargement.
- Retirer le mod : la partie charge normalement, rien n'a été écrit dans la sauvegarde.

## Contexte

Le panneau ville (jalon 1, `src/SolarpunkMod/Info/`) ne mesure que la ville entière. Or le jalon 2,
« Quartier sans camion », a besoin de prouver qu'**un district** est livré sans camion. Ce plan est
l'instrument de mesure des lots suivants (hub, petits véhicules). Le projet ne contient encore
aucun graphique.

## Vérifié dans le code du jeu (1.6.2f1)

- **Ajouter une section** : `SelectedInfoUISystem.AddMiddleSection(ISectionSource)`
  (`Game.UI.InGame/SelectedInfoUISystem.cs:207`). La section hérite d'`InfoSectionBase`
  (`InfoSectionBase.cs:14`) : `group` abstrait (`:38`), `visible`, `Reset`, `OnProcess` et
  `OnWriteProperties` (`:78-82`). Le JSON écrit contient `group` (`:128`).
  - Modèle à suivre : `AverageHappinessSection` s'affiche si l'entité sélectionnée a `District` et
    `Area` (`AverageHappinessSection.cs:546`).
  - `DistrictsSection` ne convient pas : elle sert aux districts de **service** d'un bâtiment.
- **Côté UI** : la table `selectedInfoSectionComponents` du module
  `game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx`
  est extensible (`ui/index.js:127537-127543`). On l'étend avec `moduleRegistry.extend`, clé = notre
  `group`.
- **Rafraîchissement** : les sections sont recalculées par `SetBindingsDirty` puis `UpdateSections`
  (`SelectedInfoUISystem.cs:463-516`).
- **Graphiques** : le bundle du jeu enregistre Chart.js. Les valeurs par défaut sont posées à
  `ui/index.js:65571-65580` : animations, tooltip et légende désactivés. Le jeu expose le composant
  `ResponsiveChart({ type, data, options })` dans le module
  `game-ui/common/charts/responsive-chart/responsive-chart.tsx` (`ui/index.js:65440, 65581`).
  - Un mod y accède par `getModule(…, "ResponsiveChart")`, sans embarquer Chart.js.
  - La liste exacte des contrôleurs enregistrés est minifiée. Le type `line` est probable, puisque
    `StatisticsGraph` l'utilise [S].
- **District d'une entité** :
  - Bâtiments : `CurrentDistrict.m_District` (`Game.Areas/CurrentDistrict.cs:8`).
  - Routes : `BorderDistrict.m_Left` / `m_Right` (`Game.Areas/BorderDistrict.cs:8-10`). Toute route
    le porte (`Game.Prefabs/RoadPrefab.cs:64`), maintenu par `CurrentDistrictSystem.cs:206-234`.
  - Véhicules et piétons n'ont pas de district : on remonte par leur voie
    (`CarCurrentLane.m_Lane` / `HumanCurrentLane.m_Lane`), puis `Owner`, puis l'arête
    (`BorderDistrict`) ou le bâtiment (`CurrentDistrict`) [S : chaîne `Owner` à confirmer sur les
    sous-voies].

## Décisions

1. **Ce qu'on compte** : le **trafic présent dans le district** (véhicules et personnes sur une voie
   du district). Une route frontière compte pour les deux districts qu'elle borde.
2. **Mesures transposées** :

   | Mesure du panneau ville | District |
   |---|---|
   | Part modale voiture / vélo / TC / à pied | ✅ personnes dont la voie (ou celle du véhicule) est dans le district |
   | Voitures particulières | ✅ |
   | Camions de livraison | ✅ (le chiffre clé du jalon 2) |
   | Bennes | ✅ |
   | Trains de marchandises | ❌ les voies ferrées n'ont pas de `BorderDistrict` (n'héritent pas de `RoadPrefab`) |
   | Part renouvelable | ❌ peu de sens à l'échelle d'un district |
3. **Couleurs** : vert, orange ou rouge pour la part voiture et pour les camions de livraison
   seulement. Les autres chiffres restent neutres (un nombre de voitures dépend de la taille du
   district). Seuils (décision de l'humain) :
   - part voiture : vert ≤ 20 %, orange 20-40 %, rouge > 40 % ;
   - camions de livraison : vert = 0, orange 1-2, rouge ≥ 3.
4. **Courbe** : part voiture et camions de livraison, un point toutes les 128 frames de simulation,
   60 points max (fenêtre glissante). Historique gardé en mémoire **pour la session et le district
   sélectionné seulement**, remis à zéro quand on change de district. Rien n'est sauvegardé.
5. **Calcul** : seulement quand un district est sélectionné, même cadence et même logique de part
   modale que le panneau ville (`DummyTraffic` et `Hangaround` exclus, attente de taxi = voiture).
6. **Option non imposée** : le panneau ville peut gagner la même courbe à faible coût (même composant
   UI, même tampon). À trancher par l'humain ; hors périmètre par défaut.

## Étapes de dev

1. `Info/ModalCounting.cs` : extraire de `SolarpunkInfoUISystem` le comptage modal par résident
   (`SampleModalCounts`, `IsWaitingForTaxi`, `ModalCounts`) sous forme réutilisable, avec un filtre
   optionnel « voie dans le district X ». Le panneau ville l'appelle sans filtre. Comportement ville
   inchangé.
2. `Info/DistrictLookup.cs` : résolution voie → district (`Owner`, `BorderDistrict`,
   `CurrentDistrict`), test `IsInDistrict(lane, district)`.
3. `Info/SolarpunkDistrictSection.cs` : `InfoSectionBase`, `group = "SolarpunkMod.DistrictSection"`.
   - Visible si l'entité sélectionnée a `District` et `Area`.
   - Échantillonne toutes les 128 frames (`UIUpdateState`) : comptages, moyenne glissante sur
     10 échantillons, puis tampon de courbe de 60 points.
   - `OnWriteProperties` écrit les parts, les comptes et les deux séries.
   - Remise à zéro sur changement d'entité sélectionnée.
4. `Mod.cs` : enregistrer la section via
   `World.GetOrCreateSystemManaged<SelectedInfoUISystem>().AddMiddleSection(...)`.
5. `Info/CityInfoLocale.cs` : clés de la section (titre, libellés, infobulles en/fr) en réutilisant
   les clés existantes quand le texte est identique.
6. UI :
   - `UI/src/district/district-section.tsx` : rendu avec les lignes `InfoRow` vanilla, couleur selon
     seuils (`district-thresholds.ts`) et courbe via `ResponsiveChart`.
   - `UI/src/index.tsx` : `moduleRegistry.extend(…selected-info-sections.tsx, "selectedInfoSectionComponents", …)`.
7. `dotnet build` vert, puis `/simplify`.

## Sauvegarde

Aucun composant ajouté, aucune donnée sérialisée. L'historique vit en mémoire de la section.
Retirer le mod : rien à nettoyer.

## Performance

- Un passage sur tous les résidents (comme le panneau ville), plus trois requêtes de véhicules,
  toutes les 128 frames et seulement district sélectionné.
- Le surcoût vient des lookups `Owner` / `BorderDistrict` par entité. C'est acceptable pour une
  lecture ponctuelle.
- Si le panneau ville est ouvert en même temps, les deux comptent séparément : coût doublé,
  acceptable, mutualisation hors périmètre.

## Risques et hypothèses à vérifier en jeu

- [S] La clé de `selectedInfoSectionComponents` est bien le `group` écrit, et l'extension par un mod
  est prise en compte sans recharger l'UI.
- [S] `ResponsiveChart` accepte `type: "line"` (contrôleur enregistré par le jeu). Repli : barres
  CSS simples, sans Chart.js.
- [S] La chaîne `voie → Owner → arête` couvre les trottoirs et les voies de stationnement. Les
  piétons dans un bâtiment remontent via `CurrentDistrict`.
- Une route frontière compte dans deux districts : chiffres légèrement gonflés aux bords. C'est
  assumé et dit dans l'infobulle.
- La part « à pied » reste gonflée (limite connue du jalon 1).

## Recette (6 scénarios)

1. Clic sur un district : la section « Solarpunk » apparaît sous les sections vanilla, avec des
   chiffres non nuls.
2. Clic sur un bâtiment, une route, puis un autre district : la section n'apparaît que pour un
   district, et les chiffres changent d'un district à l'autre.
3. Couleurs : un district résidentiel très motorisé s'affiche en rouge ou orange ; un district
   piéton ou vide en vert.
4. Courbe : après quelques minutes panneau ouvert, la courbe se remplit ; changer de district la
   remet à zéro.
5. Panneau ville ouvert en même temps : ses chiffres restent cohérents avec la recette du jalon 1
   (non régressé par l'extraction du comptage).
6. Sauvegarder, quitter, retirer le mod, recharger : la partie charge sans erreur.

## Hors périmètre

- Courbes sur le panneau ville (option, décision 6).
- Historique persistant entre sessions (demanderait d'écrire dans la sauvegarde).
- Mesure par habitants du district (où ils habitent) au lieu du trafic présent.
- Trains de marchandises et part renouvelable par district.

## Ajustements après revue game-designer (05/10/2026)

- **Camions normalisés** : compte affiché brut, couleur sur **camions par km de voie du district**
  (somme des longueurs des arêtes du district). Départ : vert < 0,1/km, orange < 0,5/km, rouge ≥ 0,5/km
  (estimation à calibrer ce soir : le log écrit la valeur par km à chaque échantillon).
- **Seuils sur la moyenne glissante** avec hystérésis ±10 % pour éviter le clignotement.
- **Effectif minimum** : moins de 30 personnes sur les voies du district → part modale en gris
  « pas assez de données ».
- Indicateur « camions qui **livrent** le district » (destination dans le district) : jalon 2, pas ici.

## Écarts après audit du 05/10

- Groupe de la section préfixé `SolarpunkMod.` ; la clé UI reste le nom complet du type C# (lien commenté des deux côtés).

## Écart après revue de code (05/10/2026, soir)

- **Appartenance au district** : `DistrictLocator` teste maintenant la position du milieu de la voie dans
  les triangles de la zone du district (comme `CurrentDistrictSystem` pour les bâtiments). Carrefours,
  chemins, pistes cyclables et voies ferrées comptent ; une route de bordure ne compte plus pour les
  deux districts (chaque voie tombe d'un côté). La longueur de route au km compte toujours les routes
  de bordure des deux côtés.
- Échantillonnage lié à `SimulationSystem.frameIndex` (aucune mesure en pause) ; camions et petits
  véhicules du hub séparés (`Info/DeliveryVehicleCount.cs`).
