// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor;

internal readonly struct InlineRenameLocation
{
    public Document Document { get; }
    public TextSpan TextSpan { get; }

    public InlineRenameLocation(Document document, TextSpan textSpan) : this()
    {
        this.Document = document;
        this.TextSpan = textSpan;
    }
}

internal enum InlineRenameReplacementKind
{
    NoConflict,
    ResolvedReferenceConflict,
    ResolvedNonReferenceConflict,
    UnresolvedConflict,
    Complexified,
}

internal enum InlineRenameFileRenameInfo
{
    /// <summary>
    /// This operation is not allowed
    /// on the symbol being renamed
    /// </summary>
    NotAllowed,

    /// <summary>
    /// The type being renamed has multiple definition
    /// locations which is not supported.
    /// </summary>
    TypeWithMultipleLocations,

    /// <summary>
    /// The type being renamed doesn't match the file
    /// name prior to renaming
    /// </summary>
    TypeDoesNotMatchFileName,

    /// <summary>
    /// File rename is allowed
    /// </summary>
    Allowed
}

internal readonly struct InlineRenameReplacement
{
    public InlineRenameReplacementKind Kind { get; }
    public TextSpan OriginalSpan { get; }
    public TextSpan NewSpan { get; }

    public InlineRenameReplacement(InlineRenameReplacementKind kind, TextSpan originalSpan, TextSpan newSpan) : this()
    {
        this.Kind = kind;
        this.OriginalSpan = originalSpan;
        this.NewSpan = newSpan;
    }

    internal InlineRenameReplacement(RelatedLocation location, TextSpan newSpan)
        : this(GetReplacementKind(location), location.ConflictCheckSpan, newSpan)
    {
    }

    private static InlineRenameReplacementKind GetReplacementKind(RelatedLocation location)
    {
        switch (location.Type)
        {
            case RelatedLocationType.NoConflict:
                return InlineRenameReplacementKind.NoConflict;
            case RelatedLocationType.ResolvedReferenceConflict:
                return InlineRenameReplacementKind.ResolvedReferenceConflict;
            case RelatedLocationType.ResolvedNonReferenceConflict:
                return InlineRenameReplacementKind.ResolvedNonReferenceConflict;
            case RelatedLocationType.UnresolvableConflict:
            case RelatedLocationType.UnresolvedConflict:
                return InlineRenameReplacementKind.UnresolvedConflict;
            default:
            case RelatedLocationType.PossiblyResolvableConflict:
                throw ExceptionUtilities.UnexpectedValue(location.Type);
        }
    }
}

internal interface IInlineRenameReplacementInfo
{
    /// <summary>
    /// The solution obtained after resolving all conflicts.
    /// </summary>
    Solution NewSolution { get; }

    /// <summary>
    /// Whether or not the replacement text entered by the user is valid.
    /// </summary>
    bool ReplacementTextValid { get; }

    /// <summary>
    /// The documents that need to be updated.
    /// </summary>
    IEnumerable<DocumentId> DocumentIds { get; }

    /// <summary>
    /// Returns all the replacements that need to be performed for the specified document.
    /// </summary>
    IEnumerable<InlineRenameReplacement> GetReplacements(DocumentId documentId);
}

internal static class InlineRenameReplacementInfoExtensions
{
    public static IEnumerable<InlineRenameReplacementKind> GetAllReplacementKinds(this IInlineRenameReplacementInfo info)
    {
        var replacements = info.DocumentIds.SelectMany(info.GetReplacements);
        return replacements.Select(r => r.Kind);
    }
}

internal interface IInlineRenameLocationSet
{
    /// <summary>
    /// The set of locations that need to be updated with the replacement text that the user
    /// has entered in the inline rename session.  These are the locations are all relative
    /// to the solution when the inline rename session began.
    /// </summary>
    IList<InlineRenameLocation> Locations { get; }

    /// <summary>
    /// Returns the set of replacements and their possible resolutions if the user enters the
    /// provided replacement text and options.  Replacements are keyed by their document id
    /// and TextSpan in the original solution, and specify their new span and possible conflict
    /// resolution.
    /// </summary>
    Task<IInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, SymbolRenameOptions options, CancellationToken cancellationToken);
}

