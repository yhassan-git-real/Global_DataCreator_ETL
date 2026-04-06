@echo off
setlocal

:: ============================================================
::  run.bat -- Launch Global DataCreator ETL
::  Location : repo root
::  Target   : publish\win-x64-standalone\GlobalDataCreatorETL.exe
:: ============================================================

set "ROOT=%~dp0"
set "EXE=%ROOT%publish\win-x64-standalone\GlobalDataCreatorETL.exe"

echo.
echo  ============================================================
echo   GLOBAL_DATACREATOR_ETL  ^|  Launcher
echo  ============================================================
echo.

:: -- Verify the executable exists ------------------------------
if not exist "%EXE%" (
    echo  [ERROR] Executable not found.
    echo.
    echo          Expected:
    echo          %EXE%
    echo.
    echo  Build it first by running:
    echo          scripts\build-self-contained.bat
    echo.
    pause
    exit /b 1
)

:: -- Launch ----------------------------------------------------
echo  [OK]  Executable found.
echo  [^>^>]  Launching application...
echo.
start "" "%EXE%"
echo  [OK]  Application started.
echo.
endlocal
