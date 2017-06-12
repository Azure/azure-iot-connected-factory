@REM Copyright (c) Microsoft. All rights reserved.
@REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

@setlocal EnableExtensions EnableDelayedExpansion
@echo off

rem // default build options
set build-clean=0
set build-config=Release

set current-path=%~dp0
rem // remove trailing slash
set current-path=%current-path:~0,-1%

set build-root=%current-path%
rem // resolve to fully qualified path
for %%i in ("%build-root%") do set build-root=%%~fi

echo building %build-root%

rem -----------------------------------------------------------------------------
rem -- check prerequisites
rem -----------------------------------------------------------------------------

rem ensure dotnet.exe exists
where /q dotnet.exe
if errorlevel 1 goto :NeedDotnet
goto args-loop

:NeedDotnet
@Echo Factory simulation needs DotNet from 
@Echo https://www.microsoft.com/net/core
exit /b 1

rem -----------------------------------------------------------------------------
rem -- parse script arguments
rem -----------------------------------------------------------------------------

:args-loop
if "%1" equ "" goto args-done
if "%1" equ "-c" goto arg-build-clean
if "%1" equ "--config" goto arg-build-config
call :usage && exit /b 1

:arg-build-clean
set build-clean=1
goto args-continue

:arg-build-config
shift
if "%1" equ "" call :usage && exit /b 1
set build-config=%1
goto args-continue

:args-continue
shift
goto args-loop

:args-done

cd %build-root%

rem -----------------------------------------------------------------------------
rem -- restore packets and clean publish folder
rem -----------------------------------------------------------------------------

@REM Package restore
for /f %%p in ('dir /s /b *.csproj') do ( 
    dotnet restore  %%p
    if not !ERRORLEVEL!==0 exit /b !ERRORLEVEL!
)

@REM bugbug - dotnet preview2 requires an absolute path for publish or include files are ignored
set publish=%build-root%\buildOutput

if %build-clean%==1 (
    @REM clean publish folder 
    echo Erase publish folder: %publish%
    del /F/S/Q "%publish%\*.*" > nul 2>&1
    del /F/S/Q "%build-root%\Logs" > nul 2>&1
    del /F/S/Q "%build-root%\Shared\OPC Foundation" > nul 2>&1
)

rem -----------------------------------------------------------------------------
rem -- build Station, MES and Publisher
rem -----------------------------------------------------------------------------

@REM build station
echo "build Station"
cd %build-root%\Station
dotnet build 
dotnet publish -o %publish% -c %build-config%
cd %build-root%

@REM build MES
echo "build MES"
cd %build-root%\MES
dotnet build 
dotnet publish -o %publish% -c %build-config%
cd %build-root%

@REM build CreateCerts
echo "build CreateCerts
cd %build-root%\CreateCerts
dotnet build 
dotnet publish -o %publish% -c %build-config%
cd %build-root%

@REM create log and cert folder
if not exist "%build-root%\Logs" mkdir "%build-root%\Logs"
if not exist "%build-root%\Shared\OPC Foundation" mkdir "%build-root%\Shared\OPC Foundation"

cd %build-root%

echo "Simulation built"

rem -----------------------------------------------------------------------------
rem -- build done
rem -----------------------------------------------------------------------------

goto :eof

rem -----------------------------------------------------------------------------
rem -- subroutines
rem -----------------------------------------------------------------------------

:usage
echo BuildSimulation.cmd [options]
echo options:
echo  -c, --clean             delete artifacts from previous build before building
echo  --config ^<value^>      [Release] build configuration (e.g. Debug, Release)
goto :eof