internal interface IInlineRenameInfo
{
    /// <summary>
    /// Whether or not the entity at the selected location can be renamed.
    /// </summary>
    bool CanRename { get; }

    /// <summary>
    /// Provides the reason that can be displayed to the user if the entity at the selected 
    /// location cannot be renamed.
    /// </summary>
    string? LocalizedErrorMessage { get; }

    /// <summary>
    /// The span of the entity that is being renamed.
    /// </summary>
    TextSpan TriggerSpan { get; }

    /// <summary>
    /// Whether or not this entity has overloads that can also be renamed if the user wants.
    /// </summary>
    bool HasOverloads { get; }

    /// <summary>
    /// True if overloads must be renamed (the user is not given a choice). Used if rename is invoked from within a nameof expression.
    /// </summary>
    bool MustRenameOverloads { get; }

    /// <summary>
    /// The short name of the symbol being renamed, for use in displaying information to the user.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// The full name of the symbol being renamed, for use in displaying information to the user.
    /// </summary>
    string FullDisplayName { get; }

    /// <summary>
    /// The glyph for the symbol being renamed, for use in displaying information to the user.
    /// </summary>
    Glyph Glyph { get; }

    /// <summary>
    /// The locations of the potential rename candidates for the symbol.
    /// </summary>
    ImmutableArray<DocumentSpan> DefinitionLocations { get; }

    /// <summary>
    /// Gets the final name of the symbol if the user has typed the provided replacement text
    /// in the editor.  Normally, the final name will be same as the replacement text.  However,
    /// that may not always be the same.  For example, when renaming an attribute the replacement
    /// text may be "NewName" while the final symbol name might be "NewNameAttribute".
    /// </summary>
    string GetFinalSymbolName(string replacementText);

    /// <summary>
    /// Returns the actual span that should be edited in the buffer for a given rename reference
    /// location.
    /// </summary>
    TextSpan GetReferenceEditSpan(InlineRenameLocation location, string triggerText, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the actual span that should be edited in the buffer for a given rename conflict
    /// location.
    /// </summary>
    TextSpan? GetConflictEditSpan(InlineRenameLocation location, string triggerText, string replacementText, CancellationToken cancellationToken);

    /// <summary>
    /// Determine the set of locations to rename given the provided options. May be called 
    /// multiple times.  For example, this can be called one time for the initial set of
    /// locations to rename, as well as any time the rename options are changed by the user.
    /// </summary>
    Task<IInlineRenameLocationSet> FindRenameLocationsAsync(SymbolRenameOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Called before the rename is applied to the specified documents in the workspace.  Return 
    /// <see langword="true"/> if rename should proceed, or <see langword="false"/> if it should be canceled.
    /// </summary>
    bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText);

    /// <summary>
    /// Called after the rename is applied to the specified documents in the workspace.  Return 
    /// <see langword="true"/> if this operation succeeded, or <see langword="false"/> if it failed.
    /// </summary>
    bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, string replacementText);

    /// <summary>
    /// Returns information about the file rename capabilities of 
    /// an inline rename
    /// </summary>
    InlineRenameFileRenameInfo GetFileRenameInfo();
}

#nullable enable

/// <summary>
/// Language service that allows a language to participate in the editor's inline rename feature.
/// </summary>
internal interface IEditorInlineRenameService : ILanguageService
{
    /// <summary>
    /// Returns true if the service is currently enabled for the language (e.g. the value
    /// might depend on a feature flag.)
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Returns <see cref="IInlineRenameInfo"/> necessary to establish the inline rename session.
    /// </summary>
    Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken);

    /// <summary>
    /// Returns optional context used in Copilot addition to inline rename feature.
    /// </summary>
    /// <param name="inlineRenameInfo"></param>
    /// <param name="inlineRenameLocationSet"></param>
    /// <param name="cancellationToken"></param>
    Task<ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>> GetRenameContextAsync(
        IInlineRenameInfo inlineRenameInfo,
        IInlineRenameLocationSet inlineRenameLocationSet,
        CancellationToken cancellationToken);
}
