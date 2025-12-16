# 🎮 Dofus Organizer

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4)](https://www.microsoft.com/windows)

**Application overlay tactile pour gérer facilement vos fenêtres Dofus en multicompte.**

Une interface moderne et intuitive qui vous permet de basculer instantanément entre vos différents personnages Dofus, avec support de raccourcis clavier personnalisables et deux modes d'interaction (overlay tactile ou système tray).

---

## ✨ Fonctionnalités Principales

### 🎯 Gestion des Fenêtres
- ✅ **Détection automatique** des fenêtres Dofus (via UnityWndClass)
- ✅ **Overlay transparent** toujours au-dessus du jeu
- ✅ **Bouton compact** (90x90px) pour cycler rapidement entre personnages
- ✅ **Mode configuration** avec liste complète des personnages
- ✅ **Rafraîchissement manuel** pour détecter les nouvelles fenêtres

### ⌨️ Raccourcis Clavier
- ✅ **Raccourci personnalisable** pour cycler entre les fenêtres
- ✅ **Raccourci dédié** pour revenir au "chef de groupe"
- ✅ **Support de n'importe quelle touche** (F1-F12, nombres, lettres, etc.)
- ✅ **Hotkeys globaux** - fonctionne même quand l'application est en arrière-plan

### 🎨 Interface & Personnalisation
- ✅ **Deux modes d'interaction** :
  - **Tactile** : Overlay visible avec bouton compact
  - **Classique** : Caché dans le system tray, raccourcis uniquement
- ✅ **Opacité réglable** de l'interface
- ✅ **Bouton draggable** - placez-le où vous voulez sur l'écran
- ✅ **Design moderne** avec coins arrondis et thème sombre
- ✅ **Chef de groupe** désignable avec indicateur visuel (★)

### 💾 Persistance
- ✅ **Sauvegarde automatique** de tous les paramètres :
  - Raccourcis clavier
  - Position du bouton compact
  - Mode d'interaction
  - Opacité
  - Chef de groupe désigné

---

## 🚀 Installation

### Option 1 : Télécharger la Release (Utilisateurs)

1. Allez dans la section [**Releases**](../../releases) de ce repository
2. Téléchargez la dernière version (`DofusOrganizer-vX.X.X.zip`)
3. Décompressez le fichier ZIP
4. Lancez `DofusOrganizer.exe`

> **Note** : Aucune installation de .NET n'est nécessaire - l'exécutable est autonome (*self-contained*).

### Option 2 : Compiler depuis les Sources (Développeurs)

#### Prérequis
- Windows 10 ou 11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git (optionnel)

#### Étapes

1. **Clonez le repository**
   ```bash
   git clone https://github.com/VOTRE_USERNAME/organizer-tactile.git
   cd organizer-tactile
   ```

2. **Compilez le projet**
   ```bash
   dotnet build -c Release
   ```

3. **Lancez l'application**
   ```bash
   dotnet run --project DofusOrganizer.csproj
   ```

4. **Créez un exécutable autonome (optionnel)**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```
   L'exécutable sera dans `bin/Release/net10.0-windows/win-x64/publish/`

---

## 📖 Guide Rapide

1. **Lancez Dofus** et connectez vos personnages
2. **Lancez DofusOrganizer.exe**
3. **Configurez vos paramètres** :
   - Cliquez sur l'icône ⚙️ pour accéder aux réglages
   - Définissez vos raccourcis clavier
   - Choisissez votre mode d'interaction (Tactile/Classique)
   - Ajustez l'opacité à votre convenance
4. **Désignez un chef de groupe (optionnel)** :
   - Cliquez sur l'étoile ☆ à côté d'un personnage
5. **Cliquez sur "Valider"** pour appliquer les paramètres
6. **Utilisez l'application** :
   - En mode Tactile : cliquez sur le bouton compact pour cycler
   - En mode Classique : utilisez vos raccourcis clavier
   - Double-cliquez sur le bouton compact pour afficher les réglages

---

## 📚 Documentation Complète

Pour plus de détails, consultez la documentation complète dans le dossier [**docs/**](docs/):

- 📘 [**Guide de Démarrage Rapide**](docs/QUICKSTART.md)
- 📗 [**Guide d'Utilisation Complet**](docs/GUIDE.md)
- 📙 [**Personnalisation Avancée**](docs/CUSTOMIZATION.md)
- 📕 [**Questions Fréquentes (FAQ)**](docs/FAQ.md)
- 🏗️ [**Architecture Technique**](docs/ARCHITECTURE.md)

---

## 🤝 Contribuer

Les contributions sont les bienvenues ! Veuillez consulter [CONTRIBUTING.md](CONTRIBUTING.md) pour les guidelines.

### Idées de Contributions
- 🌍 Support multi-écrans
- 🎨 Thèmes personnalisables
- 📊 Statistiques de jeu
- 🔔 Notifications d'événements
- 🌐 Traduction en d'autres langues

---

## 🐛 Signaler un Bug

Vous avez trouvé un bug ? [Créez une issue](../../issues/new?template=bug_report.md) en suivant le template fourni.

---

## 📜 Licence

Ce projet est sous licence MIT. Voir [LICENSE](LICENSE) pour plus de détails.

### ⚠️ Disclaimer Important

**Cet outil est légal et respecte les Conditions d'Utilisation d'Ankama** :
- Il ne modifie **AUCUN** fichier du jeu
- Il ne fait qu'utiliser les APIs Windows standards pour basculer entre fenêtres
- C'est l'équivalent d'un Alt+Tab automatisé
- Aucune injection de code, automation ou modification de mémoire

**Utilisation à vos propres risques.** Ce projet n'est **PAS** affilié à, approuvé par, ou associé avec Ankama Games.

---

## 🙏 Remerciements

- Créé pour la communauté Dofus 🎮
- Merci à tous les contributeurs et utilisateurs !

---

## 📞 Contact & Support

- 🐛 [Signaler un bug](../../issues/new?template=bug_report.md)
- 💡 [Proposer une fonctionnalité](../../issues/new?template=feature_request.md)
- 💬 [Discussions & Questions](../../discussions)

---

**Bon jeu et bon multicompte ! ⚔️**
