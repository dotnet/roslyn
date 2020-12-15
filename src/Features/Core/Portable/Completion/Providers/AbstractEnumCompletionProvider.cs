// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractEnumCompletionProvider : AbstractSymbolCompletionProvider
    {
        private static readonly CompletionItemRules s_rules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect);

        protected abstract (string displayText, string suffix, string insertionText) GetDefaultDisplayAndSuffixAndInsertionText(ISymbol symbol, SyntaxContext context);

        protected override async Task<ImmutableArray<ISymbol>> GetPreselectedSymbolsAsync(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
            => await GetSymbolsAsync(context, position, options, cancellationToken).ConfigureAwait(false);

        protected override Task<ImmutableArray<ISymbol>> GetSymbolsAsync(
            SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var syntaxFacts = context.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsInNonUserCode(context.SyntaxTree, context.Position, cancellationToken))
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();

            // This providers provides fully qualified names, eg "DayOfWeek.Monday"
            // Don't run after dot because SymbolCompletionProvider will provide
            // members in situations like Dim x = DayOfWeek.$$
            if (context.TargetToken.RawKind == syntaxFacts.SyntaxKinds.DotToken)
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();

            var typeInferenceService = context.GetLanguageService<ITypeInferenceService>();
            var inferedTypes = typeInferenceService.InferTypes(context.SemanticModel, position, cancellationToken);

            var hideAdvancedMembers = options.GetOption(RecommendationOptions.HideAdvancedMembers, context.SemanticModel.Language);

            // We'll want to build a list of the actual enum members and all accessible instances of that enum, too
            var result = inferedTypes
                .Select(t => t.RemoveNullableIfPresent())
                .Where(s => s.TypeKind == TypeKind.Enum)
                .SelectMany(e => e.GetMembers().OfType<IFieldSymbol>())
                .Where(f =>
                    f.IsConst &&
                    f.IsEditorBrowsable(hideAdvancedMembers, context.SemanticModel.Compilation))
                .ToImmutableArray<ISymbol>();

            return Task.FromResult(result);
        }

        // PERF: Cached values for GetDisplayAndInsertionText. Cuts down on the number of calls to ToMinimalDisplayString for large enums.
        private readonly object _cachedDisplayAndInsertionTextLock = new object();
        private (INamedTypeSymbol? containingType, SyntaxContext? context, string? containingTypeText) _cachedDisplayAndInsertionText;

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, SyntaxContext context)
        {
            if (symbol.ContainingType != null && symbol.ContainingType.TypeKind == TypeKind.Enum)
            {
                lock (_cachedDisplayAndInsertionTextLock)
                {
                    if (!Equals(_cachedDisplayAndInsertionText.containingType, symbol.ContainingType) || _cachedDisplayAndInsertionText.context != context)
                    {
                        var displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
                            .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType)
                            .WithLocalOptions(SymbolDisplayLocalOptions.None);
                        var containingTypeText = symbol.ContainingType.ToMinimalDisplayString(context.SemanticModel, context.Position, displayFormat);
                        _cachedDisplayAndInsertionText = (symbol.ContainingType, context, containingTypeText);
                    }

                    var text = $"{_cachedDisplayAndInsertionText.containingTypeText}.{symbol.Name}";
                    return (text, "", text);
                }
            }

            return GetDefaultDisplayAndSuffixAndInsertionText(symbol, context);
        }

        protected override CompletionItem CreateItem(CompletionContext completionContext, string displayText,
            string displayTextSuffix, string insertionText, List<ISymbol> symbols, SyntaxContext context, bool preselect,
            SupportedPlatformData supportedPlatformData)
        {
            var rules = GetCompletionItemRules(symbols);

            rules = rules.WithMatchPriority(preselect ? MatchPriority.Preselect : MatchPriority.Default);

            var sortText = symbols[0] is IFieldSymbol field && field.IsConst
                // e.g. System.IO.FileAccess.ReadWrite (underlying type is Int32 and value = 3), becomes "System.IO.FileAccess#2147483651"
                ? $"{insertionText.Substring(0, insertionText.LastIndexOf("."))}#{SortableUnderlyingEnumValue(field.ConstantValue)}"
                : insertionText;
            return SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayText,
                displayTextSuffix: displayTextSuffix,
                insertionText: insertionText,
                filterText: GetFilterText(symbols[0], displayText, context),
                symbols: symbols,
                contextPosition: context.Position,
                sortText: sortText,
                supportedPlatforms: supportedPlatformData,
                rules: rules,
                inlineDescription: $"= {((IFieldSymbol)symbols[0]).ConstantValue}");
        }

        private static string SortableUnderlyingEnumValue(object? constantValue)
        {
            // Convert the enum value to a string with leading zeros, so text sorting is the same as numerical sorting
            // Signed values are "shifted" into the positiv range by adding the "MinValue" e.g. sbyte:
            // -128 becomes "000"
            // 0 becomes "128"
            // 127 becomes "255"
            return constantValue switch
            {
                byte b => b.ToString("000", CultureInfo.InvariantCulture),
                sbyte s => (s - sbyte.MinValue).ToString("000", CultureInfo.InvariantCulture),
                short s => (s - short.MinValue).ToString("00000", CultureInfo.InvariantCulture),
                ushort u => u.ToString("00000", CultureInfo.InvariantCulture),
                int i => (i - (long)int.MinValue).ToString("0000000000", CultureInfo.InvariantCulture),
                uint u => u.ToString("0000000000", CultureInfo.InvariantCulture),
                long l => ((ulong)(l - long.MinValue)).ToString("00000000000000000000", CultureInfo.InvariantCulture),
                ulong u => u.ToString("00000000000000000000", CultureInfo.InvariantCulture),
                _ => throw ExceptionUtilities.UnexpectedValue(constantValue),
            };
        }

        protected override CompletionItemRules GetCompletionItemRules(IReadOnlyList<ISymbol> symbols) => s_rules;

        public override Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            var insertionText = SymbolCompletionItem.GetInsertionText(selectedItem);
            return Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, insertionText));
        }
    }
}
