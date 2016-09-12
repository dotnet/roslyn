// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract class CommonQuickInfoProvider : QuickInfoProvider
    {
        public override async Task<QuickInfoItem> GetQuickInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
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
            if (token != default(SyntaxToken) &&
                token.Span.IntersectsWith(position))
            {
                return await BuildQuickInfoAsync(document, token, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        protected abstract Task<QuickInfoItem> BuildQuickInfoAsync(Document document, SyntaxToken token, CancellationToken cancellationToken);
    }
}