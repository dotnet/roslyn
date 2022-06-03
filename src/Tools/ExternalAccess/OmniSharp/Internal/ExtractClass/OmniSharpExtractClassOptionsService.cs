// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ExtractClass;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Internal.ExtractClass
{
    [Shared]
    [ExportWorkspaceService(typeof(IExtractClassOptionsService))]
    internal class OmniSharpExtractClassOptionsService : IExtractClassOptionsService
    {
        private readonly IOmniSharpExtractClassOptionsService _omniSharpExtractClassOptionsService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OmniSharpExtractClassOptionsService(IOmniSharpExtractClassOptionsService omniSharpExtractClassOptionsService)
        {
            _omniSharpExtractClassOptionsService = omniSharpExtractClassOptionsService;
        }

        public async Task<ExtractClassOptions?> GetExtractClassOptionsAsync(Document document, INamedTypeSymbol originalType, ISymbol? selectedMember, CancellationToken cancellationToken)
        {
            var result = await _omniSharpExtractClassOptionsService.GetExtractClassOptionsAsync(document, originalType, selectedMember).ConfigureAwait(false);
            return result == null
                ? null
                : new ExtractClassOptions(
                    result.FileName,
                    result.TypeName,
                    result.SameFile,
                    result.MemberAnalysisResults.SelectAsArray(m => new ExtractClassMemberAnalysisResult(m.Member, m.MakeAbstract)));
        }
    }
}
