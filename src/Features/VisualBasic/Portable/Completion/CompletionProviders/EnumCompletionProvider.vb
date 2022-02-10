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
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(EnumCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(ObjectCreationCompletionProvider))>
    <[Shared]>
    Partial Friend Class EnumCompletionProvider
        Inherits AbstractSymbolCompletionProvider(Of VisualBasicSyntaxContext)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Private Shared Function GetPreselectedSymbolsAsync(
                context As VisualBasicSyntaxContext, position As Integer, options As CompletionOptions, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ISymbol))

            If context.SyntaxTree.IsInNonUserCode(context.Position, cancellationToken) Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            ' This providers provides fully qualified names, eg "DayOfWeek.Monday"
            ' Don't run after dot because SymbolCompletionProvider will provide
            ' members in situations like Dim x = DayOfWeek.$$
            If context.TargetToken.IsKind(SyntaxKind.DotToken) Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            Dim typeInferenceService = context.GetLanguageService(Of ITypeInferenceService)()
            Dim enumType = typeInferenceService.InferType(context.SemanticModel, position, objectAsDefault:=True, cancellationToken:=cancellationToken)

            If enumType.TypeKind <> TypeKind.Enum Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            ' We'll want to build a list of the actual enum members and all accessible instances of that enum, too
            Dim result = enumType.GetMembers().Where(
                Function(m As ISymbol) As Boolean
                    Return m.Kind = SymbolKind.Field AndAlso
                        DirectCast(m, IFieldSymbol).IsConst AndAlso
                        m.IsEditorBrowsable(options.HideAdvancedMembers, context.SemanticModel.Compilation)
                End Function).ToImmutableArray()

            Return Task.FromResult(result)
        End Function

        Private Shared Function GetNormalSymbolsAsync(
                context As VisualBasicSyntaxContext, position As Integer, options As CompletionOptions, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ISymbol))

            If context.SyntaxTree.IsInNonUserCode(context.Position, cancellationToken) OrElse
                context.SyntaxTree.IsInSkippedText(position, cancellationToken) Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            If context.TargetToken.IsKind(SyntaxKind.DotToken) Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            Dim typeInferenceService = context.GetLanguageService(Of ITypeInferenceService)()
            Dim span = New TextSpan(position, 0)
            Dim enumType = typeInferenceService.InferType(context.SemanticModel, position, objectAsDefault:=True, cancellationToken:=cancellationToken)

            If enumType.TypeKind <> TypeKind.Enum Then
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)()
            End If

            Dim otherSymbols = context.SemanticModel.LookupSymbols(position).WhereAsArray(
                Function(s) s.MatchesKind(SymbolKind.Field, SymbolKind.Local, SymbolKind.Parameter, SymbolKind.Property) AndAlso
                            s.IsEditorBrowsable(options.HideAdvancedMembers, context.SemanticModel.Compilation))

            Dim otherInstances = otherSymbols.WhereAsArray(Function(s) Equals(enumType, GetTypeFromSymbol(s)))

            Return Task.FromResult(otherInstances.Concat(enumType))
        End Function

        Protected Overrides Async Function GetSymbolsAsync(
                completionContext As CompletionContext,
                syntaxContext As VisualBasicSyntaxContext,
                position As Integer,
                options As CompletionOptions,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of (symbol As ISymbol, preselect As Boolean)))
            Dim normalSymbols = Await GetNormalSymbolsAsync(syntaxContext, position, options, cancellationToken).ConfigureAwait(False)
            Dim preselectSymbols = Await GetPreselectedSymbolsAsync(syntaxContext, position, options, cancellationToken).ConfigureAwait(False)

            Return normalSymbols.SelectAsArray(Function(s) (s, preselect:=False)).Concat(preselectSymbols.SelectAsArray(Function(s) (s, preselect:=True)))
        End Function

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return text(characterPosition) = " "c OrElse
                text(characterPosition) = "("c OrElse
                (characterPosition > 1 AndAlso text(characterPosition) = "="c AndAlso text(characterPosition - 1) = ":"c) OrElse
                SyntaxFacts.IsIdentifierStartCharacter(text(characterPosition)) AndAlso
                options.TriggerOnTypingLetters
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = ImmutableHashSet.Create(" "c, "("c, "="c)

        Private Shared Function GetTypeFromSymbol(symbol As ISymbol) As ITypeSymbol
            Dim symbolType = If(TryCast(symbol, IFieldSymbol)?.Type,
                             If(TryCast(symbol, ILocalSymbol)?.Type,
                             If(TryCast(symbol, IParameterSymbol)?.Type,
                                TryCast(symbol, IPropertySymbol)?.Type)))
            Return symbolType
        End Function

        ' PERF: Cached values for GetDisplayAndInsertionText. Cuts down on the number of calls to ToMinimalDisplayString for large enums.
        Private _cachedDisplayAndInsertionTextContainingType As INamedTypeSymbol
        Private _cachedDisplayAndInsertionTextContext As VisualBasicSyntaxContext
        Private _cachedDisplayAndInsertionTextContainingTypeText As String

        Protected Overrides Function GetDisplayAndSuffixAndInsertionText(symbol As ISymbol, context As VisualBasicSyntaxContext) As (displayText As String, suffix As String, insertionText As String)
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

        Protected Overrides Function CreateItem(
                completionContext As CompletionContext,
                displayText As String,
                displayTextSuffix As String,
                insertionText As String,
                symbols As ImmutableArray(Of (symbol As ISymbol, preselect As Boolean)),
                context As VisualBasicSyntaxContext,
                supportedPlatformData As SupportedPlatformData) As CompletionItem
            Dim rules = GetCompletionItemRules(symbols)
            Dim preselect = symbols.Any(Function(t) t.preselect)
            rules = rules.WithMatchPriority(If(preselect, MatchPriority.Preselect, MatchPriority.Default))

            Return SymbolCompletionItem.CreateWithSymbolId(
                displayText:=displayText,
                displayTextSuffix:=displayTextSuffix,
                insertionText:=insertionText,
                filterText:=GetFilterText(symbols(0).symbol, displayText, context),
                symbols:=symbols.SelectAsArray(Function(t) t.symbol),
                contextPosition:=context.Position,
                sortText:=insertionText,
                supportedPlatforms:=supportedPlatformData,
                rules:=rules)
        End Function

        Private Shared ReadOnly s_rules As CompletionItemRules =
            CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect)

        Protected Overrides Function GetCompletionItemRules(symbols As ImmutableArray(Of (symbol As ISymbol, preselect As Boolean))) As CompletionItemRules
            Return s_rules
        End Function

        Protected Overrides Function GetInsertionText(item As CompletionItem, ch As Char) As String
            Return CompletionUtilities.GetInsertionTextAtInsertionTime(item, ch)
        End Function
    End Class
End Namespace
