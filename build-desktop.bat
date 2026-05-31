@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "DESKTOP_PROJECT=%SCRIPT_DIR%PhigrosShellGUI.Desktop"
set "DIST_DIR=%SCRIPT_DIR%dist\desktop"

echo ============================================
echo   PhiShell Studio - Desktop Build Script
echo ============================================
echo Target: net10.0 (Release)
echo Output: %DIST_DIR%
echo.

REM Clean dist directory
if exist "%DIST_DIR%" rmdir /S /Q "%DIST_DIR%"
mkdir "%DIST_DIR%"

REM ========== 1/2: Windows x64 ==========
echo [1/2] Building Windows x64...
"%DOTNET_ROOT%\dotnet.exe" publish "%DESKTOP_PROJECT%" ^
    -f net10.0 -c Release -r win-x64 ^
    -p:DebugType=none ^
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
    -p:DebugType=none ^
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
echo   All builds complete!
echo   Output: %DIST_DIR%
echo ============================================
echo.
dir /B "%DIST_DIR%\win-x64\PhigrosShellGUI.Desktop.exe" "%DIST_DIR%\linux-x64\PhigrosShellGUI.Desktop" 2>nul
echo.

REM Strip PDB files (native SkiaSharp PDBs are 100MB+)
del /S /Q "%DIST_DIR%\win-x64\*.pdb" 2>nul
del /S /Q "%DIST_DIR%\linux-x64\*.pdb" 2>nul
echo [Cleanup] PDB symbols removed, size should now be ~20MB
echo.

REM Copy to Releases folder
set "RELEASE_DIR=%SCRIPT_DIR%Releases"
if not exist "%RELEASE_DIR%\win-x64" mkdir "%RELEASE_DIR%\win-x64"
if not exist "%RELEASE_DIR%\linux-x64" mkdir "%RELEASE_DIR%\linux-x64"

xcopy /E /I /Y "%DIST_DIR%\win-x64" "%RELEASE_DIR%\win-x64" >nul
xcopy /E /I /Y "%DIST_DIR%\linux-x64" "%RELEASE_DIR%\linux-x64" >nul

echo   Also copied to: %RELEASE_DIR%
echo.
pause
