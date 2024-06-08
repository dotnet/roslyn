// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Structure
{
    internal static class OmniSharpBlockStructureService
    {
        public static async Task<OmniSharpBlockStructure?> GetBlockStructureAsync(Document document, OmniSharpBlockStructureOptions options, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<BlockStructureService>();
            var blockStructure = await service.GetBlockStructureAsync(document, options.ToBlockStructureOptions(), cancellationToken).ConfigureAwait(false);
            if (blockStructure != null)
            {
                return new OmniSharpBlockStructure(blockStructure.Spans.SelectAsArray(x => new OmniSharpBlockSpan(x.Type, x.IsCollapsible, x.TextSpan, x.HintSpan, x.BannerText, x.AutoCollapse, x.IsDefaultCollapsed)));
            }
            else
            {
                return null;
            }
        }
    }
}
