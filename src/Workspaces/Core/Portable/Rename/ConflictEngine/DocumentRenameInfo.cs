// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

/// <summary>
/// Contains all the immutable information to rename and perform conflict checking for a document.
/// </summary>
internal partial record DocumentRenameInfo(
    ImmutableDictionary<TextSpan, LocationRenameContext> TextSpanToLocationContexts,
    ImmutableDictionary<SymbolKey, RenamedSymbolContext> RenamedSymbolContexts,
    MultiDictionary<TextSpan, StringAndCommentRenameContext> TextSpanToStringAndCommentRenameContexts,
    ImmutableHashSet<string> AllReplacementTexts,
    ImmutableHashSet<string> AllOriginalText,
    ImmutableHashSet<string> AllPossibleConflictNames)
{
    public (DocumentRenameInfo newDocumentRenameInfo, bool isOverlappingLocation) WithLocationRenameContext(LocationRenameContext locationRenameContext)
    {
        RoslynDebug.Assert(!locationRenameContext.RenameLocation.IsRenameInStringOrComment);
        var textSpan = locationRenameContext.RenameLocation.Location.SourceSpan;
        if (TextSpanToLocationContexts.TryGetValue(textSpan, out var existingLocationContext))
        {
            return (this, !locationRenameContext.Equals(existingLocationContext));
        }
        else
        {
            return (this with { TextSpanToLocationContexts = TextSpanToLocationContexts.Add(textSpan, locationRenameContext ) }, false );
        }
    }

    public DocumentRenameInfo WithRenamedSymbolContext(ISymbol symbol, string replacementText, bool replacementTextValid)
    {
        var symbolKey = symbol.GetSymbolKey();
        if (RenamedSymbolContexts.ContainsKey(symbolKey))
        {
            return this;
        }
        else
        {
            var symbolContext = new RenamedSymbolContext(replacementText, symbol.Name, symbol, symbol as IAliasSymbol, replacementTextValid);
            return this with { RenamedSymbolContexts = RenamedSymbolContexts.Add(symbolKey, symbolContext) };
        }
    }

    public (DocumentRenameInfo newDocumentRenameInfo, bool isOverlappingLocation) WithStringAndCommentRenameContext(StringAndCommentRenameContext stringAndCommentRenameContext)
    {
        RoslynDebug.Assert(stringAndCommentRenameContext.RenameLocation.IsRenameInStringOrComment);
        var containingLocationSpan = stringAndCommentRenameContext.RenameLocation.ContainingLocationForStringOrComment;
        if (TextSpanToStringAndCommentRenameContexts.TryGetValue(containingLocationSpan, out var replacementLocations))
        {
            if (replacementLocations.Contains(stringAndCommentRenameContext))
            {
                return (this, true);
            }
            else
            {
                // We should convert all the field here to a builder pattern
                TextSpanToStringAndCommentRenameContexts.Add(containingLocationSpan, stringAndCommentRenameContext);
                return (this, false);
            }
        }
        else
        {
            TextSpanToStringAndCommentRenameContexts.Add(containingLocationSpan, stringAndCommentRenameContext);
            return (this, false);
        }
    }
}
