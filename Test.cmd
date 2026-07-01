@echo off
dotnet run --project "%~dp0src\Tools\RunTests" -- %*
