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
    internal class DestructorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<DestructorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            DestructorDeclarationSyntax destructorDeclaration,
            ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(destructorDeclaration, spans, isMetadataAsSource);

            // fault tolerance
            if (destructorDeclaration.Body == null ||
                destructorDeclaration.Body.OpenBraceToken.IsMissing ||
                destructorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                destructorDeclaration,
                destructorDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                compressEmptyLines: false,
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
