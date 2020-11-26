' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(EnumCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(ObjectCreationCompletionProvider))>
    <[Shared]>
    Partial Friend Class EnumCompletionProvider
        Inherits AbstractEnumCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return text(characterPosition) = " "c OrElse
                text(characterPosition) = "("c OrElse
                (characterPosition > 1 AndAlso text(characterPosition) = "="c AndAlso text(characterPosition - 1) = ":"c) OrElse
                SyntaxFacts.IsIdentifierStartCharacter(text(characterPosition)) AndAlso
                options.GetOption(CompletionOptions.TriggerOnTypingLetters2, LanguageNames.VisualBasic)
        End Function

        Friend Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = ImmutableHashSet.Create(" "c, "("c, "="c)

        ' PERF: Cached values for GetDisplayAndInsertionText. Cuts down on the number of calls to ToMinimalDisplayString for large enums.
        Private _cachedDisplayAndInsertionTextContainingType As INamedTypeSymbol
        Private _cachedDisplayAndInsertionTextContext As SyntaxContext
        Private _cachedDisplayAndInsertionTextContainingTypeText As String

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(symbol As ISymbol, context As SyntaxContext) As (displayText As String, suffix As String, insertionText As String)
            If symbol.ContainingType IsNot Nothing AndAlso symbol.ContainingType.TypeKind = TypeKind.Enum Then
                If Not Equals(_cachedDisplayAndInsertionTextContainingType, symbol.ContainingType) OrElse _cachedDisplayAndInsertionTextContext IsNot context Then
                    Dim displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType).WithLocalOptions(SymbolDisplayLocalOptions.None)
                    Dim displayService = context.GetLanguageService(Of ISymbolDisplayService)()
                    _cachedDisplayAndInsertionTextContainingTypeText = symbol.ContainingType.ToMinimalDisplayString(context.SemanticModel, context.Position, displayFormat)
                    _cachedDisplayAndInsertionTextContainingType = symbol.ContainingType
                    _cachedDisplayAndInsertionTextContext = context
                End If

                Dim text As String = _cachedDisplayAndInsertionTextContainingTypeText & "." & symbol.Name
                Return (text, "", text)
            End If

            Return CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context)
        End Function

        Protected Overrides Async Function CreateContextAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of SyntaxContext)
            Dim semanticModel = Await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(False)
            Return Await VisualBasicSyntaxContext.CreateContextAsync(document.Project.Solution.Workspace, semanticModel, position, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function CreateItem(completionContext As CompletionContext,
                displayText As String, displayTextSuffix As String, insertionText As String,
                symbols As List(Of ISymbol), context As SyntaxContext, preselect As Boolean, supportedPlatformData As SupportedPlatformData) As CompletionItem
            Dim rules = GetCompletionItemRules(symbols)
            rules = rules.WithMatchPriority(If(preselect, MatchPriority.Preselect, MatchPriority.Default))

            Return SymbolCompletionItem.CreateWithSymbolId(
                displayText:=displayText,
                displayTextSuffix:=displayTextSuffix,
                insertionText:=insertionText,
                filterText:=GetFilterText(symbols(0), displayText, context),
                symbols:=symbols,
                contextPosition:=context.Position,
                sortText:=insertionText,
                supportedPlatforms:=supportedPlatformData,
                rules:=rules)
        End Function

        Private Shared ReadOnly s_rules As CompletionItemRules =
            CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect)

        Protected Overrides Function GetCompletionItemRules(symbols As IReadOnlyList(Of ISymbol)) As CompletionItemRules
            Return s_rules
        End Function

        Public Overrides Function GetTextChangeAsync(document As Document, selectedItem As CompletionItem, ch As Char?, cancellationToken As CancellationToken) As Task(Of TextChange?)
            Dim insertionText As String = SymbolCompletionItem.GetInsertionText(selectedItem)
            Return Task.FromResult(Of TextChange?)(New TextChange(selectedItem.Span, insertionText))
        End Function

        Protected Overrides Function GetInsertionText(item As CompletionItem, ch As Char) As String
            Return CompletionUtilities.GetInsertionTextAtInsertionTime(item, ch)
        End Function
    End Class
End Namespace
