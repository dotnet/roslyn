// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class CompilationUnitCompletedEvent : CompilationEvent
    {
        public CompilationUnitCompletedEvent(Compilation compilation, SyntaxTree compilationUnit) : base(compilation)
        {
            this.CompilationUnit = compilationUnit;
        }

        public SyntaxTree CompilationUnit { get; }

        public override string ToString()
        {
            return "CompilationUnitCompletedEvent(" + CompilationUnit.FilePath + ")";
        }
    }
}
