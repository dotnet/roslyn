// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract class CommonQuickInfoProvider : QuickInfoProvider
    {
        public override async Task<QuickInfoItem> GetQuickInfoAsync(QuickInfoContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
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
        {
            return true;
        }

        private async Task<QuickInfoItem> GetQuickInfoAsync(
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

        protected abstract Task<QuickInfoItem> BuildQuickInfoAsync(Document document, SyntaxToken token, CancellationToken cancellationToken);
    }
}
