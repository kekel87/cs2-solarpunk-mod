---
status: ready
created: 2026-10-05
updated: 2026-10-05
---

# Plan 005 — Dette « Fin du stationnement résidentiel »

## Ce que tu verras en jeu

- La description de la politique annonce le préavis réel, tiré de la constante du code et non plus
  écrit en dur.
- Désactiver puis réactiver la politique, même aussitôt, relance un préavis complet : aucune voiture
  n'est enlevée avant la fin du nouveau préavis.

## Contexte

Limites connues du jalon 1 bis (graphe : `implementation-fin-stationnement-jalon-1bis`) :

1. « six heures » écrit en dur dans `Policies/PolicyLocale.cs`, alors que le préavis vaut
   `ImpoundSystem.GracePeriodFrames` (`TimeSystem.kTicksPerDay / 4`).
2. `ImpoundSystem` ne tourne que toutes les 4096 frames (1/64 de jour ≈ 22 min d'horloge). Une
   désactivation suivie d'une réactivation entre deux passages passe inaperçue, et le district garde
   son ancien `StreetParkingBan.m_SinceFrame`.

Le jeu signale chaque bascule de politique par une entité événement `Event` + `Modify`
(`Game.Policies/Modify.cs` : `m_Entity`, `m_Policy`, `m_Flags`), créée par
`PoliciesUISystem.cs:241` et consommée par `ModifiedSystem` (`ModifiedSystem.cs:396`). Elle ne vit
qu'une frame.

## Décisions

- **Préavis** : le texte est construit à l'enregistrement de la locale, avec le nombre d'heures
  calculé depuis `GracePeriodFrames` (`GracePeriodFrames * 24 / kTicksPerDay`). Il n'y a plus de
  chiffre dans les chaînes, seulement un paramètre.
- **Bascule** : un nouveau système `ParkingBanToggleSystem` lit les événements `Modify` de notre
  politique, à chaque frame où il en existe (`RequireForUpdate`) :
  - activation → pose ou réinitialise `StreetParkingBan { m_SinceFrame = maintenant }` ;
  - désactivation → retire `StreetParkingBan`.
- `ImpoundSystem.UpdateBans` garde sa réconciliation périodique en filet de sécurité : district créé
  avec la politique, sauvegarde ancienne. Sa logique ne change pas.

## Étapes

1. `ImpoundSystem` (notre code, pas de Harmony) : rendre `GracePeriodFrames` `internal` et exposer `GracePeriodHours` en propriété calculée
   (déduite de `GracePeriodFrames * 24 / TimeSystem.kTicksPerDay`) pour usage depuis `PolicyLocale`.
2. `PolicyLocale` : chaînes en/fr avec `{0}` pour les heures, formatées à `Register()` depuis
   `ImpoundSystem.GracePeriodHours`.
3. `Policies/ParkingBanToggleSystem.cs` : requête `Event` + `Modify`, filtre sur
   `m_Policy == PolicyEntity` et sur un `m_Entity` portant `District`. Écriture via
   `ModificationBarrier` ou `EndFrameBarrier` (choisir selon la phase). Pour obtenir l'entité de la
   politique, ajouter `EndResidentialParkingPolicySystem.PolicyEntity` en public.
4. `Mod.cs` : `UpdateAt<ParkingBanToggleSystem>(SystemUpdatePhase.Modification4)`, la même phase
   que `ModifiedSystem` (`SystemOrder.cs:146`), qui lit le même événement.
5. `dotnet build` vert.

## Sauvegarde

Aucun changement de format : `StreetParkingBan` est inchangé. Retirer le mod reste sans dégât.

## Risques

- Durée de vie de l'événement : à confirmer qu'il existe encore en `Modification4`. C'est le cas si
  `ModifiedSystem` le lit là (`SystemOrder.cs:146`).
- Bascule via le menu debug (`DebugSystem.cs:3489`) : même événement, donc couverte.

## Recette (2 scénarios)

1. Ouvrir les politiques d'un district : la description de « Fin du stationnement résidentiel »
   annonce « 6 heures ».
2. Activer la politique, attendre environ 3 h d'horloge, la désactiver puis la réactiver aussitôt.
   Le log affiche un nouveau début de préavis, et aucune voiture n'est enlevée avant 6 h d'horloge
   après la réactivation.

## Hors périmètre

Effets rémanents au retrait du mod (`ParkingDisabled`, `CarReserveProbability`) : ils relèvent du
scénario 7 du jalon 1 bis, toujours à jouer.
