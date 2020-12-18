// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal sealed class GeneratorSyntaxWalker : SyntaxWalker
    {
        private readonly ISyntaxReceiverBase _syntaxReceiver;

        internal GeneratorSyntaxWalker(ISyntaxReceiverBase syntaxReceiver)
        {
            _syntaxReceiver = syntaxReceiver;
        }

        public override void Visit(SyntaxNode node)
        {
            switch (_syntaxReceiver)
            {
                case ISyntaxReceiver syntaxReceiver:
                    syntaxReceiver.OnVisitSyntaxNode(node);
                    break;
            }
            base.Visit(node);
        }
    }
}
