
' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
