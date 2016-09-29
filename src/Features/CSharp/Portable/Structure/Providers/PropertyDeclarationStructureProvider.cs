// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class PropertyDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<PropertyDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            PropertyDeclarationSyntax propertyDeclaration,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(propertyDeclaration, spans);

            // fault tolerance
            if (propertyDeclaration.AccessorList == null ||
                propertyDeclaration.AccessorList.OpenBraceToken.IsMissing ||
                propertyDeclaration.AccessorList.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpStructureHelpers.CreateBlockSpan(
                propertyDeclaration,
                propertyDeclaration.Identifier,
                autoCollapse: true,
                type: BlockTypes.Property,
                isCollapsible: true));
        }
    }
}
