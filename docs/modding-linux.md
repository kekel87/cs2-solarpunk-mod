# Modder CS2 sous Fedora (Proton)

> Recherche du 03/10/2026. **Rien n'est encore installé ni testé sur la machine.**
> [C] = confirmé par une source · [S] = supposé, à vérifier.

## Le jeu

- ProtonDB **Gold** [C] — https://www.protondb.com/app/949230. Saccades au début (cache shaders),
  UI parfois minuscule, Proton 9+ conseillé.
- Risque : l'install du launcher Paradox peut échouer (erreurs 2726 / 1603) [C].
  Contournement en option de lancement Steam [C], à vérifier en 1.6 [S] :
  `eval $( echo "%command%" | sed -E "s#Launcher/dowser.exe#Cities2.exe#g" )`
  (https://steamcommunity.com/sharedfiles/filedetails/?id=3064452556)

## Toolchain

L'installeur officiel (in-game) ne marche pas sous Linux. Procédure communautaire :

- Guide : https://github.com/CitiesSkylinesModding/agents-plugins/blob/main/plugins/cs2-modding/skills/cs2-mod-project/references/linux-toolchain.md
- Script : https://github.com/toverux/HallOfFame/blob/main/scripts/setup-linux-toolchain.sh
  (**à lire avant de lancer**)

Principe [C] :
- fichiers de `Cities2_Data/Content/Game/.ModdingToolchain` → `$CSII_USERDATAPATH/.cache/Modding` ;
- build par le **`dotnet` natif** (SDK 8+) ; seuls `ModPostProcessor` et `ModPublisher` (.exe)
  tournent sous le Wine du Proton du jeu, via des scripts `.sh` (chemins `Z:`, variables `DOTNET_*`
  retirées) ;
- dans le préfixe Proton : runtime .NET 6.0.36 Windows + éditeur Unity Windows (silencieux + clé de
  registre) ;
- éditeur Unity **Linux** de même version, lancé une fois en `-batchmode` pour générer
  `UnityModsProject/Library`.

Variables dans `~/.config/environment.d/60-cs2-modding.conf` (relogin requis) [C] :
`CSII_INSTALLATIONPATH`, `CSII_USERDATAPATH`, `CSII_MANAGEDPATH`, `CSII_MSCORLIBPATH`,
`CSII_LOCALMODSPATH`, `CSII_TOOLPATH`, `CSII_MODPOSTPROCESSORPATH`, `CSII_MODPUBLISHERPATH`,
`CSII_UNITYVERSION`, `CSII_ENTITIESVERSION`.

Correctifs obligatoires sous Linux [C] (`EnvironmentVariableTarget.User` y renvoie null) :
- `Mod.props` : retirer `, 'EnvironmentVariableTarget.User'` ;
- `.csproj` : repli sur `$(CSII_TOOLPATH)`.
- Si UI : chemins `\\` du `webpack.config.js`.

**Post-processing** : obligatoire pour ECS/Burst (génération de code Entities + Burst) [C]
(https://cs2.paradoxwikis.com/Modding_Toolchain). Un build `dotnet` brut contre `Managed/*.dll`
suffit peut-être pour du Harmony pur [S].

Fragile : une MAJ du jeu qui change la version Unity / Entities oblige à refaire le setup.

## Chemins sous Proton

- Jeu : `~/.local/share/Steam/steamapps/common/Cities Skylines II/` (DLL : `Cities2_Data/Managed`) [C]
- Données utilisateur [C] :
  `~/.local/share/Steam/steamapps/compatdata/949230/pfx/drive_c/users/steamuser/AppData/LocalLow/Colossal Order/Cities Skylines II/`
  - `Mods/` : mods locaux (préfixe `.` = désactivé) [C]
  - `.cache/Modding/` : toolchain [C]
  - `Logs/`, `Player.log` [S]

## Prefabs

Pour un bâtiment combinant des composants (ex. `GarbageFacility` + `CargoTransportStation`), le plus
simple est sans doute de **créer le prefab par code** au démarrage : cloner un prefab existant,
ajouter le composant, `PrefabSystem.AddPrefab` [S]. Exemple réel :
`ExchangeHubPrefabBootstrapSystem.cs` dans https://github.com/apavarino/MultiSkyLinesII [C].
L'éditeur d'assets sous Proton : non documenté, à tester [S].

## Dépendances de mods

- `Properties/PublishConfiguration.xml` [C] : `<Dependency Id="74417" DisplayName="Unified Icon Library" />`
  (ex. https://github.com/ST-Apps/CS2-ExtendedRoadUpgrades/blob/master/Properties/PublishConfiguration.xml)
- Ajouter le mod à une playset propose d'ajouter tout l'arbre de dépendances [C].
- Dépendance optionnelle : parcourir `GameManager.instance.modManager`, tester
  `info.asset.isMod && info.isLoaded` + nom d'assembly ; appel sans référence de compilation par
  réflexion [C] (ex. https://github.com/Rollocraft/CS2MultiplayerMod/blob/main/CS2MultiplayerMod/Game/MultiplayerService/Checks/ModsCheck.cs).
