' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Interface IBoundInvalidNode
        ReadOnly Property InvalidNodeChildren As ImmutableArray(Of BoundNode)
    End Interface
End Namespace
