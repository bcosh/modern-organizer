@echo off
echo ========================================
echo   Dofus Organizer - Lancement Rapide
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

echo [INFO] Lancement de l'application...
echo.

dotnet run

if %errorlevel% neq 0 (
    echo.
    echo [ERREUR] L'application a rencontré une erreur !
    pause
)
