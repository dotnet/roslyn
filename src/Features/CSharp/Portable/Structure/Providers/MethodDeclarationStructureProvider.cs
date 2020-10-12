﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class MethodDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<MethodDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            MethodDeclarationSyntax methodDeclaration,
            ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(methodDeclaration, spans, isMetadataAsSource);

            // fault tolerance
            if (methodDeclaration.Body == null ||
                methodDeclaration.Body.OpenBraceToken.IsMissing ||
                methodDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            SyntaxNodeOrToken current = methodDeclaration;
            var nextSibling = current.GetNextSibling();

            // Check IsNode to compress blank lines after this node if it is the last child of the parent.
            //
            // Whitespace between methods is collapsed in Metadata as Source.
            var compressEmptyLines = isMetadataAsSource
                && (!nextSibling.IsNode || nextSibling.IsKind(SyntaxKind.MethodDeclaration));

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                methodDeclaration,
                methodDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                compressEmptyLines: compressEmptyLines,
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
