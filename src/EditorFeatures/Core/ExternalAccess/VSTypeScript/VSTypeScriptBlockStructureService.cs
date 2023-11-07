// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(BlockStructureService), InternalLanguageNames.TypeScript), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class VSTypeScriptBlockStructureService(IVSTypeScriptBlockStructureServiceImplementation impl) : BlockStructureService
    {
        private readonly IVSTypeScriptBlockStructureServiceImplementation _impl = impl;

        public override string Language => InternalLanguageNames.TypeScript;

        public override async Task<BlockStructure> GetBlockStructureAsync(Document document, BlockStructureOptions options, CancellationToken cancellationToken)
        {
            var blockStructure = await _impl.GetBlockStructureAsync(document, cancellationToken).ConfigureAwait(false);

            return new BlockStructure(blockStructure.Spans.SelectAsArray(
                x => new BlockSpan(x.Type!, x.IsCollapsible, x.TextSpan, x.HintSpan, primarySpans: null, x.BannerText, x.AutoCollapse, x.IsDefaultCollapsed)));
        }
    }
}
