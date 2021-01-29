﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptInlineRenameInfo
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
        VSTypeScriptGlyph Glyph { get; }

        /// <summary>
        /// The locations of the symbol being renamed.
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
        TextSpan GetReferenceEditSpan(VSTypeScriptInlineRenameLocationWrapper location, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the actual span that should be edited in the buffer for a given rename conflict
        /// location.
        /// </summary>
        TextSpan? GetConflictEditSpan(VSTypeScriptInlineRenameLocationWrapper location, string replacementText, CancellationToken cancellationToken);

        /// <summary>
        /// Determine the set of locations to rename given the provided options. May be called 
        /// multiple times.  For example, this can be called one time for the initial set of
        /// locations to rename, as well as any time the rename options are changed by the user.
        /// </summary>
        Task<IVSTypeScriptInlineRenameLocationSet> FindRenameLocationsAsync(OptionSet optionSet, CancellationToken cancellationToken);

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
}
