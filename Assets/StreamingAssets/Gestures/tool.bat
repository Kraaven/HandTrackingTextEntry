@echo off
set "file=%~1"
set "letter=%~2"

powershell -Command "(Get-Content '%file%') -replace ',\"Name\":\"\"', ',\"Name\":\"%letter%\"' | Set-Content '%file%'"
echo Task complete for %file% using letter %letter%.