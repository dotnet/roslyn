// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Structure;
using Microsoft.CodeAnalysis.Host.Mef;
using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Structure
{
    [Shared]
    [ExportLanguageService(typeof(BlockStructureService), LanguageNames.FSharp)]
    internal class FSharpBlockStructureService : BlockStructureService
    {
        private readonly IFSharpBlockStructureService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpBlockStructureService(IFSharpBlockStructureService service)
        {
            _service = service;
        }

        public override string Language => LanguageNames.FSharp;

        public override async Task<BlockStructure> GetBlockStructureAsync(Document document, BlockStructureOptions options, CancellationToken cancellationToken)
        {
            var blockStructure = await _service.GetBlockStructureAsync(document, cancellationToken).ConfigureAwait(false);
            if (blockStructure != null)
            {
                return new BlockStructure(blockStructure.Spans.SelectAsArray(
                    x => new BlockSpan(x.Type, x.IsCollapsible, x.TextSpan, x.HintSpan, subHeadings: default, x.BannerText, x.AutoCollapse, x.IsDefaultCollapsed)));
            }
            else
            {
                return null;
            }
        }
    }
}
