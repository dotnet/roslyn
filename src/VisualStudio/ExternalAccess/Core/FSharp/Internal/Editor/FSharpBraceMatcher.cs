// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor;

[ExportBraceMatcher(LanguageNames.FSharp), Shared]
internal class FSharpBraceMatcher : IBraceMatcher
{
    private readonly IFSharpBraceMatcher _braceMatcher;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpBraceMatcher(IFSharpBraceMatcher braceMatcher)
    {
        _braceMatcher = braceMatcher;
    }

    public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
    {
        var result = await _braceMatcher.FindBracesAsync(document, position, cancellationToken).ConfigureAwait(false);
        return result.HasValue ? new BraceMatchingResult(result.Value.LeftSpan, result.Value.RightSpan) : null;
    }
}
