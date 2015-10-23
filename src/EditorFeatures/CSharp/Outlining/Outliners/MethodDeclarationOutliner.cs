// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class MethodDeclarationOutliner : AbstractSyntaxNodeOutliner<MethodDeclarationSyntax>
    {
        protected override void CollectOutliningSpans(
            MethodDeclarationSyntax methodDeclaration,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(methodDeclaration, spans);

            // fault tolerance
            if (methodDeclaration.Body == null ||
                methodDeclaration.Body.OpenBraceToken.IsMissing ||
                methodDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpOutliningHelpers.CreateRegion(
                methodDeclaration,
                methodDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                autoCollapse: true));
        }
    }
}
