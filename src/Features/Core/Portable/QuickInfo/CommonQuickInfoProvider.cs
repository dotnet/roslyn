// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract class CommonQuickInfoProvider : QuickInfoProvider
    {
        public override async Task<QuickInfoItem?> GetQuickInfoAsync(QuickInfoContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = await tree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);

            var info = await GetQuickInfoAsync(document, token, position, cancellationToken).ConfigureAwait(false);

            if (info == null && ShouldCheckPreviousToken(token))
            {
                var previousToken = token.GetPreviousToken();
                info = await GetQuickInfoAsync(document, previousToken, position, cancellationToken).ConfigureAwait(false);
            }

            return info;
        }

        protected virtual bool ShouldCheckPreviousToken(SyntaxToken token)
            => true;

        private async Task<QuickInfoItem?> GetQuickInfoAsync(
            Document document,
            SyntaxToken token,
            int position,
            CancellationToken cancellationToken)
        {
            if (token != default &&
                token.Span.IntersectsWith(position))
            {
                return await BuildQuickInfoAsync(document, token, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        protected abstract Task<QuickInfoItem?> BuildQuickInfoAsync(Document document, SyntaxToken token, CancellationToken cancellationToken);
    }
}
