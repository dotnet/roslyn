// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class PropertyDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<PropertyDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            PropertyDeclarationSyntax propertyDeclaration,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
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

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                propertyDeclaration,
                propertyDeclaration.Identifier,
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
