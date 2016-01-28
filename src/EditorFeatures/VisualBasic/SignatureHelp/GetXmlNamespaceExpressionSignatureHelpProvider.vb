' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider("GetXmlNamespaceExpressionSignatureHelpProvider", LanguageNames.VisualBasic)>
    Friend Partial Class GetXmlNamespaceExpressionSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of GetXmlNamespaceExpressionSyntax)

        Protected Overrides Function GetIntrinsicOperatorDocumentation(node As GetXmlNamespaceExpressionSyntax, document As Document, cancellationToken As CancellationToken) As IEnumerable(Of AbstractIntrinsicOperatorDocumentation)
            Return {New GetXmlNamespaceExpressionDocumentation()}
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsChildToken(Of GetXmlNamespaceExpressionSyntax)(Function(ce) ce.OpenParenToken)
        End Function

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Protected Overrides Function IsArgumentListToken(node As GetXmlNamespaceExpressionSyntax, token As SyntaxToken) As Boolean
            Return node.GetXmlNamespaceKeyword <> token AndAlso
                node.CloseParenToken <> token
        End Function
    End Class
End Namespace
