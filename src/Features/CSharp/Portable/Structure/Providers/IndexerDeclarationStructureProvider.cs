// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class IndexerDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<IndexerDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            IndexerDeclarationSyntax indexerDeclaration,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(indexerDeclaration, spans);

            // fault tolerance
            if (indexerDeclaration.AccessorList == null ||
                indexerDeclaration.AccessorList.IsMissing ||
                indexerDeclaration.AccessorList.OpenBraceToken.IsMissing ||
                indexerDeclaration.AccessorList.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                indexerDeclaration,
                indexerDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
