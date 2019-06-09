' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider(NameOf(NameOfExpressionSignatureHelpProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class NameOfExpressionSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of NameOfExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c
        End Function

        Protected Overrides Function GetIntrinsicOperatorDocumentationAsync(node As NameOfExpressionSyntax, document As Document, cancellationToken As CancellationToken) As ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))
            Return New ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))({New NameOfExpressionDocumentation()})
        End Function

        Protected Overrides Function IsArgumentListToken(node As NameOfExpressionSyntax, token As SyntaxToken) As Boolean
            Return _
                node.NameOfKeyword <> token AndAlso
                node.CloseParenToken <> token
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsChildToken(Of NameOfExpressionSyntax)(Function(noe) noe.OpenParenToken)
        End Function
    End Class
End Namespace
