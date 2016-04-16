// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
