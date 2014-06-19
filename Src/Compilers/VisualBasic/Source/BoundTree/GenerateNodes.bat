REM Run the node generator to create new bound tree node definitions.
REM You must run this in the directory containing Generated.vb.
tf checkout Generated.vb 
..\..\..\..\..\Binaries\Debug\BoundTreeGenerator.exe VB BoundNodes.xml Generated.vb