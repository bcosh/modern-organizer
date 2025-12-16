@echo off
echo ========================================
echo    Dofus Organizer - Script de Build
echo ========================================
echo.

REM Vérifier si dotnet est installé
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERREUR] .NET SDK n'est pas installé !
    echo Téléchargez-le ici : https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [INFO] .NET SDK détecté
echo.

REM Nettoyer les builds précédents
echo [1/3] Nettoyage des builds précédents...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
echo [OK] Nettoyage terminé
echo.

REM Build en mode Release
echo [2/3] Compilation en mode Release...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo [ERREUR] La compilation a échoué !
    pause
    exit /b 1
)
echo [OK] Compilation réussie
echo.

REM Publier en exécutable autonome
echo [3/3] Création de l'exécutable autonome...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if %errorlevel% neq 0 (
    echo [ERREUR] La publication a échoué !
    pause
    exit /b 1
)
echo [OK] Publication terminée
echo.

echo ========================================
echo          BUILD TERMINÉ !
echo ========================================
echo.
echo L'exécutable est disponible ici :
echo bin\Release\net10.0-windows\win-x64\publish\DofusOrganizer.exe
echo.
echo Vous pouvez copier cet .exe n'importe où !
echo.

REM Ouvrir le dossier de l'exécutable
start explorer "bin\Release\net10.0-windows\win-x64\publish"

pause
