@echo off
setlocal EnableDelayedExpansion

:: ============================================================
::  build-self-contained.bat -- Publish self-contained x64
::  Location : scripts\
::  Output   : publish\win-x64-standalone\
::
::  Bundles the full .NET 8 runtime -- no installation needed
::  on the target machine.
:: ============================================================

set "SCRIPTS_DIR=%~dp0"
set "ROOT=%SCRIPTS_DIR%.."
set "PROJECT=%ROOT%\src\GlobalDataCreatorETL\GlobalDataCreatorETL.csproj"
set "OUT_DIR=%ROOT%\publish\win-x64-standalone"
set "CONFIG=Release"
set "RUNTIME=win-x64"

echo.
echo  ============================================================
echo   GLOBAL_DATACREATOR_ETL  ^|  Self-Contained Build  [x64]
echo  ============================================================
echo.
echo   Config  : %CONFIG%
echo   Runtime : %RUNTIME%
echo   Output  : publish\win-x64-standalone\
echo.

:: -- Verify dotnet is available --------------------------------
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo  [ERROR] dotnet not found in PATH.
    echo          Install .NET 8 SDK: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "DOTNET_VER=%%v"
echo  [OK]  .NET SDK : %DOTNET_VER%
echo.

:: -- Clean previous publish output -----------------------------
if exist "%OUT_DIR%" (
    echo  [..] Removing previous publish output...
    rmdir /s /q "%OUT_DIR%"
    echo  [OK]  Cleared: publish\win-x64-standalone\
)

:: -- Clean obj\ and bin\ --------------------------------------
echo.
echo  [..] Cleaning obj\ and bin\...
if exist "%ROOT%\src\GlobalDataCreatorETL\bin" (
    rmdir /s /q "%ROOT%\src\GlobalDataCreatorETL\bin"
    echo  [OK]  Deleted: src\GlobalDataCreatorETL\bin\
) else (
    echo  [--]  bin\ not found, skipping.
)
if exist "%ROOT%\src\GlobalDataCreatorETL\obj" (
    rmdir /s /q "%ROOT%\src\GlobalDataCreatorETL\obj"
    echo  [OK]  Deleted: src\GlobalDataCreatorETL\obj\
) else (
    echo  [--]  obj\ not found, skipping.
)

:: -- Publish ---------------------------------------------------
echo.
echo  [..] Publishing self-contained x64...
echo       This may take 30-60 seconds...
echo.

dotnet publish "%PROJECT%" ^
    --configuration %CONFIG% ^
    --runtime %RUNTIME% ^
    --self-contained true ^
    --output "%OUT_DIR%" ^
    -p:Platform=x64 ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    --nologo

if %errorlevel% neq 0 (
    echo.
    echo  [FAILED] Publish failed -- exit code %errorlevel%.
    echo.
    pause
    exit /b %errorlevel%
)

:: -- Verify executable -----------------------------------------
set "EXE=%OUT_DIR%\GlobalDataCreatorETL.exe"
if not exist "%EXE%" (
    echo  [ERROR] GlobalDataCreatorETL.exe missing from output.
    echo.
    pause
    exit /b 1
)
echo  [OK]  Executable confirmed: GlobalDataCreatorETL.exe

:: -- Remove unused SqlClient localization folders ---------------
echo  [..]  Removing unused localization folders...
for %%L in (de es fr it ja ko pt-BR ru zh-Hans zh-Hant) do (
    if exist "%OUT_DIR%\%%L" (
        rmdir /s /q "%OUT_DIR%\%%L"
    )
)
echo  [OK]  Localization folders removed.

:: -- Copy config.json if not already there ---------------------
if not exist "%OUT_DIR%\config.json" (
    if exist "%ROOT%\config.json" (
        echo  [..] Copying config.json to publish folder...
        copy /Y "%ROOT%\config.json" "%OUT_DIR%\config.json" >nul
        echo  [OK]  config.json copied.
    ) else (
        echo  [--]  config.json not found at repo root, skipping.
    )
) else (
    echo  [OK]  config.json present in publish output.
)

:: -- Create shortcut in root directory ------------------------
echo.
echo  [..] Creating shortcut in root directory...
set "EXE_ABS="
for %%F in ("%EXE%") do set "EXE_ABS=%%~fF"
set "WD_ABS="
for %%F in ("%OUT_DIR%") do set "WD_ABS=%%~fF"
set "ROOT_ABS="
for %%F in ("%ROOT%") do set "ROOT_ABS=%%~fF"

:: -- Delete old shortcut so it is always freshly created ------
if exist "%ROOT_ABS%\GlobalDataCreatorETL.lnk" (
    del /f /q "%ROOT_ABS%\GlobalDataCreatorETL.lnk" >nul 2>&1
)

set "VBS_TMP=%TEMP%\mk_shortcut_%RANDOM%.vbs"
(
    echo Set ws = CreateObject^("WScript.Shell"^)
    echo Set sc = ws.CreateShortcut^("%ROOT_ABS%\GlobalDataCreatorETL.lnk"^)
    echo sc.TargetPath = "%EXE_ABS%"
    echo sc.WorkingDirectory = "%WD_ABS%"
    echo sc.Description = "GlobalDataCreatorETL"
    echo sc.IconLocation = "%EXE_ABS%, 0"
    echo sc.Save
) > "%VBS_TMP%"

cscript //nologo "%VBS_TMP%" >nul 2>&1
if %errorlevel% neq 0 (
    echo  [WARN] Could not create shortcut.
) else (
    echo  [OK]  Shortcut created: GlobalDataCreatorETL.lnk  (root folder^)
)
del "%VBS_TMP%" >nul 2>&1

:: -- Summary ---------------------------------------------------
set "FILE_COUNT=0"
for /f %%i in ('dir /b /s "%OUT_DIR%" 2^>nul ^| find /c /v ""') do set "FILE_COUNT=%%i"

echo.
echo  ============================================================
echo   BUILD COMPLETE
echo  ============================================================
echo.
echo   Output folder : publish\win-x64-standalone\
echo   Files         : %FILE_COUNT%
echo   Executable    : GlobalDataCreatorETL.exe  (single file -- all DLLs bundled)
echo   Config files  : Config\  +  config.json
echo.
echo   NOTE: First launch extracts native libs to a temp folder (one-time, ~2s).
echo.
echo   Edit config.json next to the .exe to configure:
echo     Paths.OutputDirectory   -- where Excel files are saved
echo     Paths.LogDirectory      -- where log files are written
echo     Database.ServerName     -- SQL Server host,port
echo     Database.DatabaseName   -- target database
echo.
echo   To launch:
echo     GlobalDataCreatorETL.lnk  (shortcut in root -- double-click)
echo     run.bat                   (from repo root)
echo     or double-click the .exe directly
echo.
echo  ============================================================
echo.
pause
endlocal
