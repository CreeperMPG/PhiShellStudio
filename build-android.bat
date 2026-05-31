@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "ANDROID_PROJECT=%SCRIPT_DIR%PhigrosShellGUI.Android"
set "DIST_DIR=%SCRIPT_DIR%dist\android"

echo ============================================
echo   PhiShell Studio - Android Build Script
echo ============================================
echo Target: net10.0-android (Release)
echo Output: %DIST_DIR%
echo.

REM ----- Detect dotnet -----
set "DOTNET_CMD=dotnet"
where dotnet >nul 2>&1
if errorlevel 1 (
    if defined DOTNET_ROOT (
        set "DOTNET_CMD=%DOTNET_ROOT%\dotnet.exe"
    ) else if exist "C:\Program Files\dotnet\dotnet.exe" (
        set "DOTNET_CMD=C:\Program Files\dotnet\dotnet.exe"
    ) else (
        echo [ERROR] dotnet not found in PATH, DOTNET_ROOT, or default location.
        echo         Install .NET SDK or set DOTNET_ROOT environment variable.
        exit /b 1
    )
)
echo Using: !DOTNET_CMD!
!DOTNET_CMD! --version
echo.

REM ----- Clean dist -----
if exist "%DIST_DIR%" rmdir /S /Q "%DIST_DIR%"
mkdir "%DIST_DIR%"

REM ----- Architecture list -----
set ARCH_LIST=android-arm64 android-arm android-x64
set ARCH_LABEL[android-arm64]=ARM64-v8a
set ARCH_LABEL[android-arm]=armeabi-v7a
set ARCH_LABEL[android-x64]=X86_64

REM armeabi-v7a 需要 Mono 运行时（CoreCLR 不支持 32-bit ARM）
set USE_MONO[android-arm]=true

for %%R in (%ARCH_LIST%) do (
    set "ARCH_LABEL=!ARCH_LABEL[%%R]!"
    set "MONO_FLAG=!USE_MONO[%%R]!"
    echo ========== [!ARCH_LABEL!] Building %%R ...
    
    if defined MONO_FLAG (
        !DOTNET_CMD! publish "%ANDROID_PROJECT%" ^
            -f net10.0-android -c Release -r %%R --self-contained ^
            -p:UseMonoRuntime=true
    ) else (
        !DOTNET_CMD! publish "%ANDROID_PROJECT%" ^
            -f net10.0-android -c Release -r %%R --self-contained
    )
    
    if errorlevel 1 (
        echo [ERROR] [!ARCH_LABEL!] Build failed!
        exit /b 1
    )
    
    REM Copy built APKs from standard output path
    set "PUBLISH_DIR=%ANDROID_PROJECT%\bin\Release\net10.0-android\%%R\publish"
    if exist "!PUBLISH_DIR!" (
        mkdir "%DIST_DIR%\%%R" >nul 2>&1
        copy /Y "!PUBLISH_DIR!\*.apk" "%DIST_DIR%\%%R\" >nul 2>&1
        copy /Y "!PUBLISH_DIR!\*.aab" "%DIST_DIR%\%%R\" >nul 2>&1
        echo [OK] [!ARCH_LABEL!] Build complete
    ) else (
        echo [WARN] [!ARCH_LABEL!] Publish directory not found: !PUBLISH_DIR!
        echo        APK may still be in project output. Check manually.
    )
    echo.
)

REM ========== Summary ==========
echo ============================================
echo   All builds complete!
echo   Output: %DIST_DIR%
dir "%DIST_DIR%" /s /b
echo ============================================
echo.

REM ----- Copy to Releases folder -----
set "RELEASE_DIR=%SCRIPT_DIR%Releases\android"
if not exist "%RELEASE_DIR%" mkdir "%RELEASE_DIR%"

echo Copying APKs to: %RELEASE_DIR%
for %%R in (%ARCH_LIST%) do (
    set "APK_FILE=%DIST_DIR%\%%R\com.CreeperMPG.PhiShell.Studio-Signed.apk"
    if exist "!APK_FILE!" (
        copy /Y "!APK_FILE!" "%RELEASE_DIR%\PhiShellStudio-%%R.apk" >nul
        echo   [OK] PhiShellStudio-%%R.apk
    ) else (
        echo   [--] PhiShellStudio-%%R.apk (not found)
    )
)

echo.
dir "%RELEASE_DIR%" /b
echo.
pause
