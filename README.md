# DiscordNative 🌸

> Client Discord natif pour **Windows 11 & Windows 10** développé en **C# (.NET 8)** et **WinUI 3 / Windows App SDK** (aucun Electron / CEF / WebView).

Inspiré de l'esprit et de la philosophie de [SakuraCord](https://github.com/SakuraCordApp/SakuraCord) (client natif macOS), **DiscordNative** apporte une expérience fluide, ultra-légère et intégrée au design moderne de Windows (Mica, acrylique, thèmes sombre/clair, coins arrondis).

---

## ✨ Fonctionnalités actuelles

- 🪟 **Design Windows 11 Natif** :
  - Support complet de l'effet **Mica / Acrylic** (`SystemBackdrop`).
  - Thème sombre et clair dynamique et synchronisé.
  - Arrière-plan personnalisé avec flou gaussien et transparence réglable (inspiré de *Files*).
  - Coins arrondis sur tous les contrôles, cartes de salons, serveurs et avatars.
- 💬 **Messagerie & Salons** :
  - Synchronisation temps réel via **Discord Gateway WebSocket** (v9).
  - Envoi et réception instantanée de messages.
  - Groupement intelligent des messages consécutifs (style Discord).
  - Prise en charge des messages système, modifications (`MESSAGE_UPDATE`) et suppressions (`MESSAGE_DELETE`).
  - Affichage et interaction avec les **réactions** d'emojis (`MESSAGE_REACTION_ADD`).
  - Rendu des **Embeds** et des images jointes.
  - Envoi de pièces jointes et fichiers locaux via sélecteur Windows natif.
- 🎙️ **Salons Vocaux** :
  - Grille visuelle moderne de participants pour les salons vocaux.
  - Connexion Gateway aux salons vocaux (`Voice State Update`, Opcode 4).
  - Encart persistent "Voice Connected" avec nom du salon et bouton de déconnexion rapide.
  - Barre de contrôle audio (Muet, Casque, Déconnexion).
- 👤 **Profils Utilisateurs** :
  - Popout de profil complet au clic sur n'importe quel avatar : bannière, avatar chevauchant avec bordure, pseudo, tag, badge et bio.
- ⚡ **Performance & Sobriété** :
  - Démarrage instantané, empreinte mémoire minimale par rapport au client officiel basé sur Chromium.

---

## 🏗️ Structure du Projet

```
DiscordNative.sln
src/
  DiscordNative.Protocol/       Bibliothèque .NET indépendante : Gateway WebSocket, REST, Auth, Modèles
    Auth/                       Authentification utilisateur & gestion de session
    Gateway/                    Connexion WebSocket, Heartbeat, Identify, Resume, Dispatch d'événements
    Rest/                       Endpoints REST Discord (Guilds, Channels, Messages, Reactions, Uploads)
    Models/                     User, Guild, Channel, Message, Attachment, Embed, Reaction...
  DiscordNative.App/            Application Windows WinUI 3 (Windows App SDK)
    Views/                      MainWindow, MainWindowContent, LoginWindow, SettingsWindow
    ViewModels/                 MainViewModel (Pattern MVVM via CommunityToolkit.Mvvm)
    Controls/                   BackgroundImageHost (gestion de l'image de fond et effets Win2D)
    Services/                   SettingsService (stockage JSON local sécurisé)
    Converters/                 Convertisseurs XAML (avatars, icônes, visibilités)
```

---

## 🚀 Prérequis & Compilation

- **Système** : Windows 10 (version 1809+) ou Windows 11
- **Outils** :
  - .NET 8 SDK
  - Visual Studio 2022 (17.10+) avec la charge de travail **Développement .NET Desktop** et **Windows App SDK**
- **Lancement** :
  ```powershell
  # Cloner le dépôt
  git clone https://github.com/djell1l/DiscordNative.git
  cd DiscordNative

  # Compiler et lancer
  .\build.ps1
  ```

---

## ⚠️ Avertissement Légal

Ce projet est un client tiers non officiel créé à des fins d'apprentissage et de démonstration technique. Il s'authentifie via le protocole utilisateur Discord. L'utilisation d'un client tiers peut enfreindre les [Conditions d'Utilisation de Discord](https://discord.com/terms). Utilisez-le à vos propres risques.

---

## 🌸 Remerciements & Inspirations

- [SakuraCord](https://github.com/SakuraCordApp/SakuraCord) pour l'inspiration originale du client natif léger sans Electron.
- [Files App](https://github.com/files-community/Files) pour l'inspiration du design XAML et la gestion des arrière-plans personnalisés.
