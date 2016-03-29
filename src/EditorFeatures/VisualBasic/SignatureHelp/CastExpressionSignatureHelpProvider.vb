' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider("CastExpressionSignatureHelpProvider", LanguageNames.VisualBasic)>
    Friend Partial Class CastExpressionSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of CastExpressionSyntax)

        Protected Overrides Function GetIntrinsicOperatorDocumentation(node As CastExpressionSyntax, document As Document, cancellationToken As CancellationToken) As IEnumerable(Of AbstractIntrinsicOperatorDocumentation)
            Select Case node.Kind
                Case SyntaxKind.CTypeExpression
                    Return {New CTypeCastExpressionDocumentation()}
                Case SyntaxKind.DirectCastExpression
                    Return {New DirectCastExpressionDocumentation()}
                Case SyntaxKind.TryCastExpression
                    Return {New TryCastExpressionDocumentation()}
            End Select

            Return SpecializedCollections.EmptyEnumerable(Of AbstractIntrinsicOperatorDocumentation)()
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsChildToken(Of CastExpressionSyntax)(Function(ce) ce.OpenParenToken) OrElse
                   token.IsChildToken(Of CastExpressionSyntax)(Function(ce) ce.CommaToken)
        End Function

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Protected Overrides Function IsArgumentListToken(node As CastExpressionSyntax, token As SyntaxToken) As Boolean
            Return node.Span.Contains(token.SpanStart) AndAlso
                node.OpenParenToken.SpanStart <= token.SpanStart AndAlso
                token <> node.CloseParenToken
        End Function
    End Class
End Namespace
