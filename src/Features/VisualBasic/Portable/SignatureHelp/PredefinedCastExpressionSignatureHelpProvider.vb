' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider("PredefinedCastExpressionSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class PredefinedCastExpressionSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of PredefinedCastExpressionSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetIntrinsicOperatorDocumentationAsync(node As PredefinedCastExpressionSyntax, document As Document, cancellationToken As CancellationToken) As ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))
            Return New ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))(GetIntrinsicOperatorDocumentationImplAsync(node, document, cancellationToken))
        End Function

        Private Shared Async Function GetIntrinsicOperatorDocumentationImplAsync(node As PredefinedCastExpressionSyntax, document As Document, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))
            Return SpecializedCollections.SingletonEnumerable(New PredefinedCastExpressionDocumentation(node.Keyword.Kind, Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)))
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsChildToken(Of PredefinedCastExpressionSyntax)(Function(ce) ce.OpenParenToken)
        End Function

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Protected Overrides Function IsArgumentListToken(node As PredefinedCastExpressionSyntax, token As SyntaxToken) As Boolean
            Return node.Keyword <> token AndAlso
                node.CloseParenToken <> token
        End Function
    End Class
End Namespace
