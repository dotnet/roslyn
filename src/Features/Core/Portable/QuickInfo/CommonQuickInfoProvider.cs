// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract class CommonQuickInfoProvider : QuickInfoProvider
    {
        public override async Task<QuickInfoItem?> GetQuickInfoAsync(QuickInfoContext context)
        {
            var info = await GetQuickInfoCoreAsync(context).ConfigureAwait(false);

            var token = context.Token;
            if (info == null && ShouldCheckPreviousToken(token))
            {
                var previousToken = token.GetPreviousToken();
                info = await GetQuickInfoCoreAsync(context.With(previousToken)).ConfigureAwait(false);
            }

            return info;
        }

        protected virtual bool ShouldCheckPreviousToken(SyntaxToken token)
            => true;

        private async Task<QuickInfoItem?> GetQuickInfoCoreAsync(QuickInfoContext context)
        {
            var token = context.Token;
            if (token != default &&
                token.Span.IntersectsWith(context.Position))
            {
                return await BuildQuickInfoAsync(context).ConfigureAwait(false);
            }

            return null;
        }

        protected abstract Task<QuickInfoItem?> BuildQuickInfoAsync(QuickInfoContext context);
    }
}
