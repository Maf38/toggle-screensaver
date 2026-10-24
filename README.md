# Toggle Screensaver

Petite application WPF (.NET 8, Windows) pour activer/désactiver la mise en veille
de l'écran et du système en un clic.

![Marmotte endormie](sleep.png)

## Télécharger

[**⬇ Télécharger l'installeur (ToggleScreensaver-win-Setup.exe)**](https://github.com/Maf38/toggle-screensaver/releases/latest/download/ToggleScreensaver-win-Setup.exe)

Installeur [Velopack](https://velopack.io) pour Windows (x64) : installation par utilisateur
dans `%LocalAppData%\ToggleScreensaver`, sans droits administrateur ni .NET à installer,
avec raccourcis Bureau et menu Démarrer.
L'application **se met à jour toute seule** : elle vérifie les releases GitHub au lancement puis
toutes les 4 h, télécharge la nouvelle version en arrière-plan et l'installe à la fermeture
(ou tout de suite via Paramètres → Mises à jour). La désinstallation (Paramètres Windows)
retire aussi l'entrée de lancement automatique.

## Fonctionnement

L'application appelle l'API Win32 [`SetThreadExecutionState`](https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate) :

- **Veille désactivée** : `ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED`
  empêche l'écran de s'éteindre et la machine de se mettre en veille tant que
  l'application reste ouverte.
- **Veille activée (normal)** : `ES_CONTINUOUS` rétablit le comportement par défaut
  de Windows.

L'état normal est systématiquement rétabli à la fermeture de l'application.

## Paramètres

L'onglet **Paramètres** enregistre chaque changement immédiatement.

| Option | Description |
| --- | --- |
| Lancer avec Windows | Démarre l'application à l'ouverture de session (clé `Run` de l'utilisateur). Démarrée ainsi, elle reste dans la zone de notification si cette option est active, sinon elle est réduite dans la barre des tâches. |
| Veille désactivée par défaut au lancement | État initial de l'application (activé par défaut). |
| Zone de notification | L'application vit dans la zone de notification : plus de bouton dans la barre des tâches, fermer ou réduire la fenêtre la masque. Menu clic droit : basculer, ouvrir, quitter ; double-clic : ouvrir. |
| Plage horaire | Réactive la veille automatiquement sur une plage (21:30 → 05:30 par défaut, peut passer minuit, pas de 5 min). À la fin de la plage, l'état par défaut est rétabli. Un basculement manuel pendant la plage reste valable jusqu'à la prochaine borne. |
| Raccourci clavier global | Bascule la veille depuis n'importe où (`Ctrl + Alt + S` par défaut). Un message s'affiche si la combinaison est déjà prise. |

L'icône de la fenêtre et de la zone de notification change avec l'état : croissant de lune
quand la veille est activée, soleil quand elle est désactivée.

Les réglages sont stockés en JSON dans `%APPDATA%\ToggleScreensaver\settings.json`
(un simple fichier suffit pour une poignée d'options ; SQLite serait disproportionné).
Une seule instance tourne à la fois : relancer l'exécutable ramène la fenêtre au premier plan.

## Workflow Git

Git flow classique : `main` (versions livrées) et `develop` (intégration) sont protégées —
pull request obligatoire, check CI `build` requis (plus `check-tag` pour `main`), pas de
force-push ni de suppression. Le travail se fait dans des branches `feature/*` créées depuis
`develop` et fusionnées par PR ; les corrections urgentes passent par `hotfix/*` depuis `main`.

## Prérequis

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Compilation et exécution

```bash
dotnet run
```

Lancée depuis les sources, l'application fonctionne normalement mais sans mises à jour
automatiques (réservées à une installation via l'installeur).

## Publier une version

1. Sur `develop`, poser un tag de version et le pousser : `git tag v1.1.0 && git push origin v1.1.0`.
   Le workflow [`release.yml`](.github/workflows/release.yml) compile, empaquette avec Velopack
   (`vpk pack`) et crée la release GitHub (installeur + paquets de mise à jour + notes générées).
2. Ouvrir une PR `develop` → `main`. Le check `check-tag` refuse la fusion si le commit n'a pas
   de tag `vX.Y.Z` ; le check `build` compile le projet.
3. Les installations existantes récupèrent la nouvelle version automatiquement.

## Structure

| Fichier | Rôle |
| --- | --- |
| `MainWindow.xaml(.cs)` | Interface : onglets Principal et Paramètres |
| `App.xaml.cs` | Démarrage, instance unique, application des réglages |
| `SleepController.cs` | Appel P/Invoke `SetThreadExecutionState` et état courant |
| `SleepScheduler.cs` | Plage horaire de veille automatique |
| `AppSettings.cs` | Réglages et persistance JSON |
| `TrayService.cs` / `IconFactory.cs` | Zone de notification et icônes lune/soleil |
| `HotkeyManager.cs` | Raccourci clavier global |
| `StartupRegistration.cs` | Lancement avec Windows |
| `UpdateService.cs` | Mises à jour automatiques (Velopack + releases GitHub) |
| `sleep.png` / `sleep.ico` | Illustration et icône |
