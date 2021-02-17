' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicIntroduceParameterService
        Inherits AbstractIntroduceParameterService(Of VisualBasicIntroduceParameterService, ExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function IntroduceParameterAsync(document As SemanticDocument, expression As ExpressionSyntax, allOccurrences As Boolean, trampoline As Boolean, cancellationToken As Threading.CancellationToken) As Task(Of Solution)
            Dim invocationDocument = document.Document
            Dim semanticModel = document.SemanticModel
            Dim semanticFacts = invocationDocument.GetLanguageService(Of ISemanticFactsService)
            Dim parameterName = semanticFacts.GenerateNameForExpression(semanticModel, expression, False, cancellationToken)

            Dim x = SyntaxGenerator.GetGenerator(invocationDocument)

        End Function

        Protected Overrides Function ExpressionWithinParameterizedMethod(expression As ExpressionSyntax) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function RewriteCore(Of TNode As SyntaxNode)(node As TNode, replacementNode As SyntaxNode, matches As ISet(Of ExpressionSyntax)) As TNode
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
