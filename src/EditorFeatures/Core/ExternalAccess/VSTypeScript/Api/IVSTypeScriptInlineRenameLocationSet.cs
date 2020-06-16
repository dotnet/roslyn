// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptInlineRenameLocationSet
    {
        /// <summary>
        /// The set of locations that need to be updated with the replacement text that the user
        /// has entered in the inline rename session.  These are the locations are all relative
        /// to the solution when the inline rename session began.
        /// </summary>
        IList<VSTypeScriptInlineRenameLocationWrapper> Locations { get; }

        /// <summary>
        /// Returns the set of replacements and their possible resolutions if the user enters the
        /// provided replacement text and options.  Replacements are keyed by their document id
        /// and TextSpan in the original solution, and specify their new span and possible conflict
        /// resolution.
        /// </summary>
        Task<IVSTypeScriptInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, OptionSet optionSet, CancellationToken cancellationToken);
    }
}
