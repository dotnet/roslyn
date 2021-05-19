// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract class CommonQuickInfoProvider : QuickInfoProvider
    {
        protected abstract Task<QuickInfoItem?> BuildQuickInfoAsync(
            CommonQuickInfoContext context, SyntaxToken token);

        public sealed override async Task<QuickInfoItem?> GetQuickInfoAsync(QuickInfoContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            using var linkedSemanticModels = TemporaryArray<(DocumentId documentId, SemanticModel semanticModel)>.Empty;
            var linkedDocumentIds = document.GetLinkedDocumentIds();
            foreach (var linkedId in linkedDocumentIds)
            {
                var linkedDoc = document.Project.Solution.GetRequiredDocument(linkedId);
                var linkedSemanticModel = await linkedDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                linkedSemanticModels.Add((linkedId, linkedSemanticModel));
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var commonContext = new CommonQuickInfoContext(
                document.Project.Solution.Workspace, document.Id, semanticModel, linkedSemanticModels.ToImmutableAndClear(), position, cancellationToken);

            return await GetQuickInfoAsync(commonContext).ConfigureAwait(false);
        }

        public async Task<QuickInfoItem?> GetQuickInfoAsync(CommonQuickInfoContext context)
        {
            var tree = context.SemanticModel.SyntaxTree;
            var token = await tree.GetTouchingTokenAsync(context.Position, context.CancellationToken, findInsideTrivia: true).ConfigureAwait(false);

            var info = await GetQuickInfoAsync(context, token).ConfigureAwait(false);

            if (info == null && ShouldCheckPreviousToken(token))
            {
                var previousToken = token.GetPreviousToken();
                info = await GetQuickInfoAsync(context, previousToken).ConfigureAwait(false);
            }

            return info;
        }

        protected virtual bool ShouldCheckPreviousToken(SyntaxToken token)
            => true;

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
}
