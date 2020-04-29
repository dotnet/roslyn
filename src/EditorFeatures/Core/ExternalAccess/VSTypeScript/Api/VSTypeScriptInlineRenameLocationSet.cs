// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal sealed class VSTypeScriptInlineRenameLocationSet : IInlineRenameLocationSet
    {
        private readonly IVSTypeScriptInlineRenameLocationSet _set;

        public VSTypeScriptInlineRenameLocationSet(IVSTypeScriptInlineRenameLocationSet set)
        {
            Contract.ThrowIfNull(set);
            _set = set;
            Locations = set.Locations?.Select(x => new InlineRenameLocation(x.Document, x.TextSpan)).ToList();
        }

        public IList<InlineRenameLocation> Locations { get; }

        public async Task<IInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, OptionSet optionSet, CancellationToken cancellationToken)
        {
            var info = await _set.GetReplacementsAsync(replacementText, optionSet, cancellationToken).ConfigureAwait(false);
            if (info != null)
            {
                return new VSTypeScriptInlineRenameReplacementInfo(info);
            }
            else
            {
                return null;
            }
        }
    }
}
