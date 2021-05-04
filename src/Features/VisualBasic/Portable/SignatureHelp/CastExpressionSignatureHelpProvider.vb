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
    <ExportSignatureHelpProvider("CastExpressionSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class CastExpressionSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of CastExpressionSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetIntrinsicOperatorDocumentationAsync(node As CastExpressionSyntax, document As Document, cancellationToken As CancellationToken) As ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))
            Select Case node.Kind
                Case SyntaxKind.CTypeExpression
                    Return ValueTaskFactory.FromResult(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))({New CTypeCastExpressionDocumentation()})
                Case SyntaxKind.DirectCastExpression
                    Return ValueTaskFactory.FromResult(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))({New DirectCastExpressionDocumentation()})
                Case SyntaxKind.TryCastExpression
                    Return ValueTaskFactory.FromResult(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))({New TryCastExpressionDocumentation()})
            End Select

            Return ValueTaskFactory.FromResult(SpecializedCollections.EmptyEnumerable(Of AbstractIntrinsicOperatorDocumentation)())
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
