// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class PropertyDeclarationOutliner : AbstractSyntaxNodeOutliner<PropertyDeclarationSyntax>
    {
        protected override void CollectOutliningSpans(
            PropertyDeclarationSyntax propertyDeclaration,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(propertyDeclaration, spans);

            // fault tolerance
            if (propertyDeclaration.AccessorList == null ||
                propertyDeclaration.AccessorList.OpenBraceToken.IsMissing ||
                propertyDeclaration.AccessorList.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpOutliningHelpers.CreateRegion(
                propertyDeclaration,
                propertyDeclaration.Identifier,
                autoCollapse: true));
        }
    }
}
