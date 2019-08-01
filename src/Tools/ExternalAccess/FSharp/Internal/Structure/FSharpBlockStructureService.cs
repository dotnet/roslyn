// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public override async Task<BlockStructure> GetBlockStructureAsync(Document document, CancellationToken cancellationToken)
        {
            var blockStructure = await _service.GetBlockStructureAsync(document, cancellationToken).ConfigureAwait(false);
            if (blockStructure != null)
            {
                return new BlockStructure(blockStructure.Spans.SelectAsArray(x => new BlockSpan(x.Type, x.IsCollapsible, x.TextSpan, x.HintSpan, x.BannerText, x.AutoCollapse, x.IsDefaultCollapsed)));
            }
            else
            {
                return null;
            }
        }
    }
}
