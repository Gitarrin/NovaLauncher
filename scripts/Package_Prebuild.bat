@echo off
setlocal enabledelayedexpansion
title Novarin Packager

echo Removing directory (if it exists)
rmdir /q /s "..\Package App\"

echo Creating directory
mkdir "..\Package App\"
mkdir "..\Package App\net35\"
mkdir "..\Package App\net48\"