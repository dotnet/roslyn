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
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    <ExportCompletionProvider(NameOf(TypeImportCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(AggregateEmbeddedLanguageCompletionProvider))>
    <[Shared]>
    Friend NotInheritable Class TypeImportCompletionProvider
        Inherits AbstractTypeImportCompletionProvider(Of SimpleImportsClauseSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.CommonTriggerCharsAndParen

        Protected Overrides Function CreateContextAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Return ImportCompletionProviderHelper.CreateContextAsync(document, position, cancellationToken)
        End Function

        Protected Overrides Function GetImportedNamespaces(location As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ImmutableArray(Of String)
            Return ImportCompletionProviderHelper.GetImportedNamespaces(location, semanticModel)
        End Function

        Protected Overrides Function IsFinalSemicolonOfUsingOrExtern(directive As SyntaxNode, token As SyntaxToken) As Boolean
            Return False
        End Function

        Protected Overrides Function ShouldProvideParenthesisCompletionAsync(document As Document, item As CompletionItem, commitKey As Char?, cancellationToken As CancellationToken) As Task(Of Boolean)
            Return Task.FromResult(False)
        End Function

        Protected Overrides Function GetAliasDeclarationNodes(node As SyntaxNode) As ImmutableArray(Of SimpleImportsClauseSyntax)
            ' VB imports can only be placed before any declarations
            Return node.GetAncestorOrThis(Of CompilationUnitSyntax).Imports.SelectMany(Function(import) import.ImportsClauses).OfType(Of SimpleImportsClauseSyntax).ToImmutableArray()
        End Function
    End Class
End Namespace
