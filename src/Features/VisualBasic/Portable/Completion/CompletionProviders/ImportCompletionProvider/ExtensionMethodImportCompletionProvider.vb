' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
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

        Protected Overrides ReadOnly Property GenericSuffix As String
            Get
                Return "(Of ...)"
            End Get
        End Property

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Friend Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.CommonTriggerCharsAndParen

        Protected Overrides Function CreateContextAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Return ImportCompletionProviderHelper.CreateContextAsync(document, position, cancellationToken)
        End Function

        Protected Overrides Function GetImportedNamespaces(location As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ImmutableArray(Of String)
            Return ImportCompletionProviderHelper.GetImportedNamespaces(location, semanticModel)
        End Function

        Protected Overrides Function IsFinalSemicolonOfUsingOrExtern(directive As SyntaxNode, token As SyntaxToken) As Boolean
            Return False
        End Function
    End Class
End Namespace
