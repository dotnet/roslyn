// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal abstract class CommonQuickInfoProvider : QuickInfoProvider
{
    protected abstract Task<QuickInfoItem?> BuildQuickInfoAsync(QuickInfoContext context, SyntaxToken token);
    protected abstract Task<QuickInfoItem?> BuildQuickInfoAsync(CommonQuickInfoContext context, SyntaxToken token);

    public override async Task<QuickInfoItem?> GetQuickInfoAsync(QuickInfoContext context)
    {
        var cancellationToken = context.CancellationToken;
        var tree = await context.Document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var tokens = await GetTokensAsync(tree, context.Position, context.CancellationToken).ConfigureAwait(false);

        foreach (var token in tokens)
        {
            var info = await GetQuickInfoAsync(context, token).ConfigureAwait(false);
            if (info != null)
                return info;
        }

        return null;
    }

    public async Task<QuickInfoItem?> GetQuickInfoAsync(CommonQuickInfoContext context)
    {
        var tokens = await GetTokensAsync(context.SemanticModel.SyntaxTree, context.Position, context.CancellationToken).ConfigureAwait(false);

        foreach (var token in tokens)
        {
            var info = await GetQuickInfoAsync(context, token).ConfigureAwait(false);
            if (info != null)
                return info;
        }

        return null;
    }

    protected async Task<ImmutableArray<SyntaxToken>> GetTokensAsync(SyntaxTree tree, int position, System.Threading.CancellationToken cancellationToken)
    {
        using var result = TemporaryArray<SyntaxToken>.Empty;
        var token = await tree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
        if (token != default)
        {
            result.Add(token);

            if (ShouldCheckPreviousToken(token))
            {
                token = token.GetPreviousToken();
                if (token != default && token.Span.IntersectsWith(position))
                    result.Add(token);
            }
        }

        return result.ToImmutableAndClear();
    }

    protected virtual bool ShouldCheckPreviousToken(SyntaxToken token)
        => true;

    private async Task<QuickInfoItem?> GetQuickInfoAsync(
        QuickInfoContext context,
        SyntaxToken token)
    {
        if (token != default &&
            token.Span.IntersectsWith(context.Position))
        {
            return await BuildQuickInfoAsync(context, token).ConfigureAwait(false);
        }

        return null;
    }

    private async Task<QuickInfoItem?> GetQuickInfoAsync(
        CommonQuickInfoContext context,
        SyntaxToken token)
    {
        if (token != default &&
            token.Span.IntersectsWith(context.Position))
        {
            return await BuildQuickInfoAsync(context, token).ConfigureAwait(false);
        }

        return null;
    }
}
