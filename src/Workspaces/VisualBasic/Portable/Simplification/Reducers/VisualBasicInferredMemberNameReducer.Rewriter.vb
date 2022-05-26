' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicInferredMemberNameReducer

        Private Class Rewriter
            Inherits AbstractReductionRewriter

            Public Sub New(pool As ObjectPool(Of IReductionRewriter))
                MyBase.New(pool)
            End Sub

            Private ReadOnly s_simplifyTupleName As Func(Of SimpleArgumentSyntax, SemanticModel, SimplifierOptions, CancellationToken, SimpleArgumentSyntax) = AddressOf SimplifyTupleName
            Private ReadOnly s_simplifyNamedFieldInitializer As Func(Of NamedFieldInitializerSyntax, SemanticModel, SimplifierOptions, CancellationToken, SyntaxNode) = AddressOf SimplifyNamedFieldInitializer

            Private Function SimplifyNamedFieldInitializer(node As NamedFieldInitializerSyntax, arg2 As SemanticModel, options As SimplifierOptions, arg4 As CancellationToken) As SyntaxNode
                If CanSimplifyNamedFieldInitializer(node) Then
                    Return SyntaxFactory.InferredFieldInitializer(node.Expression).WithTriviaFrom(node)
                End If

                Return node
            End Function

            Private Function SimplifyTupleName(
                node As SimpleArgumentSyntax,
                semanticModel As SemanticModel,
                options As SimplifierOptions,
                cancellationToken As CancellationToken
                ) As SimpleArgumentSyntax

                If CanSimplifyTupleName(node, ParseOptions) Then
                    Return node.WithNameColonEquals(Nothing).WithTriviaFrom(node)
                End If

                Return node
            End Function

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
