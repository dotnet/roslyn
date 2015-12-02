' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor.Implementation.Interactive
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VisualStudio.InteractiveWindow.Commands

Namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders

    <ExportCompletionProvider("ReplCommandCompletionProvider", LanguageNames.VisualBasic)>
    <TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)>
    <Order(Before:=PredefinedCompletionProviderNames.Keyword)>
    Friend Class ReplCommandCompletionProvider
        Inherits CompletionListProvider

        Private Async Function GetTextChangeSpanAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of TextSpan)
            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Return CompletionUtilities.GetTextChangeSpan(text, position)
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options)
        End Function

        Public Overrides Async Function ProduceCompletionListAsync(context As CompletionListContext) As Task
            Dim document = context.Document
            Dim position = context.Position
            Dim cancellationToken = context.CancellationToken

            ' the provider might be invoked in non-interactive context:
            Dim ws As Workspace = Nothing
            If Workspace.TryGetWorkspace(document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).Container, ws) Then
                Dim workspace As InteractiveWorkspace = TryCast(ws, InteractiveWorkspace)
                If workspace IsNot Nothing Then
                    Dim window = workspace.Engine.CurrentWindow
                    Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)

                    Dim shouldComplete = tree.IsBeforeFirstToken(position, cancellationToken) AndAlso tree.IsPreProcessorKeywordContext(position, cancellationToken)
                    If shouldComplete Then
                        Dim filterSpan = Await Me.GetTextChangeSpanAsync(document, position, cancellationToken).ConfigureAwait(False)

                        Dim commands As IInteractiveWindowCommands = window.GetInteractiveCommands()
                        If commands IsNot Nothing Then
                            For Each commandItem In commands.GetCommands()
                                For Each commandName In commandItem.Names
                                    ' In VB the completion needs to include the # sign.
                                    Dim completion = "#" + commandName
                                    context.AddItem(New CompletionItem(Me, completion, filterSpan, Function(c) Task.FromResult(commandItem.Description.ToSymbolDisplayParts()), glyph:=Glyph.Intrinsic))
                                Next
                            Next
                        End If
                    End If
                End If
            End If
        End Function
    End Class

End Namespace