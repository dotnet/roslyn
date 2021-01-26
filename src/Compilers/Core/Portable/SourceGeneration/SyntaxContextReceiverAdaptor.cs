// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Wraps an <see cref="ISyntaxReceiver"/> in an <see cref="ISyntaxContextReceiver"/>
    /// </summary>
    internal sealed class SyntaxContextReceiverAdaptor : ISyntaxContextReceiver
    {
        public SyntaxContextReceiverAdaptor(ISyntaxReceiver? receiver)
        {
            Receiver = receiver;
        }

        public ISyntaxReceiver? Receiver { get; }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context) => Receiver?.OnVisitSyntaxNode(context.Node);
    }
}
