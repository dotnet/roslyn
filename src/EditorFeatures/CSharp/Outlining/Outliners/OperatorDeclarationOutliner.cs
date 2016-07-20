// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class OperatorDeclarationOutliner : AbstractSyntaxNodeOutliner<OperatorDeclarationSyntax>
    {
        protected override void CollectOutliningSpans(
            OperatorDeclarationSyntax operatorDeclaration,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(operatorDeclaration, spans);

            // fault tolerance
            if (operatorDeclaration.Body == null ||
                operatorDeclaration.Body.OpenBraceToken.IsMissing ||
                operatorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpOutliningHelpers.CreateRegion(
                operatorDeclaration,
                operatorDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                autoCollapse: true));
        }
    }
}
