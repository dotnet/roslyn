using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    // PROTOTYPE(nullable-api): Document
    public enum NullableAnnotation : byte
    {
        Default = 0,
        Unknown,      // No information. Think oblivious.
        NotAnnotated, // Type is not annotated - string, int, T (including the case when T is unconstrained).
        Annotated,    // Type is annotated - string?, T? where T : class; and for int?, T? where T : struct.
        NotNullable,  // Explicitly set by flow analysis
        Nullable,     // Explicitly set by flow analysis
    }

    /// <summary>
    /// The nullable state of an rvalue computed in NullableWalker.
    /// When in doubt we conservatively use <see cref="NotNull"/>
    /// to minimize diagnostics.
    /// </summary>
    // PROTOTYPE(nullable-api): Document
    public enum NullableFlowState : byte
    {
        Default = 0,
        NotNull,
        MaybeNull
    }
}
