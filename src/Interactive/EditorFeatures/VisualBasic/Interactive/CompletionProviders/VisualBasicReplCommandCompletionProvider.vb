' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor.Completion.CompletionProviders
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders

    <ExportCompletionProvider("ReplCommandCompletionProvider", LanguageNames.VisualBasic)>
    <TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)>
    <Order(Before:=PredefinedCompletionProviderNames.Keyword)>
    Friend Class VisualBasicReplCommandCompletionProvider
        Inherits ReplCompletionProvider

        Protected Overrides Async Function GetTextChangeSpanAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of TextSpan)
            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Return CompletionUtilities.GetTextChangeSpan(text, position)
        End Function

        Protected Overrides Function GetCompletionString(commandName As String) As String
            Return "#" & commandName
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options)
        End Function

        Protected Overrides Async Function ShouldDisplayCommandCompletionsAsync(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Task(Of Boolean)
            Return Await tree.IsBeforeFirstTokenAsync(position, cancellationToken).ConfigureAwait(False) AndAlso
                tree.IsPreProcessorKeywordContext(position, cancellationToken)
        End Function
    End Class

End Namespace