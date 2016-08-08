// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class EventDeclarationOutliner : AbstractSyntaxNodeOutliner<EventDeclarationSyntax>
    {
        protected override void CollectOutliningSpans(
            EventDeclarationSyntax eventDeclaration,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(eventDeclaration, spans);

            // fault tolerance
            if (eventDeclaration.AccessorList.IsMissing ||
                eventDeclaration.AccessorList.OpenBraceToken.IsMissing ||
                eventDeclaration.AccessorList.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpOutliningHelpers.CreateRegion(
                eventDeclaration,
                eventDeclaration.Identifier,
                autoCollapse: true));
        }
    }
}
