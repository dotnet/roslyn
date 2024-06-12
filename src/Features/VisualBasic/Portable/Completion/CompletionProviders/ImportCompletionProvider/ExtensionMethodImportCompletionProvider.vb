' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(ExtensionMethodImportCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(TypeImportCompletionProvider))>
    <ExtensionOrder(Before:=NameOf(LastBuiltInCompletionProvider))>
    <[Shared]>
    Friend NotInheritable Class ExtensionMethodImportCompletionProvider
        Inherits AbstractExtensionMethodImportCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides ReadOnly Property GenericSuffix As String
            Get
                Return "(Of ...)"
            End Get
        End Property

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CommonTriggerCharsAndParen

        Protected Overrides Function IsFinalSemicolonOfUsingOrExtern(directive As SyntaxNode, token As SyntaxToken) As Boolean
            Return False
        End Function

        Protected Overrides Function ShouldProvideParenthesisCompletionAsync(document As Document, item As CompletionItem, commitKey As Char?, cancellationToken As CancellationToken) As Task(Of Boolean)
            Return Task.FromResult(False)
        End Function
    End Class
End Namespace
