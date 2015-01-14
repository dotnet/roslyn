REM Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

REM Run the node generator to create new bound tree node definitions.
REM You must run this in the directory containing Generated.cs.
call tf checkout Generated.cs
@echo on
..\..\..\..\..\Binaries\Debug\BoundTreeGenerator.exe CSharp BoundNodes.xml Generated.cs
