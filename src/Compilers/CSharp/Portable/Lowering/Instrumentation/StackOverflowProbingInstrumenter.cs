// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Implements instrumentation for <see cref="CodeAnalysis.Emit.InstrumentationKind.StackOverflowProbing"/>.
/// </summary>
internal sealed class StackOverflowProbingInstrumenter(
        MethodSymbol ensureStackMethod,
        SyntheticBoundNodeFactory factory,
        Instrumenter previous)
        : CompoundInstrumenter(previous)
{
    private readonly MethodSymbol _ensureStackMethod = ensureStackMethod;
    private readonly SyntheticBoundNodeFactory _factory = factory;

    protected override CompoundInstrumenter WithPreviousImpl(Instrumenter previous)
        => new StackOverflowProbingInstrumenter(
            _ensureStackMethod,
            _factory,
            previous);

    public static bool TryCreate(
        MethodSymbol method,
        SyntheticBoundNodeFactory factory,
        Instrumenter previous,
        [NotNullWhen(true)] out StackOverflowProbingInstrumenter? instrumenter)
    {
        instrumenter = null;

        // Do not instrument implicitly-declared methods or methods without bodies, except for constructors.
        // Instrument implicit constructors in order to prevent stack overflow caused by member initializers.
        if (method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor) &&
            (method is { IsImplicitlyDeclared: true } ||
             method is SourceMemberMethodSymbol { Bodies: { arrowBody: null, blockBody: null } } and not SynthesizedSimpleProgramEntryPointSymbol))
        {
            return false;
        }

        var ensureStackMethod = factory.WellKnownMethod(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__EnsureSufficientExecutionStack, isOptional: true);
        if (ensureStackMethod is null)
        {
            return false;
        }

        instrumenter = new StackOverflowProbingInstrumenter(ensureStackMethod, factory, previous);
        return true;
    }

    public override void InstrumentBlock(BoundBlock original, LocalRewriter rewriter, ref TemporaryArray<LocalSymbol> additionalLocals, out BoundStatement? prologue, out BoundStatement? epilogue, out BoundBlockInstrumentation? instrumentation)
    {
        base.InstrumentBlock(original, rewriter, ref additionalLocals, out prologue, out epilogue, out instrumentation);

        var isMethodBody = rewriter.CurrentMethodBody == original;
        var isLambdaBody = rewriter.CurrentLambdaBody == original;

        // Don't instrument blocks that are not a method or lambda body
        if (!isMethodBody && !isLambdaBody)
        {
            return;
        }

        Debug.Assert(_factory.TopLevelMethod is not null);
        Debug.Assert(_factory.CurrentFunction is not null);

        // static constructors can only be invoked once, so there is no need to probe:
        if (isMethodBody && _factory.TopLevelMethod.MethodKind == MethodKind.StaticConstructor)
        {
            return;
        }

        instrumentation = _factory.CombineInstrumentation(
            instrumentation,
            prologue: _factory.ExpressionStatement(_factory.Call(receiver: null, _ensureStackMethod)));
    }
}
