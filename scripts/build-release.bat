@echo off
chcp 65001 >nul
setlocal
set VER=%1
if "%VER%"=="" set VER=1.0.0
powershell -ExecutionPolicy Bypass -File "%~dp0build-release.ps1" -Version %VER%
pause
