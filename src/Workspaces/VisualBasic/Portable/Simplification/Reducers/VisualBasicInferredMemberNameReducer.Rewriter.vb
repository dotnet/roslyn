' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicInferredMemberNameReducer

        Private Class Rewriter
            Inherits AbstractReductionRewriter

            Public Sub New(pool As ObjectPool(Of IReductionRewriter))
                MyBase.New(pool)
            End Sub

            Public Overrides Function VisitSimpleArgument(node As SimpleArgumentSyntax) As SyntaxNode
                CancellationToken.ThrowIfCancellationRequested()

                Dim newNode = MyBase.VisitSimpleArgument(node)

                If node.IsParentKind(SyntaxKind.TupleExpression) Then
                    Return SimplifyNode(
                        node,
                        parentNode:=node.Parent,
                        newNode:=newNode,
                        simplifyFunc:=s_simplifyTupleName)
                End If

                Return newNode
            End Function

            Public Overrides Function VisitNamedFieldInitializer(node As NamedFieldInitializerSyntax) As SyntaxNode
                Dim newNode = MyBase.VisitNamedFieldInitializer(node)

                Return SimplifyNode(
                    node,
                    parentNode:=node.Parent,
                    newNode:=newNode,
                    simplifyFunc:=s_simplifyNamedFieldInitializer)
            End Function
        End Class
    End Class
End Namespace