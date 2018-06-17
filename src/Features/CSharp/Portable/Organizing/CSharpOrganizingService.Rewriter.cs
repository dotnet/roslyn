// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Organizing.Organizers;

namespace Microsoft.CodeAnalysis.CSharp.Organizing
{
    internal partial class CSharpOrganizingService
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly Func<SyntaxNode, IEnumerable<ISyntaxOrganizer>> _nodeToOrganizersGetter;
            private readonly SemanticModel _semanticModel;
            private readonly OptionSet _optionSet;
            private readonly CancellationToken _cancellationToken;

            public Rewriter(CSharpOrganizingService treeOrganizer, IEnumerable<ISyntaxOrganizer> organizers, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
            {
                _nodeToOrganizersGetter = treeOrganizer.GetNodeToOrganizers(organizers.ToList());
                _semanticModel = semanticModel;
                _optionSet = optionSet;
                _cancellationToken = cancellationToken;
            }

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
                    node = organizer.OrganizeNode(_semanticModel, _optionSet, node, _cancellationToken);
                }

                return node;
            }
        }
    }
}
