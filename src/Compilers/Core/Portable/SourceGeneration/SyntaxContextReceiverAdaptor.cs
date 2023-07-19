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
        private SyntaxContextReceiverAdaptor(ISyntaxReceiver receiver)
        {
            Receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        public ISyntaxReceiver Receiver { get; }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context) => Receiver.OnVisitSyntaxNode(context.Node);

        public static SyntaxContextReceiverCreator Create(SyntaxReceiverCreator creator) => () =>
        {
            var rx = creator();
            if (rx is object)
            {
                return new SyntaxContextReceiverAdaptor(rx);
            }
            // in the case that the creator function returns null, we'll also return a null adaptor
            return null;
        };
    }
}
