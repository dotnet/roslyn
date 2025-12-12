' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider("GetTypeExpressionSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class GetTypeExpressionSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of GetTypeExpressionSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetIntrinsicOperatorDocumentationAsync(node As GetTypeExpressionSyntax, document As Document, cancellationToken As CancellationToken) As ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))
            Return New ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))({New GetTypeExpressionDocumentation()})
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsChildToken(Of GetTypeExpressionSyntax)(Function(ce) ce.OpenParenToken)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableArray(Of Char) = ImmutableArray.Create("("c)

        Public Overrides ReadOnly Property RetriggerCharacters As ImmutableArray(Of Char) = ImmutableArray.Create(")"c)

        Protected Overrides Function IsArgumentListToken(node As GetTypeExpressionSyntax, token As SyntaxToken) As Boolean
            Return node.GetTypeKeyword <> token AndAlso
                node.CloseParenToken <> token
        End Function
    End Class
End Namespace
