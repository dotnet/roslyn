' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicIntroduceParameterService
        Inherits AbstractIntroduceParameterService(Of VisualBasicIntroduceParameterService, ExpressionSyntax, InvocationExpressionSyntax, IdentifierNameSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Async Function IntroduceParameterAsync(document As SemanticDocument, expression As ExpressionSyntax, allOccurrences As Boolean, trampoline As Boolean, cancellationToken As Threading.CancellationToken) As Task(Of Solution)
            If Not trampoline Then
                Return Await IntroduceParameterForRefactoringAsync(document, expression, allOccurrences, cancellationToken).ConfigureAwait(False)
            Else
                Return Await IntroduceParameterForTrampolineAsync(document, expression, allOccurrences, cancellationToken).ConfigureAwait(False)
            End If

        End Function

        Protected Overrides Function AddArgumentToArgumentList(invocationArguments As SeparatedSyntaxList(Of SyntaxNode), newArgumentExpression As SyntaxNode) As SeparatedSyntaxList(Of SyntaxNode)
            Return invocationArguments.Add(SyntaxFactory.SimpleArgument(DirectCast(newArgumentExpression.WithoutAnnotations(ExpressionAnnotationKind).WithAdditionalAnnotations(Simplifier.Annotation), ExpressionSyntax)))
        End Function

        Protected Overrides Function RewriteCore(Of TNode As SyntaxNode)(node As TNode, replacementNode As SyntaxNode, matches As ISet(Of ExpressionSyntax)) As TNode
            Return DirectCast(Rewriter.Visit(node, replacementNode, matches), TNode)
        End Function
    End Class
End Namespace
