' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    Friend Class MockCompletionProvider
        Inherits CommonCompletionProvider

        Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
            Dim item = CommonCompletionItem.Create("DisplayText", "", rules:=CompletionItemRules.Default)
            context.AddItem(item)

            Return Task.CompletedTask
        End Function

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return True
        End Function

        Public Overrides Function GetTextChangeAsync(document As Document, selectedItem As CompletionItem, ch As Char?, cancellationToken As CancellationToken) As Task(Of TextChange?)
            Return Task.FromResult(Of TextChange?)(New TextChange(selectedItem.Span, "InsertionText"))
        End Function
    End Class
End Namespace
