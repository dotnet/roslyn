@echo off
call "%~dp0\common\dotnet.cmd" run --file "%~dp0\generate-compiler-code.cs" %* 

