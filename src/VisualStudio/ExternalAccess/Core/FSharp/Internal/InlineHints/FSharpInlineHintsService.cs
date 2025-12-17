// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.InlineHints;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.InlineHints;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.InlineHints;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.InlineHints;
#endif

[ExportLanguageService(typeof(IInlineHintsService), LanguageNames.FSharp), Shared]
internal class FSharpInlineHintsService : IInlineHintsService
{
    private readonly IFSharpInlineHintsService? _service;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpInlineHintsService(
        [Import(AllowDefault = true)] IFSharpInlineHintsService? service)
    {
        _service = service;
    }

    public async Task<ImmutableArray<InlineHint>> GetInlineHintsAsync(
        Document document, TextSpan textSpan, InlineHintsOptions options, bool displayAllOverride, CancellationToken cancellationToken)
    {
        if (_service == null)
            return [];

        var hints = await _service.GetInlineHintsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        return hints.SelectAsArray(h => new InlineHint(h.Span, h.DisplayParts, (d, c) => h.GetDescriptionAsync(d, c)));
    }
}
