' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicSpecializedEnumerableCreationAnalyzer
        Inherits SpecializedEnumerableCreationAnalyzer

        Protected Overrides Sub GetCodeBlockStartedAnalyzer(context As CompilationStartAnalysisContext, genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
            context.RegisterCodeBlockStartAction(Of SyntaxKind)(AddressOf New CodeBlockStartedAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol).Initialize)
        End Sub

        Private NotInheritable Class CodeBlockStartedAnalyzer
            Inherits AbstractCodeBlockStartedAnalyzer(Of SyntaxKind)

            Public Sub New(genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
                MyBase.New(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            End Sub

            Protected Overrides Sub GetSyntaxAnalyzer(context As CodeBlockStartAnalysisContext(Of SyntaxKind), genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
                context.RegisterSyntaxNodeAction(AddressOf New SyntaxAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol).AnalyzeNode, SyntaxKind.ReturnStatement)
            End Sub
        End Class

        Private NotInheritable Class SyntaxAnalyzer
            Inherits AbstractSyntaxAnalyzer

            Public Sub New(genericEnumerableSymbol As INamedTypeSymbol, genericEmptyEnumerableSymbol As IMethodSymbol)
                MyBase.New(genericEnumerableSymbol, genericEmptyEnumerableSymbol)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Dim expressionsToAnalyze = context.Node.DescendantNodes().Where(Function(n) ShouldAnalyzeExpression(n, context.SemanticModel, context.CancellationToken))

                For Each expression In expressionsToAnalyze
                    Select Case expression.Kind()
                        Case SyntaxKind.ArrayCreationExpression
                            AnalyzeArrayCreationExpression(DirectCast(expression, ArrayCreationExpressionSyntax), AddressOf context.ReportDiagnostic)
                        Case SyntaxKind.CollectionInitializer
                            AnalyzeCollectionInitializerExpression(DirectCast(expression, CollectionInitializerSyntax), expression, AddressOf context.ReportDiagnostic)
                        Case SyntaxKind.SimpleMemberAccessExpression
                            AnalyzeMemberAccessName(DirectCast(expression, MemberAccessExpressionSyntax).Name, context.SemanticModel, AddressOf context.ReportDiagnostic, context.CancellationToken)
                    End Select
                Next
            End Sub

            Private Function ShouldAnalyzeExpression(expression As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
                Select Case expression.Kind()
                    Case SyntaxKind.ArrayCreationExpression
                        Return ShouldAnalyzeArrayCreationExpression(expression, semanticModel, cancellationToken)

                    Case SyntaxKind.CollectionInitializer
                        Dim typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken)

                        If typeInfo.Type IsNot Nothing Then
                            Return ShouldAnalyzeArrayCreationExpression(expression, semanticModel, cancellationToken)
                        End If

                        ' Get TypeInfo of the array literal in context without the target type
                        Dim speculativeTypeInfo = semanticModel.GetSpeculativeTypeInfo(expression.SpanStart, expression, SpeculativeBindingOption.BindAsExpression)
                        Dim arrayType = TryCast(speculativeTypeInfo.ConvertedType, IArrayTypeSymbol)

                        Return arrayType IsNot Nothing AndAlso
                               arrayType.Rank = 1 AndAlso
                               typeInfo.ConvertedType IsNot Nothing AndAlso
                               typeInfo.ConvertedType.OriginalDefinition.Equals(Me.GenericEnumerableSymbol)

                    Case SyntaxKind.SimpleMemberAccessExpression
                        Return True
                End Select

                Return False
            End Function

            Private Shared Sub AnalyzeArrayCreationExpression(arrayCreationExpression As ArrayCreationExpressionSyntax, addDiagnostic As Action(Of Diagnostic))
                If arrayCreationExpression.RankSpecifiers.Count = 1 Then

                    ' Check for explicit specification of empty or singleton array
                    Dim literalRankSpecifier = DirectCast(arrayCreationExpression.RankSpecifiers(0).ChildNodes() _
                        .FirstOrDefault(Function(n) n.IsKind(SyntaxKind.NumericLiteralExpression)),
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
