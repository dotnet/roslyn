REM Licensed to the .NET Foundation under one or more agreements.
REM The .NET Foundation licenses this file to you under the MIT license.
REM See the LICENSE file in the project root for more information.

vbc CrossRefModule1.vb /target:module /vbruntime-
vbc CrossRefModule2.vb /target:module /vbruntime- /addmodule:CrossRefModule1.netmodule
vbc CrossRefLib.vb /target:library /vbruntime- /addmodule:CrossRefModule1.netmodule,CrossRefModule2.netmodule
