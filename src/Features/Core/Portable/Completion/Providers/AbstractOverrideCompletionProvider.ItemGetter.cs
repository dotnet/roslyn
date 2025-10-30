// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract partial class AbstractOverrideCompletionProvider
{
    private sealed partial class ItemGetter(
        AbstractOverrideCompletionProvider overrideCompletionProvider,
        Document document,
        int position,
        SourceText text,
        SyntaxTree syntaxTree,
        int startLineNumber,
        CancellationToken cancellationToken)
        : AbstractItemGetter<AbstractOverrideCompletionProvider>(
            overrideCompletionProvider,
            document,
            position,
            text,
            syntaxTree,
            startLineNumber,
            cancellationToken)
    {
        public static async Task<ItemGetter> CreateAsync(
            AbstractOverrideCompletionProvider overrideCompletionProvider,
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var startLineNumber = text.Lines.IndexOf(position);
            return new ItemGetter(overrideCompletionProvider, document, position, text, syntaxTree, startLineNumber, cancellationToken);
        }

        public override async Task<ImmutableArray<CompletionItem>> GetItemsAsync()
        {
            // modifiers* override modifiers* type? |
            if (!TryCheckForTrailingTokens(Position))
                return default;

            var startToken = Provider.FindStartingToken(SyntaxTree, Position, CancellationToken);
            if (startToken.Parent == null)
                return default;

            var semanticModel = await Document.ReuseExistingSpeculativeModelAsync(startToken.Parent, CancellationToken).ConfigureAwait(false);
            if (!Provider.TryDetermineReturnType(startToken, semanticModel, CancellationToken, out var returnType, out var tokenAfterReturnType) ||
                !Provider.TryDetermineModifiers(tokenAfterReturnType, Text, StartLineNumber, out var seenAccessibility, out var modifiers) ||
                !TryDetermineOverridableMembers(semanticModel, startToken, seenAccessibility, out var overridableMembers))
            {
                return default;
            }

            return Provider
                .FilterOverrides(overridableMembers, returnType)
                .SelectAsArray(m => CreateItem(m, semanticModel, startToken, modifiers));
        }

        private CompletionItem CreateItem(
            ISymbol symbol, SemanticModel semanticModel,
            SyntaxToken startToken, DeclarationModifiers modifiers)
        {
            var position = startToken.SpanStart;

            var displayString = symbol.ToMinimalDisplayString(semanticModel, position, DefaultNameFormat);

            return MemberInsertionCompletionItem.Create(
                displayString,
                displayTextSuffix: "",
                modifiers,
                StartLineNumber,
                symbol,
                startToken,
                position,
                rules: GetRules());
        }

        private bool TryDetermineOverridableMembers(
            SemanticModel semanticModel,
            SyntaxToken startToken,
            Accessibility seenAccessibility,
            out ImmutableArray<ISymbol> overridableMembers)
        {
            var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(startToken.SpanStart, CancellationToken);
            if (containingType is null)
            {
                overridableMembers = default;
                return false;
            }

            var result = containingType.GetOverridableMembers(CancellationToken);

            // Filter based on accessibility
            if (seenAccessibility != Accessibility.NotApplicable)
                result = result.WhereAsArray(m => MatchesAccessibility(m.DeclaredAccessibility, seenAccessibility));

            overridableMembers = result;
            return overridableMembers.Length > 0;

            static bool MatchesAccessibility(Accessibility declaredAccessibility, Accessibility seenAccessibility)
            {
                // since some accessibility modifiers take two keywords, allow filtering to those if the user has
                // only typed one of the keywords.  This makes it less onerous than having to determine the exact
                // right modifier set to specify, and follows the intuition of writing less filtering less and
                // writing more filtering out more.
                return seenAccessibility switch
                {
                    // `private`, `private protected`
                    Accessibility.Private => declaredAccessibility is Accessibility.Private or Accessibility.ProtectedAndInternal,
                    // `protected`, `private protected`, `protected internal`
                    Accessibility.Protected => declaredAccessibility is Accessibility.Protected or Accessibility.ProtectedAndInternal or Accessibility.ProtectedOrInternal,
                    // `internal`, `protected internal`
                    Accessibility.Internal => declaredAccessibility is Accessibility.Internal or Accessibility.ProtectedOrInternal,
                    // For anything else, require an exact match.
                    _ => declaredAccessibility == seenAccessibility,
                };
            }
        }

        private bool TryCheckForTrailingTokens(int position)
        {
            var root = SyntaxTree.GetRoot(CancellationToken);
            var token = root.FindToken(position);

            // Don't want to offer Override completion if there's a token after the current
            // position.
            if (token.SpanStart > position)
            {
                return false;
            }

            // If the next token is also on our line then we don't want to offer completion.
            if (IsOnStartLine(token.GetNextToken().SpanStart))
            {
                return false;
            }

            return true;
        }
    }
}
