' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' For nodes that can generate an <see cref="IInvalidOperation"/>, this allows the Lazy implementation
    ''' to get the children of this node on demand.
    ''' </summary>
    Friend Interface IBoundInvalidNode
        ReadOnly Property InvalidNodeChildren As ImmutableArray(Of BoundNode)
    End Interface
End Namespace
