// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Linq;
using Microsoft.CodeAnalysis.Recommendations;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractEnumCompletionProvider : AbstractSymbolCompletionProvider
    {
        protected abstract (string displayText, string suffix, string insertionText) GetDefaultDisplayAndSuffixAndInsertionText(ISymbol symbol, SyntaxContext context);

        protected override Task<ImmutableArray<ISymbol>> GetPreselectedSymbolsAsync(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
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
            var enumType = typeInferenceService.InferType(context.SemanticModel, position, objectAsDefault: true, cancellationToken: cancellationToken);

            if (enumType is not { TypeKind: TypeKind.Enum })
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();

            var hideAdvancedMembers = options.GetOption(RecommendationOptions.HideAdvancedMembers, context.SemanticModel.Language);

            // We'll want to build a list of the actual enum members and all accessible instances of that enum, too
            var result = enumType.GetMembers().Where(m =>
                m.Kind == SymbolKind.Field &&
                ((IFieldSymbol)m).IsConst &&
                m.IsEditorBrowsable(hideAdvancedMembers, context.SemanticModel.Compilation)).ToImmutableArray();

            return Task.FromResult(result);
        }

        protected override Task<ImmutableArray<ISymbol>> GetSymbolsAsync(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var syntaxFacts = context.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsInNonUserCode(context.SyntaxTree, context.Position, cancellationToken)) // Removed: || context.SyntaxTree.IsInSkippedText(position, cancellationToken)
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();

            if (context.TargetToken.RawKind == syntaxFacts.SyntaxKinds.DotToken)
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();

            var typeInferenceService = context.GetLanguageService<ITypeInferenceService>();
            var span = new TextSpan(position, 0);
            var enumType = typeInferenceService.InferType(context.SemanticModel, position, objectAsDefault: true, cancellationToken: cancellationToken);

            if (enumType is not { TypeKind: TypeKind.Enum })
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();

            var hideAdvancedMembers = options.GetOption(RecommendationOptions.HideAdvancedMembers, context.SemanticModel.Language);

            var otherSymbols = context.SemanticModel.LookupSymbols(position).WhereAsArray(s =>
                s.MatchesKind(SymbolKind.Field, SymbolKind.Local, SymbolKind.Parameter, SymbolKind.Property) &&
                s.IsEditorBrowsable(hideAdvancedMembers, context.SemanticModel.Compilation));

            var otherInstances = otherSymbols.WhereAsArray(s => Equals(enumType, s.GetSymbolType()));

            return Task.FromResult(otherInstances.Concat(enumType));
        }

        // PERF: Cached values for GetDisplayAndInsertionText. Cuts down on the number of calls to ToMinimalDisplayString for large enums.
        private (INamedTypeSymbol? containingType, SyntaxContext? context, string? containingTypeText) _cachedDisplayAndInsertionText;

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, SyntaxContext context)
        {
            if (symbol.ContainingType != null && symbol.ContainingType.TypeKind == TypeKind.Enum)
            {
                if (!Equals(_cachedDisplayAndInsertionText.containingType, symbol.ContainingType) || _cachedDisplayAndInsertionText.context != context)
                {
                    var displayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType).WithLocalOptions(SymbolDisplayLocalOptions.None);
                    var containingTypeText = symbol.ContainingType.ToMinimalDisplayString(context.SemanticModel, context.Position, displayFormat);
                    _cachedDisplayAndInsertionText = (symbol.ContainingType, context, containingTypeText);
                }

                var text = _cachedDisplayAndInsertionText.containingTypeText + "." + symbol.Name;
                return (text, "", text);
            }

            return GetDefaultDisplayAndSuffixAndInsertionText(symbol, context);
        }
    }
}
