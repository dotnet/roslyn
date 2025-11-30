// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if DEBUG

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp;

internal enum PipelinePhase
{
    InitialBinding,
    LocalRewriting,
    ClosureConversion,
    StateMachineRewriting,
    None
}

/// <summary>
/// Note: do not use a static/singleton instance of this type, as it holds state.
/// </summary>
internal sealed partial class PipelinePhaseValidator : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
{
    private readonly PipelinePhase _completedPhase;

    /// <summary>
    /// Asserts that no unexpected nodes survived a given phase of rewriting.
    /// </summary>
    [Conditional("DEBUG")]
    public static void Assert(BoundNode node, PipelinePhase completedPhase)
    {
        try
        {
            new PipelinePhaseValidator(completedPhase).Visit(node);
        }
        catch (InsufficientExecutionStackException)
        {
            // Intentionally ignored to let the overflow get caught in a more crucial visitor
        }
    }

    private PipelinePhaseValidator(PipelinePhase completedPhase)
    {
        _completedPhase = completedPhase;
    }

    public override BoundNode? Visit(BoundNode? node)
    {
        if (node is null || node.HasErrors)
            return null;

        if (_completedPhase >= DoesNotSurvive(node.Kind))
            Debug.Assert(false, $"Bound nodes of kind {node.Kind} should not survive past {DoesNotSurvive(node.Kind)}");

        return base.Visit(node);
    }
}
#endif

