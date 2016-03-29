' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider("AddRemoveHandlerSignatureHelpProvider", LanguageNames.VisualBasic)>
    Friend Class AddRemoveHandlerSignatureHelpProvider
        Inherits AbstractIntrinsicOperatorSignatureHelpProvider(Of AddRemoveHandlerStatementSyntax)

        Protected Overrides Function GetIntrinsicOperatorDocumentation(node As AddRemoveHandlerStatementSyntax, document As Document, cancellationToken As CancellationToken) As IEnumerable(Of AbstractIntrinsicOperatorDocumentation)
            Select Case node.Kind
                Case SyntaxKind.AddHandlerStatement
                    Return SpecializedCollections.SingletonEnumerable(New AddHandlerStatementDocumentation())
                Case SyntaxKind.RemoveHandlerStatement
                    Return SpecializedCollections.SingletonEnumerable(New RemoveHandlerStatementDocumentation())
            End Select

            Return SpecializedCollections.EmptyEnumerable(Of AbstractIntrinsicOperatorDocumentation)()
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

