REM Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

tf edit W1.winmd W2.winmd WB.winmd WB_Version1.winmd
ilasm W1.il /mdv="WindowsRuntime 1.2" /msv:1.1 /dll /out=W1.winmd
ilasm W2.il /mdv="WindowsRuntime 1.2" /msv:1.1 /dll /out=W2.winmd
ilasm WB.il /mdv="WindowsRuntime 1.2" /msv:1.1 /dll /out=WB.winmd
ilasm WB_Version1.il /mdv="WindowsRuntime 1.2" /msv:1.1 /dll /out=WB_Version1.winmd
ilasm WImpl.il /mdv="WindowsRuntime 1.3" /msv:1.1 /dll /out=WImpl.winmd
