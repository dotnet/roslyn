' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Roslyn.Diagnostics.Analyzers.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicSpecializedEnumerableCreationAnalyzer
        Inherits SpecializedEnumerableCreationAnalyzer

        Protected Overrides Function GetCodeBlockStartedAnalyzer(genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol) As AbstractCodeBlockStartedAnalyzer
            Return New CodeBlockStartedAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
        End Function

        Private NotInheritable Class CodeBlockStartedAnalyzer
            Inherits AbstractCodeBlockStartedAnalyzer

            Public Sub New(genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
                MyBase.New(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            End Sub

            Protected Overrides Function GetSyntaxAnalyzer(genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol) As AbstractSyntaxAnalyzer
                Return New SyntaxAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            End Function
        End Class

        Private NotInheritable Class SyntaxAnalyzer
            Inherits AbstractSyntaxAnalyzer
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Public Sub New(genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
                MyBase.New(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            End Sub

            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return ImmutableArray.Create(SyntaxKind.ReturnStatement)
                End Get
            End Property

            Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                Dim expressionsToAnalyze = node.DescendantNodes().Where(Function(n) ShouldAnalyzeExpression(n, semanticModel))

                For Each expression In expressionsToAnalyze
                    Select Case expression.VisualBasicKind()
                        Case SyntaxKind.ArrayCreationExpression
                            AnalyzeArrayCreationExpression(DirectCast(expression, ArrayCreationExpressionSyntax), addDiagnostic)
                        Case SyntaxKind.CollectionInitializer
                            AnalyzeCollectionInitializerExpression(DirectCast(expression, CollectionInitializerSyntax), expression, addDiagnostic)
                        Case SyntaxKind.SimpleMemberAccessExpression
                            AnalyzeMemberAccessName(DirectCast(expression, MemberAccessExpressionSyntax).Name, semanticModel, addDiagnostic)
                    End Select
                Next
            End Sub

            Private Function ShouldAnalyzeExpression(expression As SyntaxNode, semanticModel As SemanticModel) As Boolean
                Select Case expression.VisualBasicKind()
                    Case SyntaxKind.ArrayCreationExpression,
                         SyntaxKind.CollectionInitializer
                        Return ShouldAnalyzeArrayCreationExpression(expression, semanticModel)
                    Case SyntaxKind.SimpleMemberAccessExpression
                        Return True
                End Select

                Return False
            End Function

            Private Shared Sub AnalyzeArrayCreationExpression(arrayCreationExpression As ArrayCreationExpressionSyntax, addDiagnostic As Action(Of Diagnostic))
                If arrayCreationExpression.RankSpecifiers.Count = 1 Then

                    ' Check for explicit specification of empty or singleton array
                    Dim literalRankSpecifier = DirectCast(arrayCreationExpression.RankSpecifiers(0).ChildNodes() _
                        .SingleOrDefault(Function(n) n.VisualBasicKind() = SyntaxKind.NumericLiteralExpression),
                        LiteralExpressionSyntax)

                    If literalRankSpecifier IsNot Nothing Then
                        Debug.Assert(literalRankSpecifier.Token.Value IsNot Nothing)
                        AnalyzeArrayLength(DirectCast(literalRankSpecifier.Token.Value, Integer), arrayCreationExpression, addDiagnostic)
                        Return
                    End If
                End If

                AnalyzeCollectionInitializerExpression(arrayCreationExpression.Initializer, arrayCreationExpression, addDiagnostic)
            End Sub

            Private Shared Sub AnalyzeCollectionInitializerExpression(initializer As CollectionInitializerSyntax, arrayCreationExpression As SyntaxNode, addDiagnostic As Action(Of Diagnostic))
                ' Check length of initializer list for empty or singleton array
                If initializer IsNot Nothing Then
                    AnalyzeArrayLength(initializer.Initializers.Count, arrayCreationExpression, addDiagnostic)
                End If
            End Sub
        End Class
    End Class
End Namespace
