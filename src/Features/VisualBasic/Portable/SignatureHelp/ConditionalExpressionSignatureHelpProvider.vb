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
    Friend MustInherit Class ConditionalExpressionSignatureHelpProvider(Of T As SyntaxNode)
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of T)

        Protected MustOverride ReadOnly Property Kind As SyntaxKind

        Protected Overrides Function GetIntrinsicOperatorDocumentationAsync(node As T, document As Document, cancellationToken As CancellationToken) As ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))
            Return ValueTaskFactory.FromResult(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))({New BinaryConditionalExpressionDocumentation(), New TernaryConditionalExpressionDocumentation()})
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
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property Kind As SyntaxKind
            Get
                Return SyntaxKind.TernaryConditionalExpression
            End Get
        End Property
    End Class
End Namespace

