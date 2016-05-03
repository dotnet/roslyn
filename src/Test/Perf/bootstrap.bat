@echo off

call "%VS140COMNTOOLS%VsDevCmd.bat"

set MSBuild=%ProgramFiles%\MSBuild\14.0\bin\msbuild.exe
if not exist "%MSBuild%" set MSBuild=%ProgramFiles(x86)%\MSBuild\14.0\bin\msbuild.exe

pushd %~dp0

if not exist "%~dp0..\..\..\..\Init.cmd" (
    :: Update Open Roslyn
    cd ..\..\..\
    git remote update
    git pull origin master

    call Restore.cmd
    echo Build Open Roslyn
    "%MSBuild%" "Roslyn.sln" /p:Configuration=Release
) else (
    :: Update Open Roslyn
    cd ..\..\..\
    git remote update
    git pull origin master

    :: Update Closed Roslyn
    cd ..\
    git remote update
    git pull origin master

    call Init.cmd
    echo Build Closed Roslyn
    "%MSBuild%" "Roslyn.sln" /p:Configuration=Release

    :: Start the perf tests
    cd Open\Binaries\Release\Perf\infra
    csi automation.csx
)

popd

exit /b 0