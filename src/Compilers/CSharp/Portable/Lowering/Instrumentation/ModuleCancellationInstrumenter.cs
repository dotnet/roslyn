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
/// <remarks>
/// - Adds a static writable field of type <see cref="CancellationToken"/> to PrivateImplementationDetails. The host can set this token via Reflection before executing the compiled code.
/// - Inserts calls to <see cref="CancellationToken.ThrowIfCancellationRequested"/> on the host token into each method, loop or goto. 
/// - Replaces any tokens passed as arguments with the host token.
/// - Replaces calls to methods that do not take <see cref="CancellationToken"/> as the last parameter with matching overloads that do and passes it the host token.
/// </remarks>
internal sealed class ModuleCancellationInstrumenter(
    MethodSymbol throwMethod,
    SyntheticBoundNodeFactory factory,
    Instrumenter previous)
    : CompoundInstrumenter(previous)
{
    private readonly MethodSymbol _throwMethod = throwMethod;
    private readonly SyntheticBoundNodeFactory _factory = factory;

    protected override CompoundInstrumenter WithPreviousImpl(Instrumenter previous)
        => new ModuleCancellationInstrumenter(
            _throwMethod,
            _factory,
            previous);

    public static bool TryCreate(
        MethodSymbol method,
        SyntheticBoundNodeFactory factory,
        Instrumenter previous,
        [NotNullWhen(true)] out ModuleCancellationInstrumenter? instrumenter)
    {
        instrumenter = null;

        // Do not instrument implicitly-declared methods or methods without bodies, except for constructors.
        // Instrument implicit constructors in order to cancel execution of member initializers.
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

        Debug.Assert(_factory.TopLevelMethod is not null);
        Debug.Assert(_factory.CurrentFunction is not null);

        // static constructors can only be invoked once, so there is no need to probe:
        if (isMethodBody && _factory.TopLevelMethod.MethodKind == MethodKind.StaticConstructor)
        {
            return;
        }

        instrumentation = _factory.CombineInstrumentation(
            instrumentation,
            prologue: _factory.ExpressionStatement(_factory.ThrowIfModuleCancellationRequested()));
    }

    private BoundExpression InstrumentExpression(BoundExpression expression)
        => _factory.Sequence([], [_factory.ThrowIfModuleCancellationRequested()], expression);

    private BoundStatement InstrumentStatement(BoundStatement statement)
        => _factory.StatementList(_factory.ExpressionStatement(_factory.ThrowIfModuleCancellationRequested()), statement);

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
        ref ImmutableArray<RefKind> argumentRefKindsOpt)
    {
        Previous.InterceptCallAndAdjustArguments(ref method, ref receiver, ref arguments, ref argumentRefKindsOpt);

        // If the target method is defined within this module it is already being instrumented to be cancellable.
        // However, if we are calling Invoke method of a delegate or a virtual/interface method we can't determine whether
        // or not the target is in the current module. Hence we replace the cancellation token for all calls.

        if (arguments is [.., { Type: { } lastArgumentType } lastArgument] &&
            (argumentRefKindsOpt.IsDefault || argumentRefKindsOpt is [.., RefKind.None]) &&
            lastArgumentType.Equals(_throwMethod.ContainingType, TypeCompareKind.ConsiderEverything))
        {
            // The last argument is a CancellationToken. Replace it with the module-level token.
            // Keep the previous expression so that side-effects are preserved.

            arguments = [.. arguments[0..^1], _factory.Sequence([lastArgument], _factory.ModuleCancellationToken())];
        }
        else if (FindOverloadWithCancellationToken(method) is { } cancellableOverload)
        {
            // The method being invoked does not have a CancellationToken as the last parameter, but there is an overload that does.
            // Invoke the other overload instead and pass in module-level token. 
            method = cancellableOverload;
            arguments = [.. arguments, _factory.ModuleCancellationToken()];
            argumentRefKindsOpt = argumentRefKindsOpt.IsDefault ? default : [.. argumentRefKindsOpt, RefKind.None];
        }
    }

    /// <summary>
    /// Find an overload whose last parameter is <see cref="CancellationToken"/> and
    /// the parameter types and ref kinds of all other parameters match those of <paramref name="method"/>.
    /// </summary>
    private MethodSymbol? FindOverloadWithCancellationToken(MethodSymbol method)
    {
        // Switching to a cancellable overload only applies to ordinary methods and constructors.
        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor))
        {
            return null;
        }

        // Look for an overload of the definition with no type parameter substitutions.
        var methodDefinition = method.OriginalDefinition;

        var methodsSetsRequiredMembers = methodDefinition.HasSetsRequiredMembers;

        foreach (var member in methodDefinition.ContainingType.GetMembers(method.Name))
        {
            // If the member is the method being compiled, calling it could result in a recursion not present in the code previously.
            // We can't guarantee the instrumentation won't result in infinite recursion, but we can avoid the trivial case.
            if (member == _factory.TopLevelMethod?.OriginalDefinition)
            {
                continue;
            }

            // It's unlikely that real-world APIs have overloads that differ in dynamic,
            // but if they do avoid selecting them since their intended use might differ.
            //
            // Similarly, we also only consider overloads with the same visibility.
            // We could potentially allow the visibility to differ,
            // but we'd need to check if the overload is accessible at the call site.

            const TypeCompareKind TypeComparisonKind = TypeCompareKind.CLRSignatureCompareOptions & ~TypeCompareKind.IgnoreDynamic;

            if (member.IsStatic == methodDefinition.IsStatic &&
                member.MetadataVisibility == methodDefinition.MetadataVisibility &&
                member is MethodSymbol { Parameters: [.., { RefKind: RefKind.None, Type: { } lastParamType }] parametersWithCancellationToken } overload &&
                overload.Arity == methodDefinition.Arity &&
                methodDefinition.MethodKind == overload.MethodKind &&
                methodDefinition.Parameters.Length == parametersWithCancellationToken.Length - 1 &&
                lastParamType.Equals(_throwMethod.ContainingType, TypeCompareKind.ConsiderEverything) &&
                (!methodsSetsRequiredMembers || overload.HasSetsRequiredMembers))
            {
                var typeMap = (methodDefinition.Arity > 0) ? new TypeMap(overload.TypeParameters, methodDefinition.TypeParameters) : null;

                if (MemberSignatureComparer.HaveSameParameterTypes(
                        methodDefinition.Parameters.AsSpan(),
                        typeMap1: null,
                        parametersWithCancellationToken.AsSpan(0, methodDefinition.Parameters.Length),
                        typeMap,
                        MemberSignatureComparer.RefKindCompareMode.ConsiderDifferences,
                        TypeComparisonKind) &&
                    MemberSignatureComparer.HaveSameReturnTypes(
                        methodDefinition,
                        typeMap1: null,
                        overload,
                        typeMap,
                        TypeComparisonKind) &&
                    MemberSignatureComparer.HaveSameConstraints(
                        methodDefinition.TypeParameters,
                        typeMap1: null,
                        overload.TypeParameters,
                        typeMap))
                {
                    var result = overload.AsMember(method.ContainingType);
                    return (result.Arity > 0) ? result.Construct(method.TypeArgumentsWithAnnotations) : result;
                }
            }
        }

        return null;
    }
}
