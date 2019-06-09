' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    Friend MustInherit Class ConditionalExpressionSignatureHelpProvider(Of T As SyntaxNode)
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of T)

        Protected MustOverride ReadOnly Property Kind As SyntaxKind

        Protected Overrides Function GetIntrinsicOperatorDocumentationAsync(node As T, document As Document, cancellationToken As CancellationToken) As ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))
            Return New ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))({New BinaryConditionalExpressionDocumentation(), New TernaryConditionalExpressionDocumentation()})
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) AndAlso
                   token.Parent.Kind = Kind
        End Function

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Protected Overrides Function IsArgumentListToken(node As T, token As SyntaxToken) As Boolean
            Return node.Span.Contains(token.SpanStart) AndAlso
                (token.Kind <> SyntaxKind.CloseParenToken OrElse
                token.Parent.Kind <> Kind)
        End Function
    End Class

    <ExportSignatureHelpProvider("BinaryConditionalExpressionSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Friend Class BinaryConditionalExpressionSignatureHelpProvider
        Inherits ConditionalExpressionSignatureHelpProvider(Of BinaryConditionalExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property Kind As SyntaxKind
            Get
                Return SyntaxKind.BinaryConditionalExpression
            End Get
        End Property
    End Class

    <ExportSignatureHelpProvider("TernaryConditionalExpressionSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Friend Class TernaryConditionalExpressionSignatureHelpProvider
        Inherits ConditionalExpressionSignatureHelpProvider(Of TernaryConditionalExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property Kind As SyntaxKind
            Get
                Return SyntaxKind.TernaryConditionalExpression
            End Get
        End Property
    End Class
End Namespace

