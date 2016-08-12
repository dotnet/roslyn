' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Class SyntaxNodeCache
        Friend Shared Function TryGetNode(kind As Integer, context As ISyntaxFactoryContext, child1 As GreenNode, ByRef hash As Integer) As GreenNode
            Return CommonSyntaxNodeCache.TryGetNode(kind, child1, GetFlags(context), hash)
        End Function

        Friend Shared Function TryGetNode(kind As Integer, context As ISyntaxFactoryContext, child1 As GreenNode, child2 As GreenNode, ByRef hash As Integer) As GreenNode
            Return CommonSyntaxNodeCache.TryGetNode(kind, child1, child2, GetFlags(context), hash)
        End Function

        Friend Shared Function TryGetNode(kind As Integer, context As ISyntaxFactoryContext, child1 As GreenNode, child2 As GreenNode, child3 As GreenNode, ByRef hash As Integer) As GreenNode
            Return CommonSyntaxNodeCache.TryGetNode(kind, child1, child2, child3, GetFlags(context), hash)
        End Function

        Private Shared Function GetFlags(context As ISyntaxFactoryContext) As GreenNode.NodeFlags
            Dim flags = CommonSyntaxNodeCache.GetFlags()
            flags = VisualBasicSyntaxNode.SetFactoryContext(flags, context)
            Return flags
        End Function
    End Class
End Namespace