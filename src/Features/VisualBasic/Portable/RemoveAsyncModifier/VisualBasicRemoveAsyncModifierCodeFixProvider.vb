' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.MakeMethodAsynchronous.AbstractMakeMethodAsynchronousCodeFixProvider
Imports Microsoft.CodeAnalysis.RemoveAsyncModifier
Imports Microsoft.CodeAnalysis.VisualBasic.MakeMethodSynchronous
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveAsyncModifier
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveAsyncModifier), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.MakeMethodSynchronous)>
    Friend Class VisualBasicRemoveAsyncModifierCodeFixProvider
        Inherits AbstractRemoveAsyncModifierCodeFixProvider(Of ReturnStatementSyntax, ExpressionSyntax)

        Private Const BC42356 As String = NameOf(BC42356) ' This async method lacks 'Await' operators and so will run synchronously.

        Private Shared ReadOnly s_diagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC42356)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = s_diagnosticIds

        Protected Overrides Function IsAsyncSupportingFunctionSyntax(node As SyntaxNode) As Boolean
            Return node.IsAsyncSupportedFunctionSyntax()
        End Function

        Protected Overrides Function ShouldOfferFix(methodSymbol As IMethodSymbol, knownTypes As KnownTypes) As Boolean
            ' We only apply to functions because async subs will be offered the Make Method Synchronous fixer which would do the same job
            Return Not methodSymbol.ReturnsVoid
        End Function

        Protected Overrides Function RemoveAsyncModifier(methodSymbolOpt As IMethodSymbol, node As SyntaxNode, knownTypes As KnownTypes) As SyntaxNode
            Dim methodBlock = TryCast(node, MethodBlockSyntax)
            If methodBlock IsNot Nothing Then
                Dim subOrFunctionStatement = methodBlock.SubOrFunctionStatement
                Dim newSubOrFunctionStatement = RemoveAsyncModifierHelpers.RemoveAsyncKeyword(subOrFunctionStatement)
                Return methodBlock.WithSubOrFunctionStatement(newSubOrFunctionStatement)
            End If

            Dim multiLineLambda = TryCast(node, MultiLineLambdaExpressionSyntax)
            If multiLineLambda IsNot Nothing Then
                Return RemoveAsyncModifierHelpers.FixMultiLineLambdaExpression(multiLineLambda)
            End If

            Dim singleLineLambda = TryCast(node, SingleLineLambdaExpressionSyntax)
            If singleLineLambda IsNot Nothing Then
                Return RemoveAsyncModifierHelpers.FixSingleLineLambdaExpression(singleLineLambda)
            End If

            Return Nothing
        End Function

        Protected Overrides Function AnalyzeControlFlow(semanticModel As SemanticModel, node As SyntaxNode) As ControlFlowAnalysis
            Dim methodBlock = TryCast(node, MethodBlockSyntax)
            If methodBlock IsNot Nothing Then
                Dim statements = methodBlock.Statements
                Return semanticModel.AnalyzeControlFlow(statements.First(), statements.Last())
            End If

            Dim multiLineLambda = TryCast(node, MultiLineLambdaExpressionSyntax)
            If multiLineLambda IsNot Nothing Then
                Dim statements = multiLineLambda.Statements
                Return semanticModel.AnalyzeControlFlow(statements.First(), statements.Last())
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetLastChildOfBlock(node As SyntaxNode) As SyntaxNode
            Dim methodBlock = TryCast(node, MethodBlockSyntax)
            If methodBlock IsNot Nothing Then
                Return methodBlock.Statements.Last()
            End If

            Dim multiLineLambda = TryCast(node, MultiLineLambdaExpressionSyntax)
            If multiLineLambda IsNot Nothing Then
                Return multiLineLambda.Statements.Last()
            End If

            Return Nothing
        End Function

        Protected Overrides Function ConvertToBlockBody(node As SyntaxNode, expressionBody As SyntaxNode) As SyntaxNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function TryGetExpressionBody(methodSymbolOpt As SyntaxNode, ByRef expression As SyntaxNode) As Boolean
            Dim singleLineLambda = TryCast(methodSymbolOpt, SingleLineLambdaExpressionSyntax)
            If singleLineLambda IsNot Nothing Then
                expression = singleLineLambda.Body
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
