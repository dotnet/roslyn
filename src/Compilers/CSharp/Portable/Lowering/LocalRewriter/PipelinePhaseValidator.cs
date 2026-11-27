// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp;

#if DEBUG
internal enum PipelinePhase
{
    InitialBinding,
    LocalRewriting,
    Spilling,
    ClosureConversion,
    StateMachineRewriting,
    Emit
}
#endif

internal sealed partial class PipelinePhaseValidator
#if DEBUG
     : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
#endif
{

#if DEBUG
    private readonly PipelinePhase _completedPhase;
#endif

#if DEBUG
    private PipelinePhaseValidator(PipelinePhase completedPhase)
    {
        _completedPhase = completedPhase;
    }
#endif

    [Conditional("DEBUG")]
    public static void AssertAfterInitialBinding(BoundNode node)
    {
#if DEBUG
        Assert(node, PipelinePhase.InitialBinding);
#endif
    }

    [Conditional("DEBUG")]
    public static void AssertAfterLocalRewriting(BoundNode node)
    {
#if DEBUG
        Assert(node, PipelinePhase.LocalRewriting);
#endif
    }

    [Conditional("DEBUG")]
    public static void AssertAfterSpilling(BoundNode node)
    {
#if DEBUG
        Assert(node, PipelinePhase.Spilling);
#endif
    }

    [Conditional("DEBUG")]
    public static void AssertAfterClosureConversion(BoundNode node)
    {
#if DEBUG
        Assert(node, PipelinePhase.ClosureConversion);
#endif
    }

    [Conditional("DEBUG")]
    public static void AssertAfterStateMachineRewriting(BoundNode node)
    {
#if DEBUG
        Assert(node, PipelinePhase.StateMachineRewriting);
#endif
    }

#if DEBUG
    /// <summary>
    /// Asserts that no unexpected nodes survived a given phase of rewriting.
    /// </summary>
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

    public override BoundNode? Visit(BoundNode? node)
    {
        if (node is null || node.HasErrors)
            return null;

        if (_completedPhase >= DoesNotSurvive(node.Kind))
            Debug.Assert(false, $"Bound nodes of kind {node.Kind} should not survive past {DoesNotSurvive(node.Kind)}");

        return base.Visit(node);
    }
#endif
}

