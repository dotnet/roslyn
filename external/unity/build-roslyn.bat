@ECHO OFF

SET myProgramFiles=%ProgramFiles(x86)%
IF NOT "%myProgramFiles%"=="" GOTO :BUILD

SET myProgramFiles=%ProgramFiles%
IF NOT "%myProgramFiles%"=="" GOTO :BUILD

SET myProgramFiles=C:\Program Files (x86)

:BUILD

SET msbuild=%myProgramFiles%\MSBuild\14.0\bin\MSBuild.exe
SET solutionPath=%~dp0\RoslynBuilder\RoslynBuilder.sln
SET roslynBuilder=%~dp0\..\..\Artifacts\RoslynBuilder\RoslynBuilder.exe

ECHO Using %msbuild% to build %solutionPath%

"%msbuild%" "%solutionPath%" /v:m /p:Configuration=Release /p:Platform="Any CPU"
IF %ERRORLEVEL% EQU 0 GOTO :RUNBUILDER

ECHO Failed to build %solutionPath%
GOTO :EXIT

:RUNBUILDER

ECHO Running roslynBuilder

"%roslynBuilder%"
IF %ERRORLEVEL% EQU 0 GOTO :EXIT

ECHO RoslynBuilder failed
GOTO :EXIT

:EXIT

EXIT /B %ERRORLEVEL%