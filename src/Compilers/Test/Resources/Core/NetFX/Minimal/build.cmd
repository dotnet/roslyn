@REM Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

csc /target:library /nostdlib /noconfig /keyfile:Key.snk /out:mincorlib.dll mincorlib.cs
csc /target:library /nostdlib /noconfig /keyfile:Key.snk /r:mincorlib.dll /out:minasync.dll minasync.cs
