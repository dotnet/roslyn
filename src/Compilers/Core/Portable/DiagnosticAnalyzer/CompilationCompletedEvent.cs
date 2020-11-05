// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The last event placed into a compilation's event queue.
    /// </summary>
    internal sealed class CompilationCompletedEvent : CompilationEvent
    {
        public CompilationCompletedEvent(Compilation compilation) : base(compilation) { }
        public override string ToString()
        {
            return "CompilationCompletedEvent";
        }
    }
}
