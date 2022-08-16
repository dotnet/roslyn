// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
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
        private readonly PooledDictionary<TextSpan, HashSet<StringAndCommentRenameContext>> _textSpanToStringAndCommentContexts;
        private readonly PooledHashSet<string> _allReplacementTexts;
        private readonly PooledHashSet<string> _allOriginalTexts;
        private readonly PooledHashSet<string> _allPossibleConflictNames;

        public Builder()
        {
            _textSpanToLocationContexts = PooledDictionary<TextSpan, LocationRenameContext>.GetInstance();
            _renameSymbolContexts = PooledDictionary<SymbolKey, RenamedSymbolContext>.GetInstance();
            _textSpanToStringAndCommentContexts = PooledDictionary<TextSpan, HashSet<StringAndCommentRenameContext>>.GetInstance();
            _allReplacementTexts = PooledHashSet<string>.GetInstance();
            _allOriginalTexts = PooledHashSet<string>.GetInstance();
            _allPossibleConflictNames = PooledHashSet<string>.GetInstance();
        }

        /// <summary>
        /// Add locationRenameContext to the builder.
        /// </summary>
        /// <returns>
        /// Return true if the given textSpan of <param name="locationRenameContext"/> already exists in the builder.
        /// And the existing context is not same as the input context. Otherwise, false.
        /// </returns>
        public bool AddLocationRenameContext(LocationRenameContext locationRenameContext)
        {
            RoslynDebug.Assert(!locationRenameContext.RenameLocation.IsRenameInStringOrComment);
            var textSpan = locationRenameContext.RenameLocation.Location.SourceSpan;
            if (_textSpanToLocationContexts.TryGetValue(textSpan, out var existingLocationContext)
                && !existingLocationContext.Equals(locationRenameContext))
            {
                // We are trying to rename a location with different rename context.
                return true;
            }
            else
            {
                _textSpanToLocationContexts[textSpan] = locationRenameContext;
                return false;
            }
        }

        public void AddRenamedSymbol(ISymbol symbol, string replacementText, bool replacementTextValid)
        {
            var symbolKey = symbol.GetSymbolKey();
            if (!_renameSymbolContexts.ContainsKey(symbolKey))
            {
                var symbolContext = new RenamedSymbolContext(replacementText, symbol.Name, symbol, symbol as IAliasSymbol, replacementTextValid);
                _renameSymbolContexts[symbolKey] = symbolContext;
            }
        }

        public bool AddStringAndCommentRenameContext(StringAndCommentRenameContext stringAndCommentRenameContext)
        {
            RoslynDebug.Assert(!stringAndCommentRenameContext.RenameLocation.IsRenameInStringOrComment);
            var containLocation = stringAndCommentRenameContext.RenameLocation.ContainingLocationForStringOrComment;
            if (_textSpanToStringAndCommentContexts.TryGetValue(containLocation, out var subLocationSet))
            {
                if (subLocationSet.Contains(stringAndCommentRenameContext))
                {
                    // We are tyring to rename a location with different rename context.
                    return true;
                }
                else
                {
                    subLocationSet.Add(stringAndCommentRenameContext);
                    return false;
                }
            }
            else
            {
                _textSpanToStringAndCommentContexts[containLocation] =
                    new HashSet<StringAndCommentRenameContext>() { stringAndCommentRenameContext };
                return false;
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
