' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectFunctionReturnType
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FixIncorrectFunctionReturnType), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.ImplementInterface)>
    Friend Class IncorrectFunctionReturnTypeCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC36938 As String = "BC36938" ' Iterator functions must return either IEnumerable(Of T), or IEnumerator(Of T), or the non-generic forms IEnumerable or IEnumerator.
        Friend Const BC36945 As String = "BC36945" ' The 'Async' modifier can only be used on Subs, or on Functions that return Task or Task(Of T).

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC36938, BC36945)
            End Get
        End Property

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            ' Fix All is not supported for this code fix
            Return Nothing
        End Function

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
                    Await GetCodeActionsAsync(document, lambdaHeader, rewrittenLambdaHeader, cancellationToken).ConfigureAwait(False),
                    context.Diagnostics)
                Return
            End If

            Dim methodStatement = GetNodeToFix(Of MethodStatementSyntax)(token, span)
            If methodStatement IsNot Nothing Then
                Dim rewrittenMethodStatement = AsyncOrIteratorFunctionReturnTypeFixer.RewriteMethodStatement(methodStatement, semanticModel, cancellationToken)
                context.RegisterFixes(
                    Await GetCodeActionsAsync(document, methodStatement, rewrittenMethodStatement, cancellationToken).ConfigureAwait(False),
                    context.Diagnostics)
                Return
            End If
        End Function

        Private Shared Function GetNodeToFix(Of T As SyntaxNode)(token As SyntaxToken, span As TextSpan) As T
            Return token.GetAncestors(Of T)() _
                .FirstOrDefault(Function(c) c.Span.IntersectsWith(span))
        End Function

        Private Shared Async Function GetCodeActionsAsync(document As Document, node As SyntaxNode, rewrittenNode As SyntaxNode, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CodeAction))
            If rewrittenNode IsNot node Then
                Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
                Dim newRoot = root.ReplaceNode(node, rewrittenNode)
                Dim newDocument = document.WithSyntaxRoot(newRoot)
                Return {CodeAction.Create(
                    VBFeaturesResources.Fix_Incorrect_Function_Return_Type,
                    Function(c) Task.FromResult(newDocument),
                    NameOf(VBFeaturesResources.Fix_Incorrect_Function_Return_Type))}
            End If

            Return SpecializedCollections.EmptyEnumerable(Of CodeAction)()
        End Function
    End Class
End Namespace
