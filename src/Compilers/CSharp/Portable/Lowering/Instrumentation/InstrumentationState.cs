// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Manages instrumentation state.
/// </summary>
internal sealed class InstrumentationState
{
    /// <summary>
    /// Used to temporarily suspend instrumentation, for example when lowering expression tree.
    /// </summary>
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// Current instrumenter.
    /// </summary>
    public Instrumenter Instrumenter { get; set; } = Instrumenter.NoOp;

    public void RemoveCodeCoverageInstrumenter()
    {
        Instrumenter = recurse(Instrumenter);

        static Instrumenter recurse(Instrumenter instrumenter)
            => instrumenter switch
            {
                CodeCoverageInstrumenter { Previous: var previous } => recurse(previous),
                CompoundInstrumenter compound => compound.WithPrevious(recurse(compound.Previous)),
                _ => instrumenter,
            };
    }
}
