// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractOverrideCompletionProvider
    {
        private partial class ItemGetter
        {
            private readonly CancellationToken _cancellationToken;
            private readonly int _position;
            private readonly AbstractOverrideCompletionProvider _provider;
            private readonly SymbolDisplayFormat _overrideNameFormat = SymbolDisplayFormats.NameFormat.WithParameterOptions(
                SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeExtensionThis |
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeName |
                SymbolDisplayParameterOptions.IncludeParamsRefOut)
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            private readonly Document _document;
            private readonly SourceText _text;
            private readonly SyntaxTree _syntaxTree;
            private readonly int _startLineNumber;
            private readonly TextLine _startLine;

            private ItemGetter(
                AbstractOverrideCompletionProvider overrideCompletionProvider,
                Document document,
                int position,
                SourceText text,
                SyntaxTree syntaxTree,
                int startLineNumber,
                TextLine startLine,
                CancellationToken cancellationToken)
            {
                _provider = overrideCompletionProvider;
                _document = document;
                _position = position;
                _text = text;
                _syntaxTree = syntaxTree;
                _startLineNumber = startLineNumber;
                _startLine = startLine;
                _cancellationToken = cancellationToken;
            }

            internal static async Task<ItemGetter> CreateAsync(
                AbstractOverrideCompletionProvider overrideCompletionProvider,
                Document document,
                int position,
                CancellationToken cancellationToken)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var startLineNumber = text.Lines.IndexOf(position);
                var startLine = text.Lines[startLineNumber];
                return new ItemGetter(overrideCompletionProvider, document, position, text, syntaxTree, startLineNumber, startLine, cancellationToken);
            }

            internal async Task<IEnumerable<CompletionItem>> GetItemsAsync()
            {
                // modifiers* override modifiers* type? |
                if (!TryCheckForTrailingTokens(_position))
                {
                    return null;
                }

                var startToken = _provider.FindStartingToken(_syntaxTree, _position, _cancellationToken);
                if (startToken.Parent == null)
                {
                    return null;
                }

                var semanticModel = await _document.GetSemanticModelForNodeAsync(startToken.Parent, _cancellationToken).ConfigureAwait(false);
                if (!_provider.TryDetermineReturnType(startToken, semanticModel, _cancellationToken, out var returnType, out var tokenAfterReturnType) ||
                    !_provider.TryDetermineModifiers(tokenAfterReturnType, _text, _startLineNumber, out var seenAccessibility, out var modifiers) ||
                    !TryDetermineOverridableMembers(semanticModel, startToken, seenAccessibility, out var overridableMembers))
                {
                    return null;
                }

                overridableMembers = _provider.FilterOverrides(overridableMembers, returnType);
                var symbolDisplayService = _document.GetLanguageService<ISymbolDisplayService>();

                var resolvableMembers = overridableMembers.Where(m => CanResolveSymbolKey(m, semanticModel.Compilation));

                return overridableMembers.Select(m => CreateItem(
                    m, symbolDisplayService, semanticModel, startToken, modifiers)).ToList();
            }

            private bool CanResolveSymbolKey(ISymbol m, Compilation compilation)
            {
                // SymbolKey doesn't guarantee roundtrip-ability, which we need in order to generate overrides.
                // Preemptively filter out those methods whose SymbolKeys we won't be able to round trip.
                var key = SymbolKey.Create(m, _cancellationToken);
                var result = key.Resolve(compilation, cancellationToken: _cancellationToken);
                return result.Symbol != null;
            }

            private CompletionItem CreateItem(
                ISymbol symbol, ISymbolDisplayService symbolDisplayService,
                SemanticModel semanticModel, SyntaxToken startToken, DeclarationModifiers modifiers)
            {
                var position = startToken.SpanStart;

                var displayString = symbolDisplayService.ToMinimalDisplayString(semanticModel, position, symbol, _overrideNameFormat);

                return MemberInsertionCompletionItem.Create(
                    displayString,
                    displayTextSuffix: "",
                    modifiers,
                    _startLineNumber,
                    symbol,
                    startToken,
                    position,
                    rules: _provider.GetRules());
            }

            private bool TryDetermineOverridableMembers(
                SemanticModel semanticModel, SyntaxToken startToken,
                Accessibility seenAccessibility, out ImmutableArray<ISymbol> overridableMembers)
            {
                var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(startToken.SpanStart, _cancellationToken);
                var result = containingType.GetOverridableMembers(_cancellationToken);

                // Filter based on accessibility
                if (seenAccessibility != Accessibility.NotApplicable)
                {
                    result = result.WhereAsArray(m => m.DeclaredAccessibility == seenAccessibility);
                }

                overridableMembers = result;
                return overridableMembers.Length > 0;
            }

            private bool TryCheckForTrailingTokens(int position)
            {
                var root = _syntaxTree.GetRoot(_cancellationToken);
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

            private bool IsOnStartLine(int position)
            {
                return _text.Lines.IndexOf(position) == _startLineNumber;
            }
        }
    }
}
