#!/bin/bash
# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

build_root=$(cd "$(dirname "$0")" && pwd)
cd $build_root

restore=0
assembly=0
test=0
packaging=0
mes=0

# Package restore
dotnet restore

# bugbug - dotnet preview2 requires an absolute path for publish or include files are ignored
publish=$build_root/buildOutput

# clean publish folder 
echo Erase publish folder: $publish
rm -rdf $publish/*

# build station
echo "build Station"
cd ./Station
dotnet build && echo "Station built!" || assembly = $?
dotnet publish -o $publish && echo "Station published!" || assembly = $?
cd $build_root

# build MES
echo "build MES"
cd ./MES
dotnet build && echo "MES built!" || mes = $?
dotnet publish -o $publish && echo "MES published!" || mes = $?
cd $build_root

# create log and cert folder
mkdir ./Logs
mkdir ./OPC\ Foundation

# run Factory
echo "run Factory"
cd $publish
nohup nice dotnet Station.dll Munich/ProductionLine0/Assembly 51210 200 yes 1> ../Logs/AssemblyStation.out 2> ../Logs/AssemblyStation.err < /dev/null &
nohup nice dotnet Station.dll Munich/ProductionLine0/Test 51214 100 no 1> ../Logs/TestStation.out 2> ../Logs/TestStation.err < /dev/null &
nohup nice dotnet Station.dll Munich/ProductionLine0/Packaging 51215 150 no 1> ../Logs/PackageStation.out 2> ../Logs/PackageStation.err < /dev/null &

# wait until the OPC servers started fully up
echo "Waiting 10s for stations to start up"
sleep 10
echo "Start MES"

nohup nice dotnet MES.dll 1> ../Logs/MES.out 2> ../Logs/MES.err </dev/null &
cd $build_root

echo "Factory running"
jobs

echo Factory Jobs started
echo Stop factory using: killall -i -I dotnet
