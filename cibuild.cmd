call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\vcvarsall.bat" x86

REM Build the compiler so we can self host it for the full build
src\.nuget\NuGet.exe restore src\Toolset.sln -packagesdirectory packages
msbuild /nologo /v:m /m src/Compilers/Core/VBCSCompiler/VBCSCompiler.csproj
msbuild /nologo /v:m /m src/Compilers/CSharp/csc2/csc2.csproj
msbuild /nologo /v:m /m src/Compilers/VisualBasic/vbc2/vbc2.csproj

mkdir Binaries\Bootstrap
move Binaries\Debug\* Binaries\Bootstrap
msbuild /v:m /t:Clean src/Toolset.sln
taskkill /F /IM vbcscompiler.exe

msbuild /v:m /m /p:BootstrapBuildPath=%~dp0\Binaries\Bootstrap BuildAndTest.proj /p:CIBuild=true
if ERRORLEVEL 1 (
    taskkill /F /IM vbcscompiler.exe
    echo Build failed
    exit /b 1
)

REM Kill any instances of VBCSCompiler.exe to release locked files;
REM otherwise future CI runs may fail while trying to delete those files.
taskkill /F /IM vbcscompiler.exe

REM It is okay and expected for taskkill to fail (it's a cleanup routine).  Ensure
REM caller sees successful exit.
exit /b 0
