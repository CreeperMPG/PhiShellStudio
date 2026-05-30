@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "DESKTOP_PROJECT=%SCRIPT_DIR%PhigrosShellGUI.Desktop"
set "DIST_DIR=%SCRIPT_DIR%dist\desktop"

REM Extract version from app.manifest
for /f "tokens=3 delims== " %%a in ('findstr "assemblyIdentity" "%DESKTOP_PROJECT%\app.manifest"') do (
    set "APP_VERSION=%%~a"
    goto :version_found
)
:version_found
REM Extract just the version value (remove version=" prefix)
set "APP_VERSION=!APP_VERSION:version=!"
set "APP_VERSION=!APP_VERSION:"=!"
set "APP_VERSION=!APP_VERSION:==!"
if "%APP_VERSION%"=="" set "APP_VERSION=dev"

echo ============================================
echo   PhiShell Studio - Desktop Build Script
echo   Version: %APP_VERSION%
echo ============================================
echo.

REM Clean dist directory
if exist "%DIST_DIR%" rmdir /S /Q "%DIST_DIR%"
mkdir "%DIST_DIR%"

REM ========== 1/2: Windows x64 ==========
echo [1/2] Building Windows x64...
"%DOTNET_ROOT%\dotnet.exe" publish "%DESKTOP_PROJECT%" ^
    -f net10.0 -c Release -r win-x64 ^
    -p:PublishSingleFile=true ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -o "%DIST_DIR%\win-x64"
if errorlevel 1 (
    echo [ERROR] Windows x64 build failed!
    exit /b 1
)
echo [OK] Windows x64 build complete
echo.

REM ========== 2/2: Linux x64 ==========
echo [2/2] Building Linux x64 (cross-compile)...
"%DOTNET_ROOT%\dotnet.exe" publish "%DESKTOP_PROJECT%" ^
    -f net10.0 -c Release -r linux-x64 ^
    -p:PublishSingleFile=true ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -o "%DIST_DIR%\linux-x64"
if errorlevel 1 (
    echo [ERROR] Linux x64 build failed!
    exit /b 1
)
echo [OK] Linux x64 build complete
echo.

REM ========== Summary ==========
echo ============================================
echo   ✅ All builds complete!
echo   Version: %APP_VERSION%
echo   Output: %DIST_DIR%
echo ============================================
echo.

REM Rename executables
if exist "%DIST_DIR%\win-x64\PhigrosShellGUI.Desktop.exe" (
    ren "%DIST_DIR%\win-x64\PhigrosShellGUI.Desktop.exe" "PhiShellStudio-win-x64.exe"
    echo   win-x64: PhiShellStudio-win-x64.exe
)
if exist "%DIST_DIR%\linux-x64\PhigrosShellGUI.Desktop" (
    ren "%DIST_DIR%\linux-x64\PhigrosShellGUI.Desktop" "PhiShellStudio-linux-x64"
    echo   linux-x64: PhiShellStudio-linux-x64
)
if exist "%DIST_DIR%\linux-x64\PhigrosShellGUI.Desktop.exe" (
    ren "%DIST_DIR%\linux-x64\PhigrosShellGUI.Desktop.exe" "PhiShellStudio-linux-x64.exe"
    echo   linux-x64: PhiShellStudio-linux-x64.exe
)

echo.
dir /B "%DIST_DIR%\win-x64\*.exe" "%DIST_DIR%\linux-x64\*" 2>nul
echo.

REM Copy to Releases folder
set "RELEASE_DIR=%SCRIPT_DIR%Releases\%APP_VERSION%"
if not exist "%RELEASE_DIR%\win-x64" mkdir "%RELEASE_DIR%\win-x64"
if not exist "%RELEASE_DIR%\linux-x64" mkdir "%RELEASE_DIR%\linux-x64"

for %%f in ("%DIST_DIR%\win-x64\*.exe") do copy /Y "%%f" "%RELEASE_DIR%\win-x64\" >nul
for %%f in ("%DIST_DIR%\linux-x64\*") do copy /Y "%%f" "%RELEASE_DIR%\linux-x64\" >nul

echo   Also copied to: %RELEASE_DIR%
echo.

pause
