// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

[Obsolete]
internal interface IFSharpInlineRenameReplacementInfo
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
    IEnumerable<FSharpInlineRenameReplacement> GetReplacements(DocumentId documentId);
}

[Obsolete]
internal interface IFSharpInlineRenameLocationSet
{
    /// <summary>
    /// The set of locations that need to be updated with the replacement text that the user
    /// has entered in the inline rename session.  These are the locations are all relative
    /// to the solution when the inline rename session began.
    /// </summary>
    IList<FSharpInlineRenameLocation> Locations { get; }

    /// <summary>
    /// Returns the set of replacements and their possible resolutions if the user enters the
    /// provided replacement text and options.  Replacements are keyed by their document id
    /// and TextSpan in the original solution, and specify their new span and possible conflict
    /// resolution.
    /// </summary>
    Task<IFSharpInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, OptionSet optionSet, CancellationToken cancellationToken);
}

[Obsolete]
internal interface IFSharpInlineRenameInfo
{
    /// <summary>
    /// Whether or not the entity at the selected location can be renamed.
    /// </summary>
    bool CanRename { get; }

    /// <summary>
    /// Provides the reason that can be displayed to the user if the entity at the selected 
    /// location cannot be renamed.
    /// </summary>
    string LocalizedErrorMessage { get; }

    /// <summary>
    /// The span of the entity that is being renamed.
    /// </summary>
    TextSpan TriggerSpan { get; }

    /// <summary>
    /// Whether or not this entity has overloads that can also be renamed if the user wants.
    /// </summary>
    bool HasOverloads { get; }

    /// <summary>
    /// Whether the Rename Overloads option should be forced to true. Used if rename is invoked from within a nameof expression.
    /// </summary>
    bool ForceRenameOverloads { get; }

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
    FSharpGlyph Glyph { get; }

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
    TextSpan GetReferenceEditSpan(FSharpInlineRenameLocation location, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the actual span that should be edited in the buffer for a given rename conflict
    /// location.
    /// </summary>
    TextSpan? GetConflictEditSpan(FSharpInlineRenameLocation location, string replacementText, CancellationToken cancellationToken);

    /// <summary>
    /// Determine the set of locations to rename given the provided options. May be called 
    /// multiple times.  For example, this can be called one time for the initial set of
    /// locations to rename, as well as any time the rename options are changed by the user.
    /// </summary>
    Task<IFSharpInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken);

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
}

/// <summary>
/// Language service that allows a language to participate in the editor's inline rename feature.
/// </summary>
[Obsolete]
internal interface IFSharpEditorInlineRenameService
{
    Task<IFSharpInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken);
}
