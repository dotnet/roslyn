// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class EventFieldDeclarationOutliner : AbstractSyntaxNodeOutliner<EventFieldDeclarationSyntax>
    {
        protected override void CollectOutliningSpans(
            EventFieldDeclarationSyntax eventFieldDeclaration,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(eventFieldDeclaration, spans);
        }
    }
}
