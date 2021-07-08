// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct GeneratorInfo
    {
        internal SyntaxContextReceiverCreator? SyntaxContextReceiverCreator { get; }

        internal Action<IncrementalGeneratorPostInitializationContext>? PostInitCallback { get; }

        internal Action<IncrementalGeneratorInitializationContext>? PipelineCallback { get; }

        internal bool Initialized { get; }

        internal GeneratorInfo(SyntaxContextReceiverCreator? receiverCreator, Action<IncrementalGeneratorPostInitializationContext>? postInitCallback, Action<IncrementalGeneratorInitializationContext>? pipelineCallback)
        {
            SyntaxContextReceiverCreator = receiverCreator;
            PostInitCallback = postInitCallback;
            PipelineCallback = pipelineCallback;
            Initialized = true;
        }

        internal class Builder
        {
            internal SyntaxContextReceiverCreator? SyntaxContextReceiverCreator { get; set; }

            internal Action<IncrementalGeneratorPostInitializationContext>? PostInitCallback { get; set; }

            internal Action<IncrementalGeneratorInitializationContext>? PipelineCallback { get; set; }

            public GeneratorInfo ToImmutable() => new GeneratorInfo(SyntaxContextReceiverCreator, PostInitCallback, PipelineCallback);
        }
    }
}
