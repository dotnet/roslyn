// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineHints;

internal abstract class AbstractInlineHintsService : IInlineHintsService
{
    public async Task<ImmutableArray<InlineHint>> GetInlineHintsAsync(
        Document document, TextSpan textSpan, InlineHintsOptions options, bool displayAllOverride, CancellationToken cancellationToken)
    {
        var inlineParameterService = document.GetLanguageService<IInlineParameterNameHintsService>();
        var inlineTypeService = document.GetLanguageService<IInlineTypeHintsService>();

        var parameters = inlineParameterService == null
            ? []
            : await inlineParameterService.GetInlineHintsAsync(document, textSpan, options.ParameterOptions, options.DisplayOptions, displayAllOverride, cancellationToken).ConfigureAwait(false);

        var types = inlineTypeService == null
            ? []
            : await inlineTypeService.GetInlineHintsAsync(document, textSpan, options.TypeOptions, options.DisplayOptions, displayAllOverride, cancellationToken).ConfigureAwait(false);

        return [.. parameters, .. types];
    }
}
