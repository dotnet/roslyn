// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The first event placed into a compilation's event queue.
    /// </summary>
    internal sealed class CompilationStartedEvent : CompilationEvent
    {
        public ImmutableArray<AdditionalText> AdditionalFiles { get; }

        private CompilationStartedEvent(Compilation compilation, ImmutableArray<AdditionalText> additionalFiles)
            : base(compilation)
        {
            AdditionalFiles = additionalFiles;
        }

        public CompilationStartedEvent(Compilation compilation)
            : this(compilation, ImmutableArray<AdditionalText>.Empty)
        {
        }

        public override string ToString()
        {
            return "CompilationStartedEvent";
        }

        public CompilationStartedEvent WithAdditionalFiles(ImmutableArray<AdditionalText> additionalFiles)
            => new CompilationStartedEvent(Compilation, additionalFiles);
    }
}
