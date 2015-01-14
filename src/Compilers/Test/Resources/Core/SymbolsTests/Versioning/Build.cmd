REM Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

vbc /t:library /out:V1\C.dll /keyfile:Key.snk /vbruntime- Version1.vb
vbc /t:library /out:V2\C.dll /keyfile:Key.snk /vbruntime- Version2.vb
csc /target:library /out:AR_SA\Culture.dll AR_SA.cs
csc /target:library /out:EN_US\Culture.dll EN_US.cs
