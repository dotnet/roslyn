' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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
