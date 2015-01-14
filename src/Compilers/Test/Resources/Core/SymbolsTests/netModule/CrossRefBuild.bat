REM Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

vbc CrossRefModule1.vb /target:module /vbruntime-
vbc CrossRefModule2.vb /target:module /vbruntime- /addmodule:CrossRefModule1.netmodule
vbc CrossRefLib.vb /target:library /vbruntime- /addmodule:CrossRefModule1.netmodule,CrossRefModule2.netmodule
