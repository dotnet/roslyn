// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                SymbolDisplayParameterOptions.IncludeParamsRefOut);

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

                ITypeSymbol returnType;
                DeclarationModifiers modifiers;
                Accessibility seenAccessibility;
                SyntaxToken tokenAfterReturnType;
                ISet<ISymbol> overridableMembers;
                if (!_provider.TryDetermineReturnType(startToken, semanticModel, _cancellationToken, out returnType, out tokenAfterReturnType) ||
                    !_provider.TryDetermineModifiers(tokenAfterReturnType, _text, _startLineNumber, out seenAccessibility, out modifiers) ||
                    !TryDetermineOverridableMembers(semanticModel, startToken, seenAccessibility, out overridableMembers))
                {
                    return null;
                }

                overridableMembers = _provider.FilterOverrides(overridableMembers, returnType);
                var symbolDisplayService = _document.GetLanguageService<ISymbolDisplayService>();

                return overridableMembers.Select(m => CreateItem(
                    m, symbolDisplayService, semanticModel, startToken, modifiers)).ToList();
            }

            private CompletionItem CreateItem(
                ISymbol symbol, ISymbolDisplayService symbolDisplayService,
                SemanticModel semanticModel, SyntaxToken startToken, DeclarationModifiers modifiers)
            {
                var position = startToken.SpanStart;

                var displayString = symbolDisplayService.ToMinimalDisplayString(semanticModel, position, symbol, _overrideNameFormat);

                return  MemberInsertionCompletionItem.Create(
                    displayString,
                    symbol.GetGlyph(),
                    modifiers,
                    _startLineNumber,
                    symbol,
                    startToken,
                    position,
                    rules: _provider.GetRules());
            }

            private bool TryDetermineOverridableMembers(
                SemanticModel semanticModel, SyntaxToken startToken, Accessibility seenAccessibility, out ISet<ISymbol> overridableMembers)
            {
                var result = new HashSet<ISymbol>();

                var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(startToken.SpanStart, _cancellationToken);
                if (containingType != null && !containingType.IsScriptClass && !containingType.IsImplicitClass)
                {
                    if (containingType.TypeKind == TypeKind.Class || containingType.TypeKind == TypeKind.Struct)
                    {
                        var baseTypes = containingType.GetBaseTypes().Reverse();
                        foreach (var type in baseTypes)
                        {
                            _cancellationToken.ThrowIfCancellationRequested();

                            // Prefer overrides in derived classes
                            RemoveOverriddenMembers(result, type);

                            // Retain overridable methods
                            AddOverridableMembers(result, containingType, type);
                        }

                        // Don't suggest already overridden members
                        RemoveOverriddenMembers(result, containingType);
                    }
                }

                // Filter based on accessibility
                if (seenAccessibility != Accessibility.NotApplicable)
                {
                    result.RemoveWhere(m => m.DeclaredAccessibility != seenAccessibility);
                }

                overridableMembers = result;
                return overridableMembers.Count > 0;
            }

            private void AddOverridableMembers(HashSet<ISymbol> result, INamedTypeSymbol containingType, INamedTypeSymbol type)
            {
                foreach (var member in type.GetMembers())
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    if (_provider.IsOverridable(member, containingType))
                    {
                        result.Add(member);
                    }
                }
            }

            private void RemoveOverriddenMembers(HashSet<ISymbol> result, INamedTypeSymbol containingType)
            {
                foreach (var member in containingType.GetMembers())
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var overriddenMember = member.OverriddenMember();
                    if (overriddenMember != null)
                    {
                        result.Remove(overriddenMember);
                    }
                }
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
