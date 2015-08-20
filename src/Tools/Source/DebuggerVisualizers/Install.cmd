@echo off
set VISUALIZERS=%USERPROFILE%\Documents\Visual Studio 2015\Visualizers
set BIN=%~dp0..\..\..\..\Binaries\Debug

copy /y "%BIN%\Roslyn.DebuggerVisualizers.dll" "%VISUALIZERS%"
copy /y "%BIN%\Roslyn.Test.PdbUtilities.dll" "%VISUALIZERS%"
copy /y "%BIN%\System.Reflection.Metadata.dll" "%VISUALIZERS%"
copy /y "%BIN%\System.Collections.Immutable.dll" "%VISUALIZERS%"
copy /y "%BIN%\Microsoft.DiaSymReader.dll" "%VISUALIZERS%"

echo Close all VS instances to finish.