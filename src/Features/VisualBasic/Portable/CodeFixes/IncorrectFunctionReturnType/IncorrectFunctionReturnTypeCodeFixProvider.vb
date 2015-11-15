' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectFunctionReturnType

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FixIncorrectFunctionReturnType), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.ImplementInterface)>
    Friend Class IncorrectFunctionReturnTypeCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC36938 As String = "BC36938" ' Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        Friend Const BC36945 As String = "BC36945" ' The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC36938, BC36945)
            End Get
        End Property

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(span.Start)
            If Not token.Span.IntersectsWith(span) Then
                Return
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim lambdaHeader = GetNodeToFix(Of LambdaHeaderSyntax)(token, span)
            If lambdaHeader IsNot Nothing Then
                Dim rewrittenLambdaHeader = AsyncOrIteratorFunctionReturnTypeFixer.RewriteLambdaHeader(lambdaHeader, semanticModel, cancellationToken)
                context.RegisterFixes(
                    Await GetCodeActions(document, lambdaHeader, rewrittenLambdaHeader, semanticModel, cancellationToken).ConfigureAwait(False),
                    context.Diagnostics)
                Return
            End If

            Dim methodStatement = GetNodeToFix(Of MethodStatementSyntax)(token, span)
            If methodStatement IsNot Nothing Then
                Dim rewrittenMethodStatement = AsyncOrIteratorFunctionReturnTypeFixer.RewriteMethodStatement(methodStatement, semanticModel, cancellationToken)
                context.RegisterFixes(
                    Await GetCodeActions(document, methodStatement, rewrittenMethodStatement, semanticModel, cancellationToken).ConfigureAwait(False),
                    context.Diagnostics)
                Return
            End If
        End Function

        Private Function GetNodeToFix(Of T As SyntaxNode)(token As SyntaxToken, span As TextSpan) As T
            Return token.GetAncestors(Of T)() _
                .FirstOrDefault(Function(c) c.Span.IntersectsWith(span))
        End Function

        Private Async Function GetCodeActions(document As Document, node As SyntaxNode, rewrittenNode As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CodeAction))
            If rewrittenNode IsNot node Then
                Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim newRoot = root.ReplaceNode(node, rewrittenNode)
                Dim newDocument = document.WithSyntaxRoot(newRoot)
                Return {New MyCodeAction(VBFeaturesResources.FixIncorrectFunctionReturnType, newDocument)}
            End If

            Return SpecializedCollections.EmptyEnumerable(Of CodeAction)()
        End Function

        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction

            Public Sub New(title As String, newDocument As Document)
                MyBase.New(title, Function(c) Task.FromResult(newDocument))
            End Sub
        End Class
    End Class
End Namespace
