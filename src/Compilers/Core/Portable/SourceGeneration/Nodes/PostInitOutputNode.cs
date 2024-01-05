// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class PostInitOutputNode : IIncrementalGeneratorOutputNode
    {
        private readonly Action<IncrementalGeneratorPostInitializationContext, CancellationToken> _callback;

        public PostInitOutputNode(Action<IncrementalGeneratorPostInitializationContext, CancellationToken> callback)
        {
            _callback = callback;
        }

        public IncrementalGeneratorOutputKind Kind => IncrementalGeneratorOutputKind.PostInit;

        public void AppendOutputs(IncrementalExecutionContext context, CancellationToken cancellationToken)
        {
            _callback(new IncrementalGeneratorPostInitializationContext(context.Sources, cancellationToken), cancellationToken);
        }
    }
}
