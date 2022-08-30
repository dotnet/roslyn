// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

internal partial record DocumentRenameInfo
{
    /// <summary>
    /// A mutable builder for DocumentRenameInfo.
    /// </summary>
    internal class Builder : IDisposable
    {
        private readonly PooledDictionary<TextSpan, LocationRenameContext> _textSpanToLocationContexts;
        private readonly PooledDictionary<SymbolKey, RenamedSymbolContext> _renameSymbolContexts;
        private readonly PooledDictionary<TextSpan, SortedDictionary<TextSpan, string>> _textSpanToStringAndCommentContexts;
        private readonly PooledHashSet<string> _allReplacementTexts;
        private readonly PooledHashSet<string> _allOriginalTexts;
        private readonly PooledHashSet<string> _allPossibleConflictNames;

        public Builder()
        {
            _textSpanToLocationContexts = PooledDictionary<TextSpan, LocationRenameContext>.GetInstance();
            _renameSymbolContexts = PooledDictionary<SymbolKey, RenamedSymbolContext>.GetInstance();
            _textSpanToStringAndCommentContexts = PooledDictionary<TextSpan, SortedDictionary<TextSpan, string>>.GetInstance();
            _allReplacementTexts = PooledHashSet<string>.GetInstance();
            _allOriginalTexts = PooledHashSet<string>.GetInstance();
            _allPossibleConflictNames = PooledHashSet<string>.GetInstance();
        }

        /// <summary>
        /// Add locationRenameContext to the builder.
        /// </summary>
        /// <returns>
        /// Return true if the given textSpan of <param name="locationRenameContext"/> already exists in the builder
        /// and the existing context is not same as the input context. Otherwise, false.
        /// </returns>
        public bool AddLocationRenameContext(LocationRenameContext locationRenameContext)
        {
            RoslynDebug.Assert(!locationRenameContext.RenameLocation.IsRenameInStringOrComment);
            var textSpan = locationRenameContext.RenameLocation.Location.SourceSpan;
            if (_textSpanToLocationContexts.TryGetValue(textSpan, out var existingLocationContext)
                && !existingLocationContext.Equals(locationRenameContext))
            {
                // We are trying to rename a same location with different rename context.
                return true;
            }
            else
            {
                _textSpanToLocationContexts[textSpan] = locationRenameContext;
                return false;
            }
        }

        public void AddRenamedSymbol(ISymbol symbol, string replacementText, bool replacementTextValid, ImmutableArray<string> possibleNamingConflict)
        {
            var symbolKey = symbol.GetSymbolKey();
            if (!_renameSymbolContexts.ContainsKey(symbolKey))
            {
                var symbolContext = new RenamedSymbolContext(replacementText, symbol.Name, symbol, symbol as IAliasSymbol, replacementTextValid);
                _renameSymbolContexts[symbolKey] = symbolContext;
                _allReplacementTexts.Add(replacementText);
                _allOriginalTexts.Add(symbol.Name);
                _allPossibleConflictNames.AddRange(possibleNamingConflict);
            }
        }

        /// <summary>
        /// Add StringAndCommentContext to the builder.
        /// </summary>
        /// <returns>
        /// Return true if the given textSpan of <param name="stringAndCommentRenameContext"/> already exists in the builder
        /// and the existing context is not same as the input context. Otherwise, false.
        /// </returns>
        public bool AddStringAndCommentRenameContext(StringAndCommentRenameContext stringAndCommentRenameContext)
        {
            RoslynDebug.Assert(stringAndCommentRenameContext.RenameLocation.IsRenameInStringOrComment);
            var containingLocation = stringAndCommentRenameContext.RenameLocation.ContainingLocationForStringOrComment;

            var subLocation = stringAndCommentRenameContext.RenameLocation.Location;
            RoslynDebug.Assert(subLocation.IsInSource);

            var sourceSpan = subLocation.SourceSpan;
            // SourceSpan should be a part of the containing location.
            RoslynDebug.Assert(sourceSpan.Start >= containingLocation.Start);
            RoslynDebug.Assert(sourceSpan.End <= containingLocation.End);

            var subSpan = new TextSpan(sourceSpan.Start - containingLocation.Start, sourceSpan.Length);
            var replacementText = stringAndCommentRenameContext.ReplacementText;
            if (_textSpanToStringAndCommentContexts.TryGetValue(containingLocation, out var subLocationToReplacementText))
            {
                if (subLocationToReplacementText.TryGetValue(subSpan, out var existingReplacementText)
                    && existingReplacementText != replacementText)
                {
                    // Two symbols try to rename a same subSpan,
                    // Example:
                    //      // Comment Hello
                    // class Hello
                    // {
                    //
                    // }
                    // class World
                    // {
                    //    void Hello() { }
                    // }
                    // If try to rename both 'class Hello' to 'Bar' and 'void Hello()' to 'Goo'. So both of them will try to rename
                    // 'Comment Hello'.
                    return true;
                }
                else
                {
                    subLocationToReplacementText[subSpan] = replacementText;
                    return false;
                }
            }
            else
            {
                _textSpanToStringAndCommentContexts[containingLocation] =
                    new SortedDictionary<TextSpan, string>
                    {
                        { subSpan, replacementText }
                    };
                return false;
            }
        }

        public DocumentRenameInfo ToRenameInfo()
        {
            var textSpanToLocationContexts = _textSpanToLocationContexts.ToImmutableDictionary();
            var renamedSymbolContexts = _renameSymbolContexts.ToImmutableDictionary();

            var textSpanToStringAndCommentRenameContexts = ToImmutable(_textSpanToStringAndCommentContexts);

            var allReplacementTexts = _allReplacementTexts.ToImmutableHashSet();
            var allOriginalTexts = _allOriginalTexts.ToImmutableHashSet();
            var allPossibleConflictNames = _allPossibleConflictNames.ToImmutableHashSet();
            return new DocumentRenameInfo(
                textSpanToLocationContexts,
                renamedSymbolContexts,
                textSpanToStringAndCommentRenameContexts,
                allReplacementTexts,
                allOriginalTexts,
                allPossibleConflictNames);

            static ImmutableDictionary<TextSpan, ImmutableSortedDictionary<TextSpan, string>> ToImmutable(
                PooledDictionary<TextSpan, SortedDictionary<TextSpan, string>> builder)
            {
                var dictionaryBuilder = ImmutableDictionary.CreateBuilder<TextSpan, ImmutableSortedDictionary<TextSpan, string>>();
                foreach (var pair in builder)
                {
                    dictionaryBuilder[pair.Key] = pair.Value.ToImmutableSortedDictionary();
                }

                return dictionaryBuilder.ToImmutableDictionary();
            }
        }

        public void Dispose()
        {
            _textSpanToLocationContexts.Free();
            _renameSymbolContexts.Free();
            _textSpanToStringAndCommentContexts.Free();
            _allReplacementTexts.Free();
            _allOriginalTexts.Free();
            _allPossibleConflictNames.Free();
        }
    }
}
