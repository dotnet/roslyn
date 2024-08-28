' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.MakeMethodSynchronous
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveAsyncModifier
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeMethodSynchronous
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.MakeMethodSynchronous), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddImport)>
    Friend Class VisualBasicMakeMethodSynchronousCodeFixProvider
        Inherits AbstractMakeMethodSynchronousCodeFixProvider

        Private Const BC42356 As String = NameOf(BC42356) ' This async method lacks 'Await' operators and so will run synchronously.

        Private Shared ReadOnly s_diagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC42356)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return s_diagnosticIds
            End Get
        End Property

        Protected Overrides Function IsAsyncSupportingFunctionSyntax(node As SyntaxNode) As Boolean
            Return node.IsAsyncSupportedFunctionSyntax()
        End Function

        Protected Overrides Function RemoveAsyncTokenAndFixReturnType(methodSymbolOpt As IMethodSymbol, node As SyntaxNode, knownTypes As KnownTaskTypes) As SyntaxNode
            If node.IsKind(SyntaxKind.SingleLineSubLambdaExpression) OrElse
                node.IsKind(SyntaxKind.SingleLineFunctionLambdaExpression) Then

                Return RemoveAsyncModifierHelpers.FixSingleLineLambdaExpression(DirectCast(node, SingleLineLambdaExpressionSyntax))
            ElseIf node.IsKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
                    node.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) Then

                Return RemoveAsyncModifierHelpers.FixMultiLineLambdaExpression(DirectCast(node, MultiLineLambdaExpressionSyntax))
            ElseIf node.IsKind(SyntaxKind.SubBlock) Then
                Return FixSubBlock(DirectCast(node, MethodBlockSyntax))
            Else
                Return FixFunctionBlock(methodSymbolOpt, DirectCast(node, MethodBlockSyntax), knownTypes)
            End If
        End Function

        Private Shared Function FixFunctionBlock(methodSymbol As IMethodSymbol, node As MethodBlockSyntax, knownTypes As KnownTaskTypes) As SyntaxNode
            Dim functionStatement = node.SubOrFunctionStatement

            ' if this returns Task(of T), then we want to convert this to a T returning function.
            ' if this returns Task, then we want to convert it to a Sub method.
            If methodSymbol.ReturnType.OriginalDefinition.Equals(knownTypes.TaskOfTType) Then
                Dim newAsClause = functionStatement.AsClause.WithType(methodSymbol.ReturnType.GetTypeArguments()(0).GenerateTypeSyntax())
                Dim newFunctionStatement = functionStatement.WithAsClause(newAsClause)
                newFunctionStatement = RemoveAsyncModifierHelpers.RemoveAsyncKeyword(newFunctionStatement)
                Return node.WithSubOrFunctionStatement(newFunctionStatement)
            ElseIf Equals(methodSymbol.ReturnType.OriginalDefinition, knownTypes.TaskType) Then
                ' Convert this to a 'Sub' method.
                Dim subStatement = SyntaxFactory.SubStatement(
                    functionStatement.AttributeLists,
                    functionStatement.Modifiers,
                    SyntaxFactory.Token(SyntaxKind.SubKeyword).WithTriviaFrom(functionStatement.SubOrFunctionKeyword),
                    functionStatement.Identifier,
                    functionStatement.TypeParameterList,
                    functionStatement.ParameterList,
                    functionStatement.AsClause,
                    functionStatement.HandlesClause,
                    functionStatement.ImplementsClause)

                subStatement = subStatement.RemoveNode(subStatement.AsClause, SyntaxRemoveOptions.KeepTrailingTrivia)
                subStatement = RemoveAsyncModifierHelpers.RemoveAsyncKeyword(subStatement)

                Dim endSubStatement = SyntaxFactory.EndSubStatement(
                    node.EndSubOrFunctionStatement.EndKeyword,
                    SyntaxFactory.Token(SyntaxKind.SubKeyword).WithTriviaFrom(node.EndSubOrFunctionStatement.BlockKeyword))

                Return SyntaxFactory.SubBlock(subStatement, node.Statements, endSubStatement)
            Else
                Dim newFunctionStatement = RemoveAsyncModifierHelpers.RemoveAsyncKeyword(functionStatement)
                Return node.WithSubOrFunctionStatement(newFunctionStatement)
            End If
        End Function

        Private Shared Function FixSubBlock(node As MethodBlockSyntax) As SyntaxNode
            Dim newSubStatement = RemoveAsyncModifierHelpers.RemoveAsyncKeyword(node.SubOrFunctionStatement)
            Return node.WithSubOrFunctionStatement(newSubStatement)
        End Function
    End Class
End Namespace
