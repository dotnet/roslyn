@echo off
call "%~dp0\common\dotnet.cmd" run "%~dp0\generate-compiler-code.cs" %* 

