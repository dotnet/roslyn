// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal sealed class GeneratorSyntaxWalker
    {
        private readonly ISyntaxContextReceiver _syntaxReceiver;
        private readonly ISyntaxHelper _syntaxHelper;
        private Lazy<SemanticModel>? _semanticModel;

        internal GeneratorSyntaxWalker(
            ISyntaxContextReceiver syntaxReceiver,
            ISyntaxHelper syntaxHelper)
        {
            _syntaxReceiver = syntaxReceiver;
            _syntaxHelper = syntaxHelper;
        }

        public void VisitWithModel(Lazy<SemanticModel>? model, SyntaxNode node)
        {
            Debug.Assert(_semanticModel is null
                         && model is not null
                         && model.Value.SyntaxTree == node.SyntaxTree);

            _semanticModel = model;
            Visit(node);
            _semanticModel = null;
        }

        public void Visit(SyntaxNode node)
        {
            Debug.Assert(_semanticModel is object);
            foreach (var child in node.DescendantNodesAndSelf())
            {
                Debug.Assert(_semanticModel.Value.SyntaxTree == child.SyntaxTree);
                _syntaxReceiver.OnVisitSyntaxNode(new GeneratorSyntaxContext(child, _semanticModel, _syntaxHelper));
            }
        }
    }
}
