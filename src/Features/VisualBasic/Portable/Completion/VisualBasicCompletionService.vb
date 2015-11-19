' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.SuggestionMode

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion
    <ExportLanguageService(GetType(ICompletionService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicCompletionService
        Inherits AbstractCompletionService

        Private ReadOnly _completionProviders As IEnumerable(Of CompletionListProvider) = New CompletionListProvider() {
            New KeywordCompletionProvider(),
            New SymbolCompletionProvider(),
            New ObjectInitializerCompletionProvider(),
            New ObjectCreationCompletionProvider(),
            New EnumCompletionProvider(),
            New NamedParameterCompletionProvider(),
            New VisualBasicSuggestionModeCompletionProvider(),
            New ImplementsClauseCompletionProvider(),
            New HandlesClauseCompletionProvider(),
            New PartialTypeCompletionProvider(),
            New CrefCompletionProvider(),
            New CompletionListTagCompletionProvider
        }.ToImmutableArray()

        Public Overrides Function GetDefaultCompletionProviders() As IEnumerable(Of CompletionListProvider)
            Return _completionProviders
        End Function

        Public Overrides Function GetCompletionRules() As CompletionRules
            Return New VisualBasicCompletionRules(Me)
        End Function

        ''' <summary>
        ''' In Turkish Locale, both capital 'i's should be considered same. This behavior matches the compiler behavior.
        ''' If lowered, both 'i's are lowered to small 'i' with dot
        ''' </summary>
        Public Overrides Function GetCultureSpecificQuirks(candidate As String) As String
            If CultureInfo.CurrentCulture.Name = "tr-TR" Then
                Return candidate.Replace("I"c, "İ"c)
            End If

            Return candidate
        End Function

        Public Overrides Async Function GetDefaultTrackingSpanAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of TextSpan)
            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Return CompletionUtilities.GetTextChangeSpan(text, position)
        End Function

        Protected Overrides Function TriggerOnBackspace(text As SourceText, position As Integer, triggerInfo As CompletionTriggerInfo, options As OptionSet) As Boolean
            Dim triggerChar = triggerInfo.TriggerCharacter.GetValueOrDefault()
            Return options.GetOption(CompletionOptions.TriggerOnTyping, GetLanguageName()) AndAlso (Char.IsLetterOrDigit(triggerChar) OrElse triggerChar = "."c)
        End Function

        Public Overrides ReadOnly Property DismissIfEmpty As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property SupportSnippetCompletionListOnTab As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property DismissIfLastFilterCharacterDeleted As Boolean
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
        End Function
    End Class
End Namespace
