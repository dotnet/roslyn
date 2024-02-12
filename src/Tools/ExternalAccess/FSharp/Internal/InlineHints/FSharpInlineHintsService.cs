// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.InlineHints;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.InlineHints
{
    [ExportLanguageService(typeof(IInlineHintsService), LanguageNames.FSharp), Shared]
    internal class FSharpInlineHintsService : IInlineHintsService
    {
        private readonly IFSharpInlineHintsService _service;

        // 'service' is a required import, but MEF 2 does not support silent part rejection when a required import is
        // missing so we combine AllowDefault with a null check in the constructor to defer the exception until the part
        // is instantiated.
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpInlineHintsService(
            [Import(AllowDefault = true)] IFSharpInlineHintsService? service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task<ImmutableArray<InlineHint>> GetInlineHintsAsync(
            Document document, TextSpan textSpan, InlineHintsOptions options, bool displayAllOverride, CancellationToken cancellationToken)
        {
            var hints = await _service.GetInlineHintsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            return hints.SelectAsArray(h => new InlineHint(h.Span, h.DisplayParts, (d, c) => h.GetDescriptionAsync(d, c)));
        }
    }
}
