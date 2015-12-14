' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider("GetTypeExpressionSignatureHelpProvider", LanguageNames.VisualBasic)>
    Friend Partial Class GetTypeExpressionSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of GetTypeExpressionSyntax)

        Protected Overrides Function GetIntrinsicOperatorDocumentation(node As GetTypeExpressionSyntax, document As Document, cancellationToken As CancellationToken) As IEnumerable(Of AbstractIntrinsicOperatorDocumentation)
            Return {New GetTypeExpressionDocumentation()}
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsChildToken(Of GetTypeExpressionSyntax)(Function(ce) ce.OpenParenToken)
        End Function

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Protected Overrides Function IsArgumentListToken(node As GetTypeExpressionSyntax, token As SyntaxToken) As Boolean
            Return node.GetTypeKeyword <> token AndAlso
                node.CloseParenToken <> token
        End Function
    End Class
End Namespace
