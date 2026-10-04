# Énergie, chaleur et politiques vertes

> Recherche du 04/10/2026. **[V]** vérifié (source) · **[S]** de mémoire, à confirmer.
> Chiffres CS2 issus de guides 1.2 : **à revérifier dans les DLL 1.6**.

## CS1 — ce que les DLC faisaient

- **Green Cities** [V] ([wiki](https://skylines.paradoxwikis.com/Green_Cities)) :
  - politiques de district *Electric Cars* (bruit ≈ -50 %), *Combustion Engine Ban* (électrique et
    service seulement), *Filter Industrial Waste* ([wiki](https://skylines.paradoxwikis.com/Policies)) ;
  - spécialisations : **Self-sufficient buildings** (résidentiel : -30 % déchets, électricité,
    impôts, toits solaires visuels — [source](https://guidestrats.com/cities-skylines-self-sufficient-buildings/)),
    *Organic & local produce* (commerce, -50 % camions, sans bruit), *IT cluster* (bureaux) ;
  - service : géothermie, OTEC, Solar Updraft Tower, Eco Water Outlet / Treatment, Recycling
    Center, collecteur de déchets flottant ; bus et benne au biocarburant.
- **Snowfall** [S] : réseau de **chauffage** (tuyaux, chaudière au fioul, géothermie de chauffage),
  bâtiments non raccordés = surconsommation électrique en hiver, routes chauffantes. **Aucune
  climatisation** dans CS1.
- Autres [V] : *Encourage Biking* (After Dark), pêche durable (Sunset Harbor), *Recycle Garbage*
  (Parklife). [S] *Old Town* (transit voiture bloqué), Recycling, Power Usage Awareness.
- **Gaz de ville** : absent de CS1 et CS2.

## CS2 vanilla 1.6

- Production : éolien, solaire (160 MW, batterie 50 MWh, amélioration *Backup Battery*),
  géothermie, hydro, charbon, gaz, nucléaire, incinérateur ; *Emergency Battery Station*
  ([guide 1.2](https://steamcommunity.com/sharedfiles/filedetails/?id=3068846564)).
- Politiques ([wiki](https://cs2.paradoxwikis.com/Policies), probablement daté) : *Energy
  Consumption Awareness* (district, -5 % électricité), *Recycling*, *Combustion Engine Ban*,
  *Heavy Traffic Ban*… ; ville : *Advanced Pollution Management*.
- **Bâtiments zonés** : ni production, ni extension. Pas d'équivalent « self-sufficient ».
- **Chauffage** : la température module la conso électrique ville entière ;
  `GetHeatingMultiplier` alimente un champ que rien ne lit (`capacites-vanilla.md`). Effet de la
  chaleur (clim) : à vérifier dans le décompilé.

## Mods CS2 (recherche API Paradox Mods, non exhaustive)

| ID | Mod | Contenu | 1.6 |
|---|---|---|---|
| 157287 | Battery Regulation System | Charge/décharge des batteries en parallèle | oui |
| 150364 | More Parkings (NEW) | Lot « Solar Panel 2x2 » qui produit | oui |
| 125568 | Waste Collection Center | Asset de tri visuel | oui |
| 159757 | Urban Industrial District Pack | Industrie urbaine peu polluante | oui |
| 155720 | TaxRateTweak | Preuve : ajouter des politiques par code mod | — |

Rien sur : solaire en toiture fonctionnel, réseau de chaleur, politiques vertes, gaz.

**Part modale** : InfoLoom (91433, maintenu 1.6.2) **ne l'affiche pas** (démographie, emploi,
demande, districts). Transit Scope (139959) = composition du trafic par route ; Vehicle Use
(150568) = remplissage des véhicules. Aucun ne donne la part modale ville → le panneau du mod est
légitime.

## Idées réelles

| Idée | Précédent | Dans le mod |
|---|---|---|
| Solaire de balcon | Allemagne, 800 W légal depuis 05/2024, ≈ 1 M d'installations [V] ([wiki](https://en.wikipedia.org/wiki/Balcony_solar_power)) | Politique de district bon marché |
| Rénovation Energiesprong | Pays-Bas, > 5 000 logements, -78 % d'énergie à Utrecht [V] ([source](https://www.energiesprong.org/post/the-netherlands-utrecht-overvecht-net-zero-retrofit)) | Politique : conversion progressive, creux de chantier |
| Stockage solaire saisonnier | Drake Landing, 97 % du chauffage [V] ([wiki](https://en.wikipedia.org/wiki/Drake_Landing_Solar_Community)) | Bâtiment été → hiver |
| Réseau d'anergie (chaud et froid) | ETH Hönggerberg depuis 2010 [V] ([ETH](https://ethambassadors.ethz.ch/2026/07/16/the-energy-of-tomorrow-discovering-eth-zurichs-hidden-energy-network/)) | Réseau de chaleur 5ᵉ génération |
| Micro-hydro en conduite | Portland LucidPipe, 4 × 50 kW [V] ([source](https://newatlas.com/portland-lucidpipe-power-system/36130/)) | Amélioration de station de pompage |
| Solaire flottant | O'MEGA1, Piolenc, 17 MWc [V] ([source](https://www.akuoenergy.com/en/akuo-in-the-world/all-our-projects/omega1)) | Centrale solaire sur l'eau |
| Agrivoltaïsme | Loi APER (France) [S] | Amélioration de ferme |
| Autoconsommation collective | Droit français [S] | Variante de quartier de la coopérative |
| Clim individuelle qui réchauffe la rue | Paris, ≈ +0,5 à 2 °C [S] | Cercle vicieux de la carte de chaleur |

## Gaz de ville et chaudières

Très répandu dans le monde réel, absent des city builders [S] :
- Part des logements chauffés au gaz : Pays-Bas ≈ 90 %, Royaume-Uni ≈ 85 %, États-Unis ≈ 50 %,
  France ≈ 40 % [S, à sourcer]. Le chauffage = une grosse part des émissions d'une ville.
- **Sortie du gaz** : Pays-Bas — séisme de Groningue, plus de raccordement gaz pour le neuf depuis
  2018, conversion **quartier par quartier** (*wijkaanpak*) [S] ; Allemagne — loi chauffage 2024
  (65 % renouvelable) [S] ; Royaume-Uni — fin des chaudières gaz dans le neuf [S].
- **Remplacements** : pompe à chaleur, réseau de chaleur, biométhane issu des déchets
  (méthaniseur). **Hydrogène pour le chauffage = hype** : essais de Whitby et Redcar abandonnés
  (2023) [S] → Chirper anti-hype.
