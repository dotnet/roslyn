' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
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
                    Return RoslynValueTaskExtensions.FromResult(SpecializedCollections.SingletonEnumerable(Of AbstractIntrinsicOperatorDocumentation)(New AddHandlerStatementDocumentation()))
                Case SyntaxKind.RemoveHandlerStatement
                    Return RoslynValueTaskExtensions.FromResult(SpecializedCollections.SingletonEnumerable(Of AbstractIntrinsicOperatorDocumentation)(New RemoveHandlerStatementDocumentation()))
            End Select

            Return RoslynValueTaskExtensions.FromResult(SpecializedCollections.EmptyEnumerable(Of AbstractIntrinsicOperatorDocumentation)())
        End Function

        Protected Overrides Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return token.IsChildToken(Of AddRemoveHandlerStatementSyntax)(Function(ce) ce.AddHandlerOrRemoveHandlerKeyword) OrElse
                   token.IsChildToken(Of AddRemoveHandlerStatementSyntax)(Function(ce) ce.CommaToken)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableArray(Of Char) = ImmutableArray.Create(" "c, ","c)

        Public Overrides ReadOnly Property RetriggerCharacters As ImmutableArray(Of Char) = ImmutableArray(Of Char).Empty

        Protected Overrides Function IsArgumentListToken(node As AddRemoveHandlerStatementSyntax, token As SyntaxToken) As Boolean
            Return True
        End Function
    End Class
End Namespace

