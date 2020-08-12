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
    <ExportSignatureHelpProvider("AddRemoveHandlerSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Friend Class AddRemoveHandlerSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of AddRemoveHandlerStatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetIntrinsicOperatorDocumentationAsync(node As AddRemoveHandlerStatementSyntax, document As Document, cancellationToken As CancellationToken) As ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))
            Select Case node.Kind
                Case SyntaxKind.AddHandlerStatement
                    Return New ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))(SpecializedCollections.SingletonEnumerable(New AddHandlerStatementDocumentation()))
                Case SyntaxKind.RemoveHandlerStatement
                    Return New ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))(SpecializedCollections.SingletonEnumerable(New RemoveHandlerStatementDocumentation()))
            End Select

            Return New ValueTask(Of IEnumerable(Of AbstractIntrinsicOperatorDocumentation))(SpecializedCollections.EmptyEnumerable(Of AbstractIntrinsicOperatorDocumentation)())
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsChildToken(Of AddRemoveHandlerStatementSyntax)(Function(ce) ce.AddHandlerOrRemoveHandlerKeyword) OrElse
                   token.IsChildToken(Of AddRemoveHandlerStatementSyntax)(Function(ce) ce.CommaToken)
        End Function

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = " "c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return False
        End Function

        Protected Overrides Function IsArgumentListToken(node As AddRemoveHandlerStatementSyntax, token As SyntaxToken) As Boolean
            Return True
        End Function
    End Class
End Namespace

