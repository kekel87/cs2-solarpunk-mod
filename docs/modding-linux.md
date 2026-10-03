# Modder CS2 sous Fedora (Proton)

> Recherche du 03/10/2026. **Toolchain installée le 03/10/2026** par `setup-linux-toolchain.sh`
> (toverux/HallOfFame), lancé avec `STEAM_ROOT=~/Jeux/SteamLibrary` : Entities `1.3.10`. Build du
> squelette `src/SolarpunkMod` vert (post-processing + Burst Windows/Linux/Mac, déploiement dans
> `Mods/`). Build : charger les variables (`set -a; . ~/.config/environment.d/60-cs2-modding.conf;
> set +a` en bash, ou se reconnecter) puis `dotnet build src/SolarpunkMod`.
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

Correctif local en plus du script [C], constaté au premier build : Proton 11 écrit
`ntsync: up and running.` sur stderr, et `Mod.targets` traite tout stderr comme une erreur
(`LogStandardErrorAsError`) → build en échec alors que le post-processing a réussi. Dans
`$CSII_TOOLPATH/ModPostProcessor/ModPostProcessor.sh` et `ModPublisher/ModPublisher.sh`, le filtre
devient `grep -v -e '^wineserver: ' -e '^ntsync: '`. 🔴 **Relancer le script écrase ce correctif** :
le refaire après chaque relance.

**Post-processing** : obligatoire pour ECS/Burst (génération de code Entities + Burst) [C]
(https://cs2.paradoxwikis.com/Modding_Toolchain). Un build `dotnet` brut contre `Managed/*.dll`
suffit peut-être pour du Harmony pur [S].

Fragile : une MAJ du jeu qui change la version Unity / Entities oblige à refaire le setup.

## Chemins sous Proton

- Jeu : `~/.local/share/Steam/steamapps/common/Cities Skylines II/` (DLL : `Cities2_Data/Managed`) [C]
- Données utilisateur [C] :
  `~/.local/share/Steam/steamapps/compatdata/949230/pfx/drive_c/users/steamuser/AppData/LocalLow/Colossal Order/Cities Skylines II/`
  - `Mods/` : mods locaux (préfixe `.` = désactivé) [C]. Un mod local se charge toujours mais
    **n'apparaît pas dans la liste des mods du jeu** (elle ne montre que Paradox Mods) [C, constaté
    le 03/10/2026]. Preuve de chargement : `Logs/Modding.log` (`Loaded SolarpunkMod…`) et
    `Logs/SolarpunkMod.Mod.log` (`OnLoad`)
  - `.cache/Modding/` : toolchain [C]
  - `Logs/` (un fichier par logger : `Modding.log`, `SolarpunkMod.Mod.log`…), `Player.log` [C]

**Sur la machine de l'humain** [C] : Steam **Flatpak**, jeu dans une bibliothèque secondaire.
- Racine de bibliothèque : `~/Jeux/SteamLibrary` (jeu, `compatdata/949230`, et une copie de
  Proton Experimental, préfixe `11.0-100`)
- Le script toolchain se lance donc avec `STEAM_ROOT=~/Jeux/SteamLibrary`
- Unity du jeu : `2022.3.62f2` (lu dans `UnityModsProject.zip`)

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

## Ce qu'on installe sur la machine — et comment l'enlever

Inventaire tenu à jour à chaque ajout. Abréviations :
`U=~/Jeux/SteamLibrary/steamapps/compatdata/949230/pfx` (préfixe Proton du jeu),
`D="$U/drive_c/users/steamuser/AppData/LocalLow/Colossal Order/Cities Skylines II"`.

| Quoi | Où | Désinstaller |
|------|----|--------------|
| SDK .NET 8 (paquet Fedora) | système | `sudo dnf remove dotnet-sdk-8.0` |
| Unity Hub (Flatpak) + licence Unity Personal | `~/.var/app/com.unity.UnityHub` | `flatpak uninstall --delete-data com.unity.UnityHub`, puis `flatpak uninstall --unused` (retire le runtime `org.freedesktop.Platform.Compat.i386` qu'il a tiré) ; licence : rien à faire (compte Unity gratuit, supprimable sur unity.com) |
| Éditeur Unity Linux 2022.3.62f2 | `~/Unity/Hub/Editor/2022.3.62f2` | supprimer le dossier (et `~/Unity` s'il est vide) |
| Unity 6.6 (`6000.6.4f1`), installé depuis le Hub, **inutile au mod** | `~/Unity/Hub/Editor/6000.6.4f1` | le désinstaller depuis le Hub (Installs → ⋮ → Uninstall) |
| Log du script toolchain | `~/cs2-toolchain-setup.log` | supprimer le fichier |
| Téléchargements temporaires + log | `~/.cache/cs2-modding-linux` | supprimer le dossier |
| Variables `CSII_*` | `~/.config/environment.d/60-cs2-modding.conf` | supprimer le fichier, puis se reconnecter |
| Toolchain + projet Unity des mods | `$D/.cache/Modding` | supprimer le dossier |
| Runtime .NET 6.0.36 Windows | `$U/drive_c/Program Files/dotnet` | supprimer le dossier |
| Éditeur Unity Windows 2022.3.62f2 | `$U/drive_c/Program Files/Unity 2022.3.62f2` | supprimer le dossier |
| Clés de registre du préfixe | `HKLM\SOFTWARE\Unity Technologies\Installer\Unity 2022.3.62f2`, `HKCU\Environment\CSII_UNITYVERSION` | inoffensives pour le jeu ; `wine reg delete` si on veut |
| Mod local du projet | `$D/Mods/SolarpunkMod` | supprimer le dossier |

🔴 **Ne jamais supprimer tout le préfixe** (`compatdata/949230`, ni via « supprimer les données de
compatibilité » de Steam) pour faire le ménage : **les sauvegardes et réglages du jeu sont dedans**
(`$D/Saves`). Le ménage se fait dossier par dossier, comme dans le tableau.
