@echo off
setlocal EnableDelayedExpansion

:: ============================================================
::  build-release.bat -- Clean + Fresh Release Build
::  Location : scripts\
::  Output   : src\GlobalDataCreatorETL\bin\Release\net8.0-windows\
::
::  Deletes all obj\ and bin\ artifacts then does a full
::  Release build from scratch.
:: ============================================================

set "SCRIPTS_DIR=%~dp0"
set "ROOT=%SCRIPTS_DIR%.."
set "PROJECT=%ROOT%\src\GlobalDataCreatorETL\GlobalDataCreatorETL.csproj"
set "BIN_DIR=%ROOT%\src\GlobalDataCreatorETL\bin"
set "OBJ_DIR=%ROOT%\src\GlobalDataCreatorETL\obj"
set "PUBLISH_DIR=%ROOT%\publish"
set "CONFIG=Release"

echo.
echo  ============================================================
echo   GLOBAL_DATACREATOR_ETL  ^|  Clean Release Build
echo  ============================================================
echo.
echo   Configuration : %CONFIG%
echo   Project       : src\GlobalDataCreatorETL\GlobalDataCreatorETL.csproj
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

:: -- Step 1: dotnet clean --------------------------------------
echo  ----------------------------------------------------------------
echo   STEP 1 of 3 -- dotnet clean
echo  ----------------------------------------------------------------
echo.
dotnet clean "%PROJECT%" --configuration %CONFIG% --nologo
if %errorlevel% neq 0 (
    echo  [WARN] dotnet clean returned exit code %errorlevel%. Continuing...
)

:: -- Step 2: Delete bin\ and obj\ ------------------------------
echo.
echo  ----------------------------------------------------------------
echo   STEP 2 of 3 -- Delete bin\ and obj\
echo  ----------------------------------------------------------------
echo.
if exist "%BIN_DIR%" (
    echo  [..] Deleting bin\...
    rmdir /s /q "%BIN_DIR%"
    echo  [OK]  Deleted: src\GlobalDataCreatorETL\bin\
) else (
    echo  [--]  bin\ already absent.
)
if exist "%OBJ_DIR%" (
    echo  [..] Deleting obj\...
    rmdir /s /q "%OBJ_DIR%"
    echo  [OK]  Deleted: src\GlobalDataCreatorETL\obj\
) else (
    echo  [--]  obj\ already absent.
)

:: -- Optional: clean publish folder ----------------------------
if exist "%PUBLISH_DIR%" (
    echo.
    echo  [?]  Found publish\ directory. Delete it too? [Y/N]
    set /p "DEL_PUBLISH=  Choice: "
    if /i "!DEL_PUBLISH!"=="Y" (
        rmdir /s /q "%PUBLISH_DIR%"
        echo  [OK]  Deleted: publish\
    ) else (
        echo  [--]  Kept: publish\
    )
)
echo.

:: -- Step 3: Build ---------------------------------------------
echo  ----------------------------------------------------------------
echo   STEP 3 of 3 -- dotnet build  [%CONFIG%]
echo  ----------------------------------------------------------------
echo.

dotnet build "%PROJECT%" ^
    --configuration %CONFIG% ^
    -p:Platform=x64 ^
    --nologo ^
    -v minimal

if %errorlevel% neq 0 (
    echo.
    echo  ============================================================
    echo   BUILD FAILED  (exit code %errorlevel%)
    echo  ============================================================
    echo.
    echo   Common fixes:
    echo     Missing packages  :  dotnet restore
    echo     Syntax error      :  check reported file + line number
    echo     SDK not found     :  verify .NET 8 SDK is installed
    echo.
    pause
    exit /b %errorlevel%
)

:: -- Summary ---------------------------------------------------
echo.
echo  ============================================================
echo   BUILD SUCCEEDED
echo  ============================================================
echo.
echo   Output : src\GlobalDataCreatorETL\bin\Release\net8.0-windows\
echo.
echo   Next steps:
echo     - Portable self-contained package : scripts\build-self-contained.bat
echo     - Launch published exe            : run.bat
echo.
echo  ============================================================
echo.
pause
endlocal
