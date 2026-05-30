@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "ANDROID_PROJECT=%SCRIPT_DIR%PhigrosShellGUI.Android"
set "DIST_DIR=%SCRIPT_DIR%dist\android"

REM Extract version from AndroidManifest.xml
for /f "tokens=2 delims== " %%a in ('findstr "versionName" "%ANDROID_PROJECT%\Properties\AndroidManifest.xml"') do (
    set "APP_VERSION=%%~a"
    goto :version_found
)
:version_found
REM Remove quotes from version if any
set "APP_VERSION=%APP_VERSION:"=%"
if "%APP_VERSION%"=="" set "APP_VERSION=dev"

echo ============================================
echo   PhiShell Studio - Android Build Script
echo   Version: %APP_VERSION%
echo ============================================
echo.
echo Target: net10.0-android (Release)
echo Output: %DIST_DIR%
echo.

REM Clean dist directory
if exist "%DIST_DIR%" rmdir /S /Q "%DIST_DIR%"
mkdir "%DIST_DIR%"

REM ========== 1/3: ARM64-v8a ==========
echo [1/3] Building ARM64-v8a (android-arm64)...
"%DOTNET_ROOT%\dotnet.exe" publish "%ANDROID_PROJECT%" ^
    -f net10.0-android -c Release -r android-arm64 --self-contained ^
    -p:AndroidPackageOutputPath="%DIST_DIR%\arm64-v8a"
if errorlevel 1 (
    echo [ERROR] ARM64-v8a build failed!
    exit /b 1
)
echo [OK] ARM64-v8a build complete
echo.

REM ========== 2/3: ARMEABI-v7a ==========
echo [2/3] Building ARMEABI-v7a (android-arm)...
"%DOTNET_ROOT%\dotnet.exe" publish "%ANDROID_PROJECT%" ^
    -f net10.0-android -c Release -r android-arm --self-contained ^
    -p:AndroidPackageOutputPath="%DIST_DIR%\armeabi-v7a"
if errorlevel 1 (
    echo [ERROR] ARMEABI-v7a build failed!
    exit /b 1
)
echo [OK] ARMEABI-v7a build complete
echo.

REM ========== 3/3: X86_64 ==========
echo [3/3] Building X86_64 (android-x64)...
"%DOTNET_ROOT%\dotnet.exe" publish "%ANDROID_PROJECT%" ^
    -f net10.0-android -c Release -r android-x64 --self-contained ^
    -p:AndroidPackageOutputPath="%DIST_DIR%\x86_64"
if errorlevel 1 (
    echo [ERROR] X86_64 build failed!
    exit /b 1
)
echo [OK] X86_64 build complete
echo.

REM ========== Summary ==========
echo ============================================
echo   ✅ All builds complete!
echo   Version: %APP_VERSION%
echo   Output: %DIST_DIR%
echo ============================================
echo.
dir /B /S "%DIST_DIR%\*.apk" 2>nul || echo (no APK found - check build output)
echo.

REM Copy to Releases folder with clean names
set "RELEASE_DIR=%SCRIPT_DIR%Releases\%APP_VERSION%\android"
if not exist "%RELEASE_DIR%" mkdir "%RELEASE_DIR%"

copy /Y "%DIST_DIR%\arm64-v8a\com.CreeperMPG.PhiShell.Studio-Signed.apk" "%RELEASE_DIR%\PhiShellStudio-arm64-v8a.apk" >nul
copy /Y "%DIST_DIR%\armeabi-v7a\com.CreeperMPG.PhiShell.Studio-Signed.apk" "%RELEASE_DIR%\PhiShellStudio-armeabi-v7a.apk" >nul
copy /Y "%DIST_DIR%\x86_64\com.CreeperMPG.PhiShell.Studio-Signed.apk" "%RELEASE_DIR%\PhiShellStudio-x86_64.apk" >nul

echo   Also copied to: %RELEASE_DIR%
dir "%RELEASE_DIR%"
echo.

pause
