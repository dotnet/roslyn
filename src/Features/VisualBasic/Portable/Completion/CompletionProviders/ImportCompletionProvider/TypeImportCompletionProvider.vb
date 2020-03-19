﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    <ExportCompletionProvider(NameOf(TypeImportCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(EmbeddedLanguageCompletionProvider))>
    <[Shared]>
    Friend NotInheritable Class TypeImportCompletionProvider
        Inherits AbstractTypeImportCompletionProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Friend Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.CommonTriggerCharsAndParen

        Protected Overrides Function CreateContextAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Return ImportCompletionProviderHelper.CreateContextAsync(document, position, cancellationToken)
        End Function

        Protected Overrides Function GetImportedNamespaces(location As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ImmutableArray(Of String)
            Return ImportCompletionProviderHelper.GetImportedNamespaces(location, semanticModel, cancellationToken)
        End Function
    End Class
End Namespace
