@echo off
cd /d %~dp0
dotnet publish MeuIPTVPro.csproj -c Release -r win-x64 --self-contained true -o publish
pause
