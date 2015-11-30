﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    <Flags()>
    Friend Enum TypeParameterConstraintKind
        None = 0
        ReferenceType = 1
        ValueType = 2
        Constructor = 4
    End Enum

End Namespace
