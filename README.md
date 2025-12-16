# 🎮 Dofus Organizer

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4)](https://www.microsoft.com/windows)

**Application overlay tactile pour gérer facilement vos fenêtres Dofus en multicompte.**

Une interface moderne et intuitive qui vous permet de basculer instantanément entre vos différents personnages Dofus, avec support de raccourcis clavier personnalisables et deux modes d'interaction (overlay tactile ou système tray).

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