# Toggle Screensaver

Petite application WPF (.NET 8, Windows) pour activer/désactiver la mise en veille
de l'écran et du système en un clic.

![Marmotte endormie](sleep.png)

## Télécharger

[**⬇ Télécharger ToggleScreensaver.exe**](https://github.com/Maf38/toggle-screensaver/releases/latest/download/ToggleScreensaver.exe)

Exécutable autonome pour Windows (x64), aucune installation de .NET requise.
Il est reconstruit automatiquement à chaque mise à jour de la branche `main`
(voir [`.github/workflows/release.yml`](.github/workflows/release.yml)).

## Fonctionnement

L'application appelle l'API Win32 [`SetThreadExecutionState`](https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate) :

- **Veille désactivée** : `ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED`
  empêche l'écran de s'éteindre et la machine de se mettre en veille tant que
  l'application reste ouverte.
- **Veille activée (normal)** : `ES_CONTINUOUS` rétablit le comportement par défaut
  de Windows.

L'état normal est systématiquement rétabli à la fermeture de la fenêtre.

## Prérequis

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Compilation et exécution

```bash
dotnet run
```

Pour produire un exécutable autonome :

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Structure

| Fichier | Rôle |
| --- | --- |
| `MainWindow.xaml` | Interface (image, libellé d'état, bouton toggle) |
| `MainWindow.xaml.cs` | Appel P/Invoke et logique de bascule |
| `App.xaml` / `App.xaml.cs` | Point d'entrée de l'application WPF |
| `sleep.png` / `sleep.ico` | Illustration et icône |
