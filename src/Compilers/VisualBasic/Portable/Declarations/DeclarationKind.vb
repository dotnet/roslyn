' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Enum DeclarationKind As Byte
        [Namespace]
        [Class]
        [Interface]
        [Structure]
        [Enum]
        [Delegate]
        [Module]
        Script
        Submission
        ImplicitClass
        EventSyntheticDelegate
    End Enum

End Namespace
