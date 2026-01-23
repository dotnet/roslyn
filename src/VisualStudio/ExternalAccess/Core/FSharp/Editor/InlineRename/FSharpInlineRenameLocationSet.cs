// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Rename;

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

internal abstract class FSharpInlineRenameLocationSet : IInlineRenameLocationSet
{
    /// <summary>
    /// The set of locations that need to be updated with the replacement text that the user
    /// has entered in the inline rename session.  These are the locations are all relative
    /// to the solution when the inline rename session began.
    /// </summary>
    public abstract IList<FSharpInlineRenameLocation> Locations { get; }

    IList<InlineRenameLocation> IInlineRenameLocationSet.Locations
        => Locations?.Select(x => new InlineRenameLocation(x.Document, x.TextSpan)).ToList();

    /// <summary>
    /// Returns the set of replacements and their possible resolutions if the user enters the
    /// provided replacement text and options.  Replacements are keyed by their document id
    /// and TextSpan in the original solution, and specify their new span and possible conflict
    /// resolution.
    /// </summary>
    public abstract Task<FSharpInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, CancellationToken cancellationToken);

    async Task<IInlineRenameReplacementInfo> IInlineRenameLocationSet.GetReplacementsAsync(string replacementText, SymbolRenameOptions options, CancellationToken cancellationToken)
        => await GetReplacementsAsync(replacementText, cancellationToken).ConfigureAwait(false);
}
