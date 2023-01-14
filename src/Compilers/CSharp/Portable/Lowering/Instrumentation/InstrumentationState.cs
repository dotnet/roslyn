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
    /// Used to temporary suspend instrumentation, for example when lowering expression tree.
    /// </summary>
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// Current instrumenter.
    /// </summary>
    public Instrumenter Instrumenter { get; set; } = Instrumenter.NoOp;

    public void RemoveDynamicAnalysisInstrumentation()
        => Instrumenter = RemoveDynamicAnalysisInjector(Instrumenter);

    private static Instrumenter RemoveDynamicAnalysisInjector(Instrumenter instrumenter)
        => instrumenter switch
        {
            DynamicAnalysisInjector { Previous: var previous } => RemoveDynamicAnalysisInjector(previous),
            CompoundInstrumenter compound => compound.WithPrevious(RemoveDynamicAnalysisInjector(compound.Previous)),
            _ => instrumenter,
        };
}
