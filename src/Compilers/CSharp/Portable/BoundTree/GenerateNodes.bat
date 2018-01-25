REM Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

REM Run the node generator to create new bound tree node definitions.
REM You must run this in the directory containing BoundNodes.xml

@echo on
dotnet ..\..\..\..\..\Binaries\Debug\Exes\CompilersBoundTreeGenerator\BoundTreeGenerator.dll CSharp BoundNodes.xml ..\Generated\BoundNodes.xml.Generated.cs
