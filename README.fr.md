<p align="center"><img src="docs/banner.png" alt="SoundSwap"></p>

<p align="center">
  <a href="https://github.com/Stevehunn/soundswap/releases/latest"><img alt="Version" src="https://img.shields.io/github/v/release/Stevehunn/soundswap?color=3DDBB0"></a>
  <a href="https://github.com/Stevehunn/soundswap/actions/workflows/build.yml"><img alt="Build" src="https://github.com/Stevehunn/soundswap/actions/workflows/build.yml/badge.svg"></a>
  <a href="LICENSE"><img alt="Licence : MIT" src="https://img.shields.io/badge/licence-MIT-3DDBB0"></a>
  <img alt="Plateforme : Windows 10/11" src="https://img.shields.io/badge/plateforme-Windows%2010%2F11-1E8FD8">
</p>

[English](README.md) · **Français** · [Español](README.es.md)

# SoundSwap

**Change de micro et de casque (ou d'enceintes) Windows avec un simple raccourci clavier.**
SoundSwap vit dans la zone de notification et change à la volée les périphériques audio de **sortie** et d'**entrée** par défaut.
Gratuit, open source ([MIT](LICENSE)), de la famille [SoundDeck](https://sounddeck.stream/soundswap).

## Fonctionnalités
- **Configurations** : un nom + un périphérique de sortie + un périphérique d'entrée (+ un raccourci facultatif), affichés en cartes que l'on peut **glisser pour réordonner**.
- **Raccourcis globaux** : un raccourci « configuration suivante » et un par configuration, actifs même fenêtre réduite ou masquée.
- **Zone de notification** : clic gauche = ouvrir la fenêtre, clic droit = changer de configuration. Fermer la fenêtre demande : réduire, quitter ou annuler.
- **Thèmes** : sombre, clair, système, ou **personnalisé** (4 couleurs au choix, aperçu en direct, préréglages).
- **10 langues** : English, Français, Español, Deutsch, Português, Italiano, Русский, 中文, 日本語, العربية.
- **Mises à jour intégrées** : un bouton *Nouvelle version disponible* télécharge pour vous la dernière release GitHub (vérifiée), il ne reste qu'à l'installer.
- **Lancement avec Windows** (réduit), instance unique, notification facultative à chaque changement.

## Installation
1. Téléchargez **`SoundSwap-Setup-<version>.exe`** depuis la [dernière release](https://github.com/Stevehunn/soundswap/releases/latest) et lancez-le.
   L'installation se fait pour l'utilisateur courant uniquement : aucun droit administrateur requis.
2. Pas d'installeur ? Téléchargez le `…-portable.zip`, décompressez-le où vous voulez et lancez `SoundSwap.exe`.

Prérequis : Windows 10 ou 11 (64 bits) ; .NET est inclus.
Windows SmartScreen peut afficher un avertissement car l'installeur n'est pas encore signé : *Informations complémentaires → Exécuter quand même*.

## Mode d'emploi
1. **Créer une configuration** : *+ Nouvelle configuration* → nom, sortie, entrée, raccourci facultatif → *Valider*.
2. **L'appliquer** : bouton **▶** de la carte, son raccourci, ou le menu de l'icône de notification.
3. **Faire défiler** : le raccourci du haut (par défaut `Ctrl + Alt + →`) passe à la configuration suivante. L'ordre est celui des cartes (glissez pour le changer).
   Les configurations dont un périphérique est débranché sont ignorées.
4. **Enregistrer un raccourci** : cliquez dans le champ, appuyez sur les touches (au moins une parmi Ctrl / Alt / Maj / Win). `Échap`, `Retour arrière`, `Suppr` ou **✕** l'efface.
   Un message rouge signale qu'un autre programme utilise déjà cette combinaison.
5. **Le laisser tourner** : fermez avec **X** → *Réduire dans les icônes cachées*. Les raccourcis ne fonctionnent que tant que SoundSwap tourne.
6. **Lancer avec Windows** : cochez la case en bas. SoundSwap démarre alors masqué dans la zone de notification à l'ouverture de session.
   (Si vous le désactivez dans l'onglet *Démarrage* du Gestionnaire des tâches, SoundSwap le respecte.)
7. **Personnaliser** : choisissez thème et langue en haut. Avec *Personnalisé*, **Personnaliser…** ouvre le choix des couleurs.

Plus de détails : [guide utilisateur complet](docs/articles/user-guide.md) (en anglais).

## Mises à jour et vie privée
SoundSwap consulte la dernière release de ce dépôt environ 10 secondes après le lancement puis toutes les 12 heures. Si une version plus récente existe, un bouton apparaît :
cliquez pour télécharger l'installeur (uniquement depuis `github.com` ; taille et SHA-256 vérifiés), cliquez à nouveau pour l'installer.
Cette vérification est la **seule** requête réseau de SoundSwap : pas de télémétrie, pas de compte. Décochez *Vérifier automatiquement les mises à jour* pour la désactiver.
Détails : [Updates](docs/articles/updates.md).

## Dépannage
| Problème | Que faire |
|---|---|
| Un raccourci affiche un message rouge « indisponible » | Un autre programme l'utilise : choisissez une autre combinaison |
| Les raccourcis ne font rien | SoundSwap a été quitté (pas réduit) : relancez-le et regardez la zone de notification |
| « Périphérique indisponible » | Un des périphériques de la configuration est débranché ou désactivé dans Windows |
| Ne démarre pas avec Windows | Cochez l'option dans SoundSwap ; vérifiez que l'onglet *Démarrage* du Gestionnaire des tâches n'est pas sur Désactivé |
| Listes au texte étrange / pas de bouton de mise à jour | Une ancienne version tourne : réinstallez, ou `dotnet clean` puis recompilez ; le pied de fenêtre affiche la version (`v1.2.0`) |

Les réglages sont enregistrés dans `%APPDATA%\SoundSwap\data.json` ; les erreurs sont journalisées dans `%APPDATA%\SoundSwap\error.log` (à joindre aux signalements de bug).

## Compiler depuis les sources
Windows 10/11 + [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) :
```powershell
git clone https://github.com/Stevehunn/soundswap.git
cd soundswap
dotnet build SoundSwap.sln -c Release
dotnet test tests/SoundSwap.Core.Tests
dotnet run --project src/SoundSwap.App -c Release
.\scripts\publish.ps1 -Version 1.2.0 -Installer -Portable   # l'installeur nécessite Inno Setup 6
```

## Organisation du projet
| Projet | Rôle |
|---|---|
| `src/SoundSwap.Core` | Logique : configurations, raccourcis, palette de thème, mise à jour. Sans interface, testée |
| `src/SoundSwap.Audio` | Périphériques audio Windows (NAudio + `IPolicyConfig`) |
| `src/SoundSwap.App` | Application WPF (MVVM) : fenêtres, zone de notification, raccourcis, thèmes, traductions |
| `tests/SoundSwap.Core.Tests` | Tests xUnit |
| `installer/`, `scripts/`, `.github/workflows/` | Installeur, scripts de build, CI/publication |

## Documentation
Tous les types et membres publics sont documentés par des commentaires XML ; la référence d'API et les articles sont générés avec
[DocFX](https://dotnet.github.io/docfx/) depuis [`docs/`](docs/) (GitHub Pages). Commencez par [Architecture](docs/articles/architecture.md).

## Contribuer
Issues et pull requests bienvenues : voir [CONTRIBUTING.md](CONTRIBUTING.md) (ajouter une langue = un fichier JSON).
Sécurité : [SECURITY.md](SECURITY.md).

## Licence
[MIT](LICENSE) © 2026 Stevehunn. Composants tiers : [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
