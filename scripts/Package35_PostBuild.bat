@echo off
setlocal enabledelayedexpansion
title Novarin Packager
echo Creating directory (if it doesn't exist)
mkdir "..\Package App\net35\"

echo Novarin Launcher (NovaLauncher) for .NET 3.5

echo Copy^: (Release) MergedApp.exe ^-^> (Package App) NovaLauncher.exe
copy "..\Release35\MergedApp.exe" "..\Package App\net35\NovaLauncher.exe"

echo Novarin RPC Manager (NovarinRPCManager)

echo Copy^: (Release) MergedRPCApp.exe ^-^> (Package App) NovarinRPCManager.exe
copy "..\Release\MergedRPCApp.exe" "..\Package App\net35\NovarinRPCManager.exe"

echo Copy^: Required References ^-^> Package App
copy "..\..\NovarinRPCManager\References\discord_game_sdk.dll" "..\Package App\net35\"
copy "..\..\NovarinRPCManager\References\discord_game_sdk.so" "..\Package App\net35\"

echo NovaLauncher.zip

echo Compress: Package App
powershell -Command "Compress-Archive -Path '..\Package App\net35\*' -DestinationPath '..\Package App\net35\NovaLauncher.zip' -Force"

echo Compress: Obtain SHA256
powershell -Command "(Get-FileHash '..\Package App\net35\NovaLauncher.zip' -Algorithm SHA256).Hash.ToLower() > '..\Package App\net35\NovaLauncher.sha256.txt'"
type "..\Package App\net35\NovaLauncher.sha256.txt"

echo Done.
exit /b 0