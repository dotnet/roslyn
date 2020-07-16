REM Licensed to the .NET Foundation under one or more agreements.
REM The .NET Foundation licenses this file to you under the MIT license.
REM See the LICENSE file in the project root for more information.

vbc /t:library /out:V1\C.dll /keyfile:Key.snk /vbruntime- Version1.vb
vbc /t:library /out:V2\C.dll /keyfile:Key.snk /vbruntime- Version2.vb
csc /target:library /out:AR_SA\Culture.dll AR_SA.cs
csc /target:library /out:EN_US\Culture.dll EN_US.cs
