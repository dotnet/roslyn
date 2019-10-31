' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend NotInheritable Class ExtensionMethodImportCompletionProvider
        Inherits AbstractExtensionMethodImportCompletionProvider

        Protected Overrides ReadOnly Property GenericSuffix As String
            Get
                Return "(Of ...)"
            End Get
        End Property

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacterOrParen(text, characterPosition, options)
        End Function

        Protected Overrides Function CreateContextAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Return ImportCompletionProviderHelper.CreateContextAsync(document, position, cancellationToken)
        End Function

        Protected Overrides Function GetImportedNamespaces(location As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ImmutableArray(Of String)
            Return ImportCompletionProviderHelper.GetImportedNamespaces(location, semanticModel, cancellationToken)
        End Function
    End Class
End Namespace
