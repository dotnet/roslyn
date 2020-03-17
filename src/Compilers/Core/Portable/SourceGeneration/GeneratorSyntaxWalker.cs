// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    internal class GeneratorSyntaxWalker : SyntaxWalker
    {
        private readonly ImmutableArray<ISyntaxReceiver> _syntaxReceivers;

        public GeneratorSyntaxWalker(ImmutableArray<ISyntaxReceiver> syntaxReceivers)
        {
            _syntaxReceivers = syntaxReceivers;
        }

        public override void Visit(SyntaxNode node)
        {
            foreach (var recevier in _syntaxReceivers)
            {
                recevier.OnVisitSyntaxNode(node);
            }
            base.Visit(node);
        }
    }

}
