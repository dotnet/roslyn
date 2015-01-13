' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    <Flags>
    Friend Enum DebugAttributes As Byte
        None = 0
        CompilerGeneratedAttribute = 1
        DebuggerHiddenAttribute = 2
        DebuggerNonUserCodeAttribute = 4
    End Enum
End Namespace