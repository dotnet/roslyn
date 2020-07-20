// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
