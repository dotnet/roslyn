
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Operations
    Friend Interface IBoundNodeWithIOperationChildren
        ''' <summary>
        ''' An array of child bound nodes.
        ''' </summary>
        ''' <remarks>Note that any of the child nodes may be null.</remarks>
        ReadOnly Property Children As ImmutableArray(Of BoundNode)
    End Interface
End Namespace