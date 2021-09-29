// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class CompilationUnitCompletedEvent : CompilationEvent
    {
        public CompilationUnitCompletedEvent(Compilation compilation, SyntaxTree compilationUnit, TextSpan? filterSpan = null)
            : base(compilation)
        {
            this.CompilationUnit = compilationUnit;
            this.FilterSpan = filterSpan;
        }

        public SyntaxTree CompilationUnit { get; }

        /// <summary>
        /// Optional filter span for a synthesized CompilationUnitCompletedEvent generated for span-based semantic diagnostic computation.
        /// See https://github.com/dotnet/roslyn/issues/56843 for details.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        public override string ToString()
        {
            var str = "CompilationUnitCompletedEvent(" + CompilationUnit.FilePath + ")";
            if (FilterSpan.HasValue)
            {
                str += FilterSpan.Value.ToString();
            }

            return str;
        }
    }
}
