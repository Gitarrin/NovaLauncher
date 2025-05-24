@echo off
setlocal enabledelayedexpansion
title Novarin Packager

if "%1"=="" (
	echo Error: No target framework specified.
	exit /b 1
)

echo Removing directory (if it exists)
rmdir /q /s "..\..\Package App\%1"

exit /b 0