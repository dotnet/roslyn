// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Host.Mef;

#if Unified_ExternalAccess 
using Microsoft.CodeAnalysis.ExternalAccess.Unified.FSharp.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.FSharp.Internal.Editor;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor;
#endif

[ExportBraceMatcher(LanguageNames.FSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class FSharpBraceMatcher([Import(AllowDefault = true)] IFSharpBraceMatcher braceMatcher) : IBraceMatcher
{
    private readonly IFSharpBraceMatcher _braceMatcher = braceMatcher;

    public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
    {
        if (_braceMatcher == null)
            return null;

        var result = await _braceMatcher.FindBracesAsync(document, position, cancellationToken).ConfigureAwait(false);
        return result.HasValue ? new BraceMatchingResult(result.Value.LeftSpan, result.Value.RightSpan) : null;
    }
}
