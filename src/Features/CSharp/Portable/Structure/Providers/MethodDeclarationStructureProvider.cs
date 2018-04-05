// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(methodDeclaration, spans);

            // fault tolerance
            if (methodDeclaration.Body == null ||
                methodDeclaration.Body.OpenBraceToken.IsMissing ||
                methodDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                methodDeclaration,
                methodDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
