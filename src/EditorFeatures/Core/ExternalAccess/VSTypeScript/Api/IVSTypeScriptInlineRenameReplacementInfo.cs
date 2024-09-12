// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal abstract class VSTypeScriptInlineRenameReplacementInfo : IInlineRenameReplacementInfo
{
    /// <summary>
    /// The solution obtained after resolving all conflicts.
    /// </summary>
    public abstract Solution NewSolution { get; }

    /// <summary>
    /// Whether or not the replacement text entered by the user is valid.
    /// </summary>
    public abstract bool ReplacementTextValid { get; }

    /// <summary>
    /// The documents that need to be updated.
    /// </summary>
    public abstract IEnumerable<DocumentId> DocumentIds { get; }

    /// <summary>
    /// Returns all the replacements that need to be performed for the specified document.
    /// </summary>
    public abstract IEnumerable<VSTypeScriptInlineRenameReplacementWrapper> GetReplacements(DocumentId documentId);

    IEnumerable<InlineRenameReplacement> IInlineRenameReplacementInfo.GetReplacements(DocumentId documentId)
        => GetReplacements(documentId).Select(r => r.UnderlyingObject);
}
