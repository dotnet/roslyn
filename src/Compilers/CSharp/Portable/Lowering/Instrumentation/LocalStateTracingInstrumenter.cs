// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LocalStateTracingInstrumenter : CompoundInstrumenter
    {
        /// <summary>
        /// Represents instrumentation scope - i.e. method, lambda or local function body.
        /// We define a new <see cref="Scope.ContextVariable"/> (of LocalStoreTracker well-known type) in each scope,
        /// so that this variable is always directly accessible within any instrumented code and always stack-allocated.
        /// </summary>
        private sealed class Scope
        {
            public LocalSymbol ContextVariable;
            private ArrayBuilder<LocalSymbol>? _lazyPreviousContextVariables;

            public Scope(LocalSymbol contextVariable)
            {
                ContextVariable = contextVariable;
            }

            public void Open(LocalSymbol local)
            {
                _lazyPreviousContextVariables ??= ArrayBuilder<LocalSymbol>.GetInstance();
                _lazyPreviousContextVariables.Push(ContextVariable);
                ContextVariable = local;
            }

            public void Close(bool isMethodBody)
            {
                if (_lazyPreviousContextVariables is { Count: > 0 })
                {
                    ContextVariable = _lazyPreviousContextVariables.Pop();
                }

                if (isMethodBody)
                {
                    Debug.Assert(_lazyPreviousContextVariables?.IsEmpty() != false);
                    _lazyPreviousContextVariables?.Free();
                }
            }
        }

        private readonly Scope _scope;
        private readonly SyntheticBoundNodeFactory _factory;
        private readonly BindingDiagnosticBag _diagnostics;
        private readonly TypeSymbol _instrumentationType;

        private LocalStateTracingInstrumenter(
            Scope state,
            TypeSymbol instrumentationType,
            SyntheticBoundNodeFactory factory,
            BindingDiagnosticBag diagnostics,
            Instrumenter previous)
            : base(previous)
        {
            _scope = state;
            _instrumentationType = instrumentationType;
            _factory = factory;
            _diagnostics = diagnostics;
        }

        protected override CompoundInstrumenter WithPreviousImpl(Instrumenter previous)
            => new LocalStateTracingInstrumenter(
                _scope,
                _instrumentationType,
                _factory,
                _diagnostics,
                previous);

        public static bool TryCreate(
            MethodSymbol method,
            BoundStatement methodBody,
            SyntheticBoundNodeFactory factory,
            BindingDiagnosticBag diagnostics,
            Instrumenter previous,
            [NotNullWhen(true)] out LocalStateTracingInstrumenter? instrumenter)
        {
            instrumenter = null;

            // Do not instrument implicitly-declared methods, except for constructors.
            // Instrument implicit constructors in order to instrument member initializers.
            if (method.IsImplicitlyDeclared && !method.IsImplicitConstructor)
            {
                return false;
            }

            // Method has no user-defined body
            if (method is SourceMemberMethodSymbol { Bodies: { arrowBody: null, blockBody: null } } and not SynthesizedSimpleProgramEntryPointSymbol)
            {
                return false;
            }

            var instrumentationType = factory.Compilation.GetWellKnownType(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker);
            if (method.ContainingType.Equals(instrumentationType))
            {
                return false;
            }

            var scope = new Scope(factory.SynthesizedLocal(instrumentationType, methodBody.Syntax, kind: SynthesizedLocalKind.LocalStoreTracker));
            instrumenter = new LocalStateTracingInstrumenter(scope, instrumentationType, factory, diagnostics, previous);
            return true;
        }

        private MethodSymbol? GetLocalOrParameterStoreLogger(TypeSymbol variableType, Symbol targetSymbol, bool? refAssignmentSourceIsLocal, SyntaxNode syntax)
        {
            var enumDelta = (targetSymbol.Kind == SymbolKind.Parameter) ?
                WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreBoolean - WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreBoolean : 0;

            var overload = refAssignmentSourceIsLocal switch
            {
                true => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreLocalAlias,
                false => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreParameterAlias,
                null => variableType.EnumUnderlyingTypeOrSelf().SpecialType switch
                {
                    SpecialType.System_Boolean
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreBoolean,
                    SpecialType.System_SByte or SpecialType.System_Byte
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreByte,
                    SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Char
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt16,
                    SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Single
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt32,
                    SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Double
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt64,
                    SpecialType.System_String
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreString,
                    _ when variableType.IsPointerOrFunctionPointer()
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStorePointer,
                    _ when !variableType.IsManagedTypeNoUseSiteDiagnostics
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUnmanaged,
                    _ when variableType.TypeKind is TypeKind.Struct
                        // well emit ToString constrained virtcall to avoid boxing the struct
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreString,
                    _
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreObject,
                }
            };

            // LogLocalStoreLocalAlias does not have a corresponding parameter version,
            // since it is not possible to assign address of a local to a by-ref parameter.
            Debug.Assert(enumDelta == 0 || overload != WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreLocalAlias);

            overload += enumDelta;

            var symbol = GetWellKnownMethodSymbol(overload, syntax);
            if (symbol is not null)
            {
                return symbol.IsGenericMethod ? symbol.Construct(variableType) : symbol;
            }

            var objectOverload = enumDelta + WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreObject;

            if (refAssignmentSourceIsLocal.HasValue || variableType.IsRefLikeType || variableType.IsPointerOrFunctionPointer() || overload == objectOverload)
            {
                return null;
            }

            // fall back to Object overload if the specialized one is not present
            return GetWellKnownMethodSymbol(objectOverload, syntax);
        }

        private MethodSymbol? GetWellKnownMethodSymbol(WellKnownMember overload, SyntaxNode syntax)
            => (MethodSymbol?)Binder.GetWellKnownTypeMember(_factory.Compilation, overload, _diagnostics, syntax: syntax, isOptional: false);

        public override void PreInstrumentBlock(BoundBlock original, LocalRewriter rewriter)
        {
            Previous.PreInstrumentBlock(original, rewriter);

            if (rewriter.CurrentLambdaBody == original)
            {
                _scope.Open(_factory.SynthesizedLocal(_instrumentationType, original.Syntax, kind: SynthesizedLocalKind.LocalStoreTracker));
            }
        }

        public override void InstrumentBlock(BoundBlock original, LocalRewriter rewriter, ref TemporaryArray<LocalSymbol> additionalLocals, out BoundStatement? prologue, out BoundStatement? epilogue, out BoundBlockInstrumentation? instrumentation)
        {
            base.InstrumentBlock(original, rewriter, ref additionalLocals, out var previousPrologue, out epilogue, out instrumentation);

            var isMethodBody = rewriter.CurrentMethodBody == original;
            var isLambdaBody = rewriter.CurrentLambdaBody == original;

            // Don't instrument blocks that are not a method or lambda body
            if (!isMethodBody && !isLambdaBody)
            {
                prologue = previousPrologue;
                return;
            }

            Debug.Assert(_factory.TopLevelMethod is not null);
            Debug.Assert(_factory.CurrentFunction is not null);

            var isStateMachine = _factory.CurrentFunction.IsAsync || _factory.CurrentFunction.IsIterator;

            var prologueBuilder = ArrayBuilder<BoundStatement>.GetInstance(_factory.CurrentFunction.ParameterCount);

            foreach (var parameter in _factory.CurrentFunction.Parameters)
            {
                if (parameter.RefKind == RefKind.Out)
                {
                    continue;
                }

                var parameterLogger = GetLocalOrParameterStoreLogger(parameter.Type, parameter, refAssignmentSourceIsLocal: null, _factory.Syntax);
                if (parameterLogger != null)
                {
                    prologueBuilder.Add(_factory.ExpressionStatement(_factory.Call(receiver: _factory.Local(_scope.ContextVariable), parameterLogger, new[]
                    {
                        MakeSourceArgument(parameterLogger.Parameters[0], parameter, parameter.Type, _factory.Parameter(parameter), refAssignmentSourceIndex: null),
                        _factory.Literal((ushort)parameter.Ordinal)
                    })));
                }
            }

            if (previousPrologue != null)
            {
                prologueBuilder.Add(previousPrologue);
            }

            prologue = _factory.StatementList(prologueBuilder.ToImmutableAndFree());

            var (entryOverload, entryArgs) = (isLambdaBody, isStateMachine) switch
            {
                (isLambdaBody: false, isStateMachine: false) =>
                    (WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogMethodEntry,
                    new[] { _factory.MethodDefIndex(_factory.TopLevelMethod) }),
                (isLambdaBody: true, isStateMachine: false) =>
                    (WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLambdaEntry,
                    new[] { _factory.MethodDefIndex(_factory.TopLevelMethod), _factory.MethodDefIndex(_factory.CurrentFunction) }),
                (isLambdaBody: false, isStateMachine: true) =>
                    (WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogStateMachineMethodEntry,
                    new[] { _factory.MethodDefIndex(_factory.TopLevelMethod), _factory.StateMachineInstanceId() }),
                (isLambdaBody: true, isStateMachine: true) =>
                    (WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogStateMachineLambdaEntry,
                    new[] { _factory.MethodDefIndex(_factory.TopLevelMethod), _factory.MethodDefIndex(_factory.CurrentFunction), _factory.StateMachineInstanceId() }),
            };

            var entryLogger = GetWellKnownMethodSymbol(entryOverload, _factory.Syntax);
            var instrumentationPrologue = (entryLogger != null) ?
                _factory.Assignment(_factory.Local(_scope.ContextVariable), _factory.Call(receiver: null, entryLogger, entryArgs)) : _factory.NoOp(NoOpStatementFlavor.Default);

            var returnLogger = GetWellKnownMethodSymbol(WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogReturn, _factory.Syntax);
            var instrumentationEpilogue = (returnLogger != null) ?
                _factory.ExpressionStatement(_factory.Call(receiver: _factory.Local(_scope.ContextVariable), returnLogger)) : _factory.NoOp(NoOpStatementFlavor.Default);

            // currently don't need to compose multiple instrumentations
            Debug.Assert(instrumentation is null);

            instrumentation = new BoundBlockInstrumentation(
                _factory.Syntax,
                _scope.ContextVariable,
                instrumentationPrologue,
                instrumentationEpilogue);

            _scope.Close(isMethodBody);
        }

        public override BoundExpression InstrumentUserDefinedLocalAssignment(BoundAssignmentOperator original)
        {
            Debug.Assert(original.Left is BoundLocal { LocalSymbol.SynthesizedKind: SynthesizedLocalKind.UserDefined } or BoundParameter);

            var assignment = base.InstrumentUserDefinedLocalAssignment(original);

            bool? refAssignmentSourceIsLocal;
            BoundExpression? refAssignmentSourceIndex;
            if (original.IsRef)
            {
                if (original.Right is BoundLocal { LocalSymbol.SynthesizedKind: SynthesizedLocalKind.UserDefined } rightLocal)
                {
                    refAssignmentSourceIsLocal = true;
                    refAssignmentSourceIndex = _factory.LocalId(rightLocal.LocalSymbol);
                }
                else if (original.Right is BoundParameter rightParameter)
                {
                    refAssignmentSourceIsLocal = false;
                    refAssignmentSourceIndex = _factory.ParameterId(rightParameter.ParameterSymbol);
                }
                else
                {
                    // We don't track field aliases.
                    return assignment;
                }
            }
            else
            {
                refAssignmentSourceIsLocal = null;
                refAssignmentSourceIndex = null;
            }

            var isLocalOrParameter = TryGetLocalOrParameterInfo(original.Left, out var targetSymbol, out var targetType, out var targetIndex);
            Debug.Assert(isLocalOrParameter);
            Debug.Assert(targetType is not null); // TODO: why are these needed?
            Debug.Assert(targetSymbol is not null);
            Debug.Assert(targetIndex is not null);

            var logger = GetLocalOrParameterStoreLogger(targetType, targetSymbol, refAssignmentSourceIsLocal, original.Syntax);
            if (logger is null)
            {
                return assignment;
            }

            return _factory.Sequence(new[]
            {
                _factory.Call(
                    receiver: _factory.Local(_scope.ContextVariable),
                    logger,
                    new[] { MakeSourceArgument(logger.Parameters[0], targetSymbol, targetType, assignment, refAssignmentSourceIndex), targetIndex })
            }, VariableRead(targetSymbol));
        }

        private bool TryGetLocalOrParameterInfo(BoundNode node, [NotNullWhen(true)] out Symbol? symbol, [NotNullWhen(true)] out TypeSymbol? type, [NotNullWhen(true)] out BoundExpression? indexExpression)
        {
            if (node is BoundLocal { LocalSymbol: var localSymbol })
            {
                symbol = localSymbol;
                type = localSymbol.Type;
                indexExpression = _factory.LocalId(localSymbol);
                return true;
            }

            if (node is BoundParameter { ParameterSymbol: var parameterSymbol })
            {
                symbol = parameterSymbol;
                type = parameterSymbol.Type;
                indexExpression = _factory.ParameterId(parameterSymbol);
                return true;
            }

            symbol = null;
            indexExpression = null;
            type = null;
            return false;
        }

        private BoundExpression MakeSourceArgument(ParameterSymbol parameter, Symbol targetSymbol, TypeSymbol targetType, BoundExpression value, BoundExpression? refAssignmentSourceIndex)
        {
            if (refAssignmentSourceIndex != null)
            {
                return _factory.Sequence(new[] { value }, refAssignmentSourceIndex);
            }

            if (parameter.RefKind == RefKind.None)
            {
                if (parameter.Type.SpecialType == SpecialType.System_String && targetType.SpecialType != SpecialType.System_String)
                {
                    var toString = GetWellKnownMethodSymbol(WellKnownMember.System_Object__ToString, value.Syntax);
                    if (toString is null)
                    {
                        // arbitrary string, won't happen in practice
                        return _factory.Literal("");
                    }

                    return _factory.Call(value, toString);
                }

                return _factory.Convert(parameter.Type, value);
            }

            // address of assigned value:
            Debug.Assert(parameter.RefKind == RefKind.Ref);
            if (value is BoundLocal or BoundParameter)
            {
                return value;
            }

            return _factory.Sequence(new[] { value }, VariableRead(targetSymbol));
        }

        private BoundExpression VariableRead(Symbol localOrParameterSymbol)
            => localOrParameterSymbol switch
            {
                LocalSymbol local => _factory.Local(local),
                ParameterSymbol param => _factory.Parameter(param),
                _ => throw ExceptionUtilities.UnexpectedValue(localOrParameterSymbol)
            };

        public override void InstrumentCatchBlock(
            BoundCatchBlock original,
            ref BoundExpression? rewrittenSource,
            ref BoundStatementList? rewrittenFilterPrologue,
            ref BoundExpression? rewrittenFilter,
            ref BoundBlock rewrittenBody,
            ref TypeSymbol? rewrittenType,
            SyntheticBoundNodeFactory factory)
        {
            base.InstrumentCatchBlock(
                original,
                ref rewrittenSource,
                ref rewrittenFilterPrologue,
                ref rewrittenFilter,
                ref rewrittenBody,
                ref rewrittenType,
                factory);

            if (original.WasCompilerGenerated)
            {
                return;
            }

            var targetSymbol = original.Locals.FirstOrDefault(l => l.SynthesizedKind == SynthesizedLocalKind.UserDefined);
            if (targetSymbol is null)
            {
                return;
            }

            var targetType = targetSymbol.Type;
            var targetIndex = _factory.LocalId(targetSymbol);

            var logger = GetLocalOrParameterStoreLogger(targetType, targetSymbol, refAssignmentSourceIsLocal: null, original.Syntax);
            if (logger is null)
            {
                return;
            }

            var logCallStatement = _factory.ExpressionStatement(
                _factory.Call(
                    receiver: _factory.Local(_scope.ContextVariable),
                    logger,
                    new[] { MakeSourceArgument(logger.Parameters[0], targetSymbol, targetType, VariableRead(targetSymbol), refAssignmentSourceIndex: null), targetIndex }));

            rewrittenFilterPrologue = _factory.StatementList(
                (rewrittenFilterPrologue != null) ?
                    ImmutableArray.Create<BoundStatement>(logCallStatement, rewrittenFilterPrologue) :
                    ImmutableArray.Create<BoundStatement>(logCallStatement));
        }

        public override BoundExpression InstrumentCall(BoundCall original, BoundExpression rewritten)
            => InstrumentCall(base.InstrumentCall(original, rewritten), original.Arguments, original.ArgumentRefKindsOpt);

        public override BoundExpression InstrumentObjectCreationExpression(BoundObjectCreationExpression original, BoundExpression rewritten)
            => InstrumentCall(base.InstrumentObjectCreationExpression(original, rewritten), original.Arguments, original.ArgumentRefKindsOpt);

        public override BoundExpression InstrumentFunctionPointerInvocation(BoundFunctionPointerInvocation original, BoundExpression rewritten)
            => InstrumentCall(base.InstrumentFunctionPointerInvocation(original, rewritten), original.Arguments, original.ArgumentRefKindsOpt);

        private BoundExpression InstrumentCall(BoundExpression invocation, ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKinds)
        {
            Debug.Assert(refKinds.IsDefault || arguments.Length == refKinds.Length);
            Debug.Assert(invocation.Type is not null);

            if (refKinds.IsDefaultOrEmpty)
            {
                return invocation;
            }

            var builder = ArrayBuilder<BoundExpression>.GetInstance();

            BoundLocal? temp = null;
            if (invocation.Type.SpecialType != SpecialType.System_Void)
            {
                temp = _factory.StoreToTemp(invocation, out var store);
                builder.Add(store);
            }
            else
            {
                builder.Add(invocation);
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                if (refKinds[i] is not (RefKind.Ref or RefKind.Out))
                {
                    // not by-ref
                    continue;
                }

                if (!TryGetLocalOrParameterInfo(arguments[i], out var targetSymbol, out var targetType, out var targetIndex))
                {
                    // not a local or parameter
                    continue;
                }

                var logger = GetLocalOrParameterStoreLogger(targetType, targetSymbol, refAssignmentSourceIsLocal: null, invocation.Syntax);
                if (logger is null)
                {
                    return invocation;
                }

                builder.Add(_factory.Call(
                    receiver: _factory.Local(_scope.ContextVariable),
                    logger,
                    new[] { MakeSourceArgument(logger.Parameters[0], targetSymbol, targetType, VariableRead(targetSymbol), refAssignmentSourceIndex: null), targetIndex }));
            }

            if (temp != null)
            {
                return _factory.Sequence(ImmutableArray.Create(temp.LocalSymbol), builder.ToImmutableAndFree(), temp);
            }

            // The call is void returning.
            var lastExpression = builder.Last();
            builder.RemoveLast();
            return _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, builder.ToImmutableAndFree(), lastExpression);
        }
    }
}
