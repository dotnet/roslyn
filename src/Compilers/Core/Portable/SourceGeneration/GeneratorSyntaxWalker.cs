// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal sealed class GeneratorSyntaxWalker : SyntaxWalker
    {
        private readonly ISyntaxReceiverBase _syntaxReceiver;

        private SemanticModel? _semanticModel;

        internal GeneratorSyntaxWalker(ISyntaxReceiverBase syntaxReceiver)
        {
            _syntaxReceiver = syntaxReceiver;
        }

        public void VisitWithModel(SemanticModel model, SyntaxNode node)
        {
            Debug.Assert(_semanticModel is null);
            _semanticModel = model;
            Visit(node);
            _semanticModel = null;
        }

        public override void Visit(SyntaxNode node)
        {
            switch (_syntaxReceiver)
            {
                case ISyntaxReceiver syntaxReceiver:
                    syntaxReceiver.OnVisitSyntaxNode(node);
                    break;
                case ISyntaxReceiverWithContext syntaxReceiverWithContext:
                    Debug.Assert(_semanticModel is object && _semanticModel.SyntaxTree == node.SyntaxTree);
                    syntaxReceiverWithContext.OnVisitSyntaxNode(new GeneratorSyntaxReceiverContext(node, _semanticModel));
                    break;
            }
            base.Visit(node);
        }
    }
}
