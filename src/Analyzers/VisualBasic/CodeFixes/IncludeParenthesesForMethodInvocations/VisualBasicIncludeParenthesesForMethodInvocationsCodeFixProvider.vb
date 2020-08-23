' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IncludeParenthesesForMethodInvocations
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicIncludeParenthesesForMethodInvocationsCodeFixProvider
        Inherits SyntaxEditorBasedCodeFixProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(IDEDiagnosticIds.AddParenthesesForMethodInvocationsDiagnosticId)

        Friend Overrides ReadOnly Property CodeFixCategory As CodeFixCategory = CodeFixCategory.CodeStyle

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            For Each diagnostic In context.Diagnostics
                Dim node = root.FindNode(diagnostic.AdditionalLocations(0).SourceSpan, getInnermostNodeForTie:=True)
                While Not (node Is Nothing OrElse node.IsKind(SyntaxKind.InvocationExpression))
                    node = node.Parent
                End While

                If TypeOf node IsNot InvocationExpressionSyntax Then
                    Return
                End If
                Dim invocationExpression = DirectCast(node, InvocationExpressionSyntax)
                If invocationExpression.ArgumentList Is Nothing Then
                    ' Add the parentheses.
                    context.RegisterCodeFix(New MyCodeAction(
                        VisualBasicAnalyzersResources.Add_parentheses_to_method_invocation,
                        Function(ct) FixAsync(context.Document, diagnostic, ct)),
                        diagnostic)
                Else
                    ' Remove the parentheses.
                    context.RegisterCodeFix(New MyCodeAction(
                        VisualBasicAnalyzersResources.Remove_parentheses_from_method_invocation,
                        Function(ct) FixAsync(context.Document, diagnostic, ct)),
                        diagnostic)
                End If
            Next
        End Function

        Protected Overrides Async Function FixAllAsync(
            document As Document, diagnostics As ImmutableArray(Of Diagnostic),
            editor As SyntaxEditor, cancellationToken As CancellationToken) As Task
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            For Each diagnostic In diagnostics
                Dim node = root.FindNode(diagnostic.AdditionalLocations(0).SourceSpan, getInnermostNodeForTie:=True)
                While Not (node Is Nothing OrElse node.IsKind(SyntaxKind.InvocationExpression))
                    node = node.Parent
                End While
                If TypeOf node IsNot InvocationExpressionSyntax Then
                    Return
                End If

                Dim invocationExpression = DirectCast(node, InvocationExpressionSyntax)
                If invocationExpression.ArgumentList Is Nothing Then
                    editor.ReplaceNode(invocationExpression, invocationExpression.WithArgumentList(SyntaxFactory.ArgumentList()))
                Else
                    editor.ReplaceNode(invocationExpression, invocationExpression.WithArgumentList(Nothing))
                End If
            Next
        End Function

        Private Class MyCodeAction
            Inherits CustomCodeActions.DocumentChangeAction

            Friend Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument, title)
            End Sub
        End Class
    End Class
End Namespace
