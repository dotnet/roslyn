// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract class CompilationEvent
    {
        internal CompilationEvent(Compilation compilation)
        {
            this.Compilation = compilation;
        }

        public Compilation Compilation { get; }

        /// <summary>
        /// Flush any cached data in this <see cref="CompilationEvent"/> to minimize space usage (at the possible expense of time later).
        /// The principal effect of this is to free cached information on events that have a <see cref="SemanticModel"/>.
        /// </summary>
        public virtual void FlushCache() { }
    }
}
