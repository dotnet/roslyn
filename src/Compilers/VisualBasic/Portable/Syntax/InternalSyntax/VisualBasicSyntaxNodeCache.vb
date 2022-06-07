' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Module VisualBasicSyntaxNodeCache
        Friend Function TryGetNode(kind As Integer, child1 As GreenNode, context As ISyntaxFactoryContext, ByRef hash As Integer) As GreenNode
            Return SyntaxNodeCache.TryGetNode(kind, child1, GetNodeFlags(context), hash)
        End Function

        Friend Function TryGetNode(kind As Integer, child1 As GreenNode, child2 As GreenNode, context As ISyntaxFactoryContext, ByRef hash As Integer) As GreenNode
            Return SyntaxNodeCache.TryGetNode(kind, child1, child2, GetNodeFlags(context), hash)
        End Function

        Friend Function TryGetNode(kind As Integer, child1 As GreenNode, child2 As GreenNode, child3 As GreenNode, context As ISyntaxFactoryContext, ByRef hash As Integer) As GreenNode
            Return SyntaxNodeCache.TryGetNode(kind, child1, child2, child3, GetNodeFlags(context), hash)
        End Function

        Private Function GetNodeFlags(context As ISyntaxFactoryContext) As GreenNode.NodeFlags
            Dim flags = SyntaxNodeCache.GetDefaultNodeFlags()
            If context.IsWithinAsyncMethodOrLambda Then
                flags = flags Or GreenNode.NodeFlags.FactoryContextIsInAsync
            End If

            If context.IsWithinIteratorContext Then
                flags = flags Or GreenNode.NodeFlags.FactoryContextIsInIterator
            End If

            Return flags
        End Function
    End Module
End Namespace
