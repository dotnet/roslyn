// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
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

        // Allow large array instances in the pool, as these arrays often exceed the ArrayBuilder reuse size threshold
        using var _ = ArrayBuilder<InlineHint>.GetInstance(discardLargeInstances: false, out var result);

        if (inlineParameterService is not null)
        {
            await inlineParameterService.AddInlineHintsAsync(document, textSpan, options.ParameterOptions, options.DisplayOptions, displayAllOverride, result, cancellationToken).ConfigureAwait(false);
        }

        if (inlineTypeService is not null)
        {
            await inlineTypeService.AddInlineHintsAsync(document, textSpan, options.TypeOptions, options.DisplayOptions, displayAllOverride, result, cancellationToken).ConfigureAwait(false);
        }

        return result.ToImmutableAndClear();
    }
}
