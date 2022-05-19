// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.SourceGeneration;

namespace Microsoft.CodeAnalysis
{
    internal sealed class GeneratorSyntaxWalker : SyntaxWalker
    {
        private readonly ISyntaxContextReceiver _syntaxReceiver;
        private readonly ISyntaxHelper _syntaxHelper;
        private SemanticModel? _semanticModel;

        internal GeneratorSyntaxWalker(
            ISyntaxContextReceiver syntaxReceiver,
            ISyntaxHelper syntaxHelper)
        {
            _syntaxReceiver = syntaxReceiver;
            _syntaxHelper = syntaxHelper;
        }

        public void VisitWithModel(SemanticModel model, SyntaxNode node)
        {
            Debug.Assert(_semanticModel is null
                         && model is not null
                         && model.SyntaxTree == node.SyntaxTree);

            _semanticModel = model;
            Visit(node);
            _semanticModel = null;
        }

        public override void Visit(SyntaxNode node)
        {
            Debug.Assert(_semanticModel is object && _semanticModel.SyntaxTree == node.SyntaxTree);
            _syntaxReceiver.OnVisitSyntaxNode(new GeneratorSyntaxContext(node, _semanticModel, _syntaxHelper));
            base.Visit(node);
        }
    }
}
