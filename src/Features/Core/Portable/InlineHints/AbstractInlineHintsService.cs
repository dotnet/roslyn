// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal abstract class AbstractInlineHintsService : IInlineHintsService
    {
        public async Task<ImmutableArray<InlineHint>> GetInlineHintsAsync(
            Document document, TextSpan textSpan, InlineHintsOptions options, bool displayAllOverride, CancellationToken cancellationToken)
        {
            var inlineParameterService = document.GetLanguageService<IInlineParameterNameHintsService>();
            var inlineTypeService = document.GetLanguageService<IInlineTypeHintsService>();

            var parameters = inlineParameterService == null
                ? ImmutableArray<InlineHint>.Empty
                : await inlineParameterService.GetInlineHintsAsync(document, textSpan, options.ParameterOptions, options.DisplayOptions, displayAllOverride, cancellationToken).ConfigureAwait(false);

            var types = inlineTypeService == null
                ? ImmutableArray<InlineHint>.Empty
                : await inlineTypeService.GetInlineHintsAsync(document, textSpan, options.TypeOptions, options.DisplayOptions, displayAllOverride, cancellationToken).ConfigureAwait(false);

            return parameters.Concat(types);
        }
    }
}
