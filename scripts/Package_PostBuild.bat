@echo off
setlocal enabledelayedexpansion
title Novarin Packager
echo Creating directory (if it doesn't exist)
mkdir "..\Package App\"

echo Novarin Launcher (NovaLauncher)

echo Copy^: (Release) MergedApp.exe ^-^> (Package App) NovaLauncher.exe
copy "..\Release\MergedApp.exe" "..\Package App\NovaLauncher.exe"

echo Novarin RPC Manager (NovarinRPCManager)

echo Copy^: (Release) MergedRPCApp.exe ^-^> (Package App) NovarinRPCManager.exe
copy "..\Release\MergedRPCApp.exe" "..\Package App\NovarinRPCManager.exe"

echo Copy^: Required References ^-^> Package App
copy "..\..\NovarinRPCManager\References\discord_game_sdk.dll" "..\Package App\"
copy "..\..\NovarinRPCManager\References\discord_game_sdk.so" "..\Package App\"

echo NovaLauncher.zip

echo Compress: Package App
powershell -Command "Compress-Archive -Path '..\Package App\*' -DestinationPath '..\Package App\NovaLauncher.zip' -Force"

echo Compress: Obtain SHA256
powershell -Command "(Get-FileHash '..\Package App\NovaLauncher.zip' -Algorithm SHA256).Hash.ToLower() > '..\Package App\NovaLauncher.sha256.txt'"
type "..\Package App\NovaLauncher.sha256.txt"

echo Done.
exit /b 0