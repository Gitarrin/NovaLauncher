@echo off
setlocal enabledelayedexpansion
title Novarin Packager

if "%1"=="" (
	echo Error: No target framework specified.
	exit /b 1
)

if "%1"=="net35" (
	set "TARGET_PLATFORM=%1"
	set "TARGET_NAME=.NET 3.5"
	set "TARGET_IL=v2,C:\Windows\Microsoft.NET\Framework\v2.0.50727"
) else if "%1"=="net48" (
	set "TARGET_PLATFORM=%1"
	set "TARGET_NAME=.NET 4.8"
	set "TARGET_IL=v4,C:\Windows\Microsoft.NET\Framework\v4.0.30319"
) else (
	echo Error: Unsupported framework "%1".
	exit /b 2
)
echo Info: Packing for %TARGET_NAME%...

if "%2"=="" (
	echo Error: Please set the Target EXE.
	exit /b 3
)
set "OUT_EXE=%2"

echo Creating directory (if it doesn't exist)
mkdir "..\..\Package App\%TARGET_PLATFORM%\"

echo %TARGET_PLATFORM%: Merging App...
"..\..\..\packages\ILMerge.3.0.41\tools\net452\ILMerge.exe" /target:winexe /out:"%cd%\MergedApp.exe" "%cd%\%OUT_EXE%" "%cd%\Newtonsoft.Json.dll" "%cd%\ICSharpCode.SharpZipLib.dll" "%cd%\CommandLine.dll" /targetplatform:%TARGET_IL%

echo %TARGET_PLATFORM%: Copying Merged App...
copy "%cd%\MergedApp.exe" "..\..\Package App\%TARGET_PLATFORM%\%OUT_EXE%"

echo NovarinRPCManager: Retrieve Novarin RPC Manager
copy "..\MergedRPCApp.exe" "..\..\Package App\%TARGET_PLATFORM%\NovarinRPCManager.exe"

echo NovarinRPCManager: Retrieve Novarin RPC Manager References
copy "..\..\..\NovarinRPCManager\References\discord_game_sdk.*" "..\..\Package App\%TARGET_PLATFORM%\"

echo %TARGET_PLATFORM% Compress: Package App
powershell -Command "Compress-Archive -Path '..\..\Package App\%TARGET_PLATFORM%\*' -DestinationPath '..\..\Package App\%TARGET_PLATFORM%\NovaLauncher.zip' -Force"

echo %TARGET_PLATFORM% Compress: Obtain SHA256
powershell -Command "(Get-FileHash '..\..\Package App\%TARGET_PLATFORM%\NovaLauncher.zip' -Algorithm SHA256).Hash.ToLower() > '..\..\Package App\%TARGET_PLATFORM%\NovaLauncher.sha256.txt'"
type "..\..\Package App\%TARGET_PLATFORM%\NovaLauncher.sha256.txt"

echo %TARGET_PLATFORM%: Done!

exit /b 0