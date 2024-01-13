// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing
{
    internal partial class CSharpOrganizingService
    {
        private class Rewriter(CSharpOrganizingService treeOrganizer, IEnumerable<ISyntaxOrganizer> organizers, SemanticModel semanticModel, CancellationToken cancellationToken) : CSharpSyntaxRewriter
        {
            private readonly Func<SyntaxNode, IEnumerable<ISyntaxOrganizer>> _nodeToOrganizersGetter = treeOrganizer.GetNodeToOrganizers(organizers.ToList());
            private readonly SemanticModel _semanticModel = semanticModel;
            private readonly CancellationToken _cancellationToken = cancellationToken;

            public override SyntaxNode DefaultVisit(SyntaxNode node)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                return node;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (node == null)
                {
                    return null;
                }

                // First, recurse into our children, updating them.
                node = base.Visit(node);

                // Now, try to update this new node itself.
                var organizers = _nodeToOrganizersGetter(node);
                foreach (var organizer in organizers)
                {
                    node = organizer.OrganizeNode(_semanticModel, node, _cancellationToken);
                }

                return node;
            }
        }
    }
}
