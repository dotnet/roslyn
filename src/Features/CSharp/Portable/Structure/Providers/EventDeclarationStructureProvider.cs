// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class EventDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<EventDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            EventDeclarationSyntax eventDeclaration,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(eventDeclaration, spans);

            // fault tolerance
            if (eventDeclaration.AccessorList.IsMissing ||
                eventDeclaration.AccessorList.OpenBraceToken.IsMissing ||
                eventDeclaration.AccessorList.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                eventDeclaration,
                eventDeclaration.Identifier,
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}