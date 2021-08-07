@echo off
break on
pushd %~dp0

powershell.exe -noprofile -ExecutionPolicy RemoteSigned .\shell\sys.ps1 -Mode stop

popd
echo exit /b 0
