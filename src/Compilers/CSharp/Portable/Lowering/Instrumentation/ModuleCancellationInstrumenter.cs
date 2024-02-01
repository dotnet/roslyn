// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Implements instrumentation for <see cref="CodeAnalysis.Emit.InstrumentationKind.ModuleCancellation"/>.
/// </summary>
internal sealed class ModuleCancellationInstrumenter(
        MethodSymbol throwMethod,
        SyntheticBoundNodeFactory factory,
        Instrumenter previous)
        : CompoundInstrumenter(previous)
{
    protected override CompoundInstrumenter WithPreviousImpl(Instrumenter previous)
        => new ModuleCancellationInstrumenter(
            throwMethod,
            factory,
            previous);

    public static bool TryCreate(
        MethodSymbol method,
        SyntheticBoundNodeFactory factory,
        Instrumenter previous,
        [NotNullWhen(true)] out ModuleCancellationInstrumenter? instrumenter)
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

        var throwMethod = factory.WellKnownMethod(WellKnownMember.System_Threading_CancellationToken__ThrowIfCancellationRequested, isOptional: true);
        if (throwMethod is null)
        {
            return false;
        }

        instrumenter = new ModuleCancellationInstrumenter(throwMethod, factory, previous);
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

        Debug.Assert(factory.TopLevelMethod is not null);
        Debug.Assert(factory.CurrentFunction is not null);

        // static constructors can only be invoked once, so there is no need to probe:
        if (isMethodBody && factory.TopLevelMethod.MethodKind == MethodKind.StaticConstructor)
        {
            return;
        }

        instrumentation = factory.Instrumentation(
            instrumentation,
            prologue: factory.ExpressionStatement(factory.ThrowIfModuleCancellationRequested()));
    }

    private BoundExpression InstrumentExpression(BoundExpression expression)
        => factory.Sequence([], [factory.ThrowIfModuleCancellationRequested()], expression);

    private BoundStatement InstrumentStatement(BoundStatement statement)
        => factory.StatementList(factory.ExpressionStatement(factory.ThrowIfModuleCancellationRequested()), statement);

    public override BoundExpression InstrumentWhileStatementCondition(BoundWhileStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        => InstrumentExpression(base.InstrumentWhileStatementCondition(original, rewrittenCondition, factory));

    public override BoundExpression InstrumentDoStatementCondition(BoundDoStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        => InstrumentExpression(base.InstrumentDoStatementCondition(original, rewrittenCondition, factory));

    public override BoundExpression InstrumentForStatementCondition(BoundForStatement original, BoundExpression rewrittenCondition, SyntheticBoundNodeFactory factory)
        => InstrumentExpression(base.InstrumentForStatementCondition(original, rewrittenCondition, factory));

    public override BoundStatement InstrumentForStatementConditionalGotoStartOrBreak(BoundForStatement original, BoundStatement branchBack)
        => InstrumentStatement(base.InstrumentForStatementConditionalGotoStartOrBreak(original, branchBack));

    public override BoundStatement InstrumentForEachStatementConditionalGotoStart(BoundForEachStatement original, BoundStatement branchBack)
        => InstrumentStatement(base.InstrumentForEachStatementConditionalGotoStart(original, branchBack));

    public override BoundStatement InstrumentGotoStatement(BoundGotoStatement original, BoundStatement rewritten)
        => InstrumentStatement(base.InstrumentGotoStatement(original, rewritten));

    public override void InterceptCallAndAdjustArguments(
        ref MethodSymbol method,
        ref BoundExpression? receiver,
        ref ImmutableArray<BoundExpression> arguments,
        ref ImmutableArray<RefKind> argumentRefKindsOpt,
        bool invokedAsExtensionMethod,
        SimpleNameSyntax? nameSyntax)
    {
        Previous.InterceptCallAndAdjustArguments(ref method, ref receiver, ref arguments, ref argumentRefKindsOpt, invokedAsExtensionMethod, nameSyntax);

        if (arguments is [.., { Type: { } lastArgumentType } lastArgument] &&
            (argumentRefKindsOpt.IsDefault || argumentRefKindsOpt is [.., RefKind.None]) &&
            lastArgumentType.Equals(throwMethod.ContainingType, TypeCompareKind.ConsiderEverything))
        {
            // The last argument is a CancellationToken. Replace it with the module-level token.
            // Keep the previous expression so that side-effects are preserved.
            arguments = [.. arguments[0..^1], factory.Sequence([lastArgument], factory.ModuleCancellationToken())];
        }
        else if (FindOverloadWithCancellationToken(method) is { } cancellableOverload)
        {
            // The method being invoked does not have a CancellationToken as the last parameter, but there is an overload that does.
            // Invoke the other overload instead and pass in module-level token. 
            method = cancellableOverload;
            arguments = [.. arguments, factory.ModuleCancellationToken()];
            argumentRefKindsOpt = argumentRefKindsOpt.IsDefault ? default : [.. argumentRefKindsOpt, RefKind.None];
        }
    }

    /// <summary>
    /// Find an overload whose last parameter is <see cref="CancellationToken"/> and
    /// the parameter types and ref kinds of all other parameters match those of <paramref name="method"/>.
    /// </summary>
    private MethodSymbol? FindOverloadWithCancellationToken(MethodSymbol method)
    {
        // It's unlikely that real-world APIs have overloads that differ in dynamic,
        // but if they do avoid selecting them since their intended use might differ.
        var typeComparisonKind = TypeCompareKind.CLRSignatureCompareOptions & ~TypeCompareKind.IgnoreDynamic;

        foreach (var member in method.ContainingType.GetMembers(method.Name))
        {
            if (member is MethodSymbol { Parameters: [.., { RefKind: RefKind.None, Type: { } lastParamType }] parametersWithCancellationToken } overload &&
                overload.Arity == method.Arity &&
                method.Parameters.Length == parametersWithCancellationToken.Length - 1 &&
                lastParamType.Equals(throwMethod.ContainingType, TypeCompareKind.ConsiderEverything) &&
                MemberSignatureComparer.HaveSameParameterTypes(
                    method.Parameters.AsSpan(),
                    typeMap1: null,
                    parametersWithCancellationToken.AsSpan(0, method.Parameters.Length),
                    method.TypeSubstitution,
                    MemberSignatureComparer.RefKindCompareMode.ConsiderDifferences,
                    typeComparisonKind) &&
                MemberSignatureComparer.HaveSameReturnTypes(
                    method,
                    typeMap1: null,
                    overload,
                    method.TypeSubstitution,
                    typeComparisonKind) &&
                MemberSignatureComparer.HaveSameConstraints(
                    method.TypeParameters,
                    typeMap1: null,
                    overload.TypeParameters,
                    method.TypeSubstitution))
            {
                return (overload.Arity > 0) ? overload.Construct(method.TypeArgumentsWithAnnotations) : overload;
            }
        }

        return null;
    }
}
