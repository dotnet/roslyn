REM Run the node generator to create new bound tree node definitions.
REM You must run this in the directory containing Generated.cs.
call tf checkout Generated.cs
@echo on
..\..\..\..\..\Binaries\Debug\BoundTreeGenerator.exe CSharp BoundNodes.xml Generated.cs
