// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Implements instrumentation for <see cref="CodeAnalysis.Emit.InstrumentationKindExtensions.LocalStateTracing"/>.
    /// </summary>
    /// <remarks>
    /// Adds calls to well-known instrumentation helpers defined by <see cref="WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker"/> to the bodies of instrumented methods.
    /// These allow tracing method entries, returns and writes to user-defined local variables and parameters.
    /// 
    /// The instrumenter also adds the ability to stitch calls to MoveNext methods of a state machine that are executed as continuations of the same instance of the state machine
    /// but potentially from multiple different threads.
    ///
    /// The instrumentation introduces several new bound nodes:
    ///
    /// 1) <see cref="BoundBlockInstrumentation"/>
    ///    This node is attached to a <see cref="BoundBlock"/> that represents the lowered body of an instrumented method, lambda or local function.
    ///    It defines a local variable used to store instrumentation context and prologue and epilogue.
    ///    
    ///    <see cref="BoundBlock"/> with block instrumentation is eventually lowered to:
    ///    <code>
    ///    [[prologue]]
    ///    try
    ///    {
    ///       [[method body]]
    ///    }
    ///    finally
    ///    {
    ///       [[epilogue]]
    ///    }
    ///    </code>
    ///
    ///    The prologue is:
    ///    <code>
    ///    var $context = LocalStateTracker.LogXyzEntry($ids);
    ///    </code>
    ///
    ///    Where Xyz is a combination of <c>StateMachine</c> and either <c>Method</c> or <c>Lambda</c>, and $ids is the corresponding set of arguments identifying the context.
    ///    LogXyzEntry methods are static factory methods for <see cref="WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker"/>. 
    ///    
    ///    The tracker type is a <c>ref struct</c>. It can only be allocated on the stack and accessed only directly from the declaring method (no lifting).
    ///    For member methods $ids is a single argument that is the method token.
    ///    For lambdas and local functions the token of the lambda method is passed in addition to the containing method token. This allows the logger to determine which lambda belongs to which method,
    ///    as that it not apparent from metadata.
    ///    For state machines, the state machine instance id is passed. The instance id is stored on a new synthesized field of the state machine type added by the instrumentation that
    ///    is initialized to a unique number when the state machine type is instantiated. The number is provided by
    ///    <see cref="WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__GetNewStateMachineInstanceId"/>.
    ///    
    ///    The epilogue is simply:
    ///    <code>
    ///    $context.LogReturn();
    ///    </code>
    ///
    /// 2) <see cref="BoundStateMachineInstanceId"/>
    ///    This node represents a reference to a synthesized state machine instance id field of the state machine. Lowered to a field read during state machine lowering.
    ///
    /// 3) <see cref="BoundParameterId"/>/<see cref="BoundLocalId"/>
    ///    Represents id of a user-defined parameter/local. Emitted as ldc.i4 of either the parameter/local ordinal if the variable was not lifted, 
    ///    or the token of its hoisted field.
    ///
    /// Each local variable write is followed by a call to one of the <c>LogLocalStoreXyz</c> or <c>LogParameterStoreXyz</c> instance methods on <c>$context</c>.
    /// Writes to locals passed to a function call site by-ref are logged after the call returns.
    /// <c>LogParameterStoreXyz</c> are emitted on explicit parameter assignment and also at the beginning of a method with parameters to log their initial values.
    /// 
    /// The loggers are specialized to handle all kinds of variable types efficiently (without boxing).
    /// Specialized loggers are also used to track local variable aliases via ref assignments.
    /// </remarks>
    internal sealed class LocalStateTracingInstrumenter : CompoundInstrumenter
    {
        /// <summary>
        /// Represents instrumentation scope - i.e. method, lambda or local function body.
        /// We define a new <see cref="ContextVariable"/> (of LocalStoreTracker well-known type) in each scope,
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
                    Debug.Assert(_lazyPreviousContextVariables?.IsEmpty != false);
                    _lazyPreviousContextVariables?.Free();
                    _lazyPreviousContextVariables = null;
                }
            }
        }

        private readonly Scope _scope;
        private readonly SyntheticBoundNodeFactory _factory;
        private readonly BindingDiagnosticBag _diagnostics;

        /// <summary>
        /// Type of the variable that holds on the instrumentation context (LocalStateTracker).
        /// </summary>
        private readonly TypeSymbol _contextType;

        private LocalStateTracingInstrumenter(
            Scope scope,
            TypeSymbol contextType,
            SyntheticBoundNodeFactory factory,
            BindingDiagnosticBag diagnostics,
            Instrumenter previous)
            : base(previous)
        {
            _scope = scope;
            _contextType = contextType;
            _factory = factory;
            _diagnostics = diagnostics;
        }

        protected override CompoundInstrumenter WithPreviousImpl(Instrumenter previous)
            => new LocalStateTracingInstrumenter(
                _scope,
                _contextType,
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

            var contextType = factory.Compilation.GetWellKnownType(WellKnownType.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker);
            if (IsSameOrNestedType(method.ContainingType, contextType))
            {
                return false;
            }

            var scope = new Scope(factory.SynthesizedLocal(contextType, methodBody.Syntax, kind: SynthesizedLocalKind.LocalStoreTracker));
            instrumenter = new LocalStateTracingInstrumenter(scope, contextType, factory, diagnostics, previous);
            return true;
        }

        private static bool IsSameOrNestedType(NamedTypeSymbol type, NamedTypeSymbol otherType)
        {
            while (true)
            {
                if (type.Equals(otherType))
                {
                    return true;
                }

                if (type.ContainingType is null)
                {
                    return false;
                }

                type = type.ContainingType;
            }
        }

        private MethodSymbol? GetLocalOrParameterStoreLogger(TypeSymbol variableType, Symbol targetSymbol, bool? refAssignmentSourceIsLocal, SyntaxNode syntax)
        {
            var enumDelta = (targetSymbol.Kind == SymbolKind.Parameter) ?
                WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogParameterStoreBoolean - WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreBoolean : 0;

            WellKnownMember? overloadOpt = refAssignmentSourceIsLocal switch
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
                    SpecialType.System_Int32 or SpecialType.System_UInt32
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt32,
                    SpecialType.System_Int64 or SpecialType.System_UInt64
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUInt64,
                    SpecialType.System_Single
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreSingle,
                    SpecialType.System_Double
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreDouble,
                    SpecialType.System_Decimal
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreDecimal,
                    SpecialType.System_String
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreString,
                    _ when variableType.IsPointerOrFunctionPointer()
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStorePointer,
                    _ when !variableType.IsManagedTypeNoUseSiteDiagnostics
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreUnmanaged,
                    _ when variableType.IsRefLikeType && !hasOverriddenToString(variableType)
                        => null, // not possible to invoke ToString on ref struct that doesn't override it
                    _ when variableType is TypeParameterSymbol { AllowsRefLikeType: true }
                        => null, // not possible to invoke ToString on ref struct type parameter
                    _ when variableType.TypeKind is TypeKind.Struct
                        // we'll emit ToString constrained virtcall to avoid boxing the struct
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreString,
                    _
                        => WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreObject,
                }
            };

            static bool hasOverriddenToString(TypeSymbol variableType)
                => variableType.GetMembers(WellKnownMemberNames.ObjectToString).Any(m => m.GetOverriddenMember() is not null);

            if (!overloadOpt.HasValue)
            {
                return null;
            }

            // LogLocalStoreLocalAlias does not have a corresponding parameter version,
            // since it is not possible to assign address of a local to a by-ref parameter.
            Debug.Assert(enumDelta == 0 || overloadOpt.Value != WellKnownMember.Microsoft_CodeAnalysis_Runtime_LocalStoreTracker__LogLocalStoreLocalAlias);

            var overload = overloadOpt.Value + enumDelta;

            var symbol = GetWellKnownMethodSymbol(overload, syntax);
            Debug.Assert(symbol?.IsGenericMethod != true);
            return symbol;
        }

        private MethodSymbol? GetWellKnownMethodSymbol(WellKnownMember overload, SyntaxNode syntax)
            => (MethodSymbol?)Binder.GetWellKnownTypeMember(_factory.Compilation, overload, _diagnostics, syntax: syntax, isOptional: false);

        private MethodSymbol? GetSpecialMethodSymbol(SpecialMember overload, SyntaxNode syntax)
            => (MethodSymbol?)Binder.GetSpecialTypeMember(_factory.Compilation, overload, _diagnostics, syntax: syntax);

        public override void PreInstrumentBlock(BoundBlock original, LocalRewriter rewriter)
        {
            Previous.PreInstrumentBlock(original, rewriter);

            if (rewriter.CurrentLambdaBody == original)
            {
                _scope.Open(_factory.SynthesizedLocal(_contextType, original.Syntax, kind: SynthesizedLocalKind.LocalStoreTracker));
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

<<<<<<< HEAD
            var isStateMachine = getIsStateMachine(_factory.CurrentFunction);
=======
            var currentFunction = _factory.CurrentFunction;
            var isStateMachine = (currentFunction.IsAsync && !_factory.Compilation.IsRuntimeAsyncEnabledIn(currentFunction))
                                 || currentFunction.IsIterator;
>>>>>>> dotnet/main

            var prologueBuilder = ArrayBuilder<BoundStatement>.GetInstance(_factory.CurrentFunction.ParameterCount);

            foreach (var parameter in _factory.CurrentFunction.GetParametersIncludingExtensionParameter(skipExtensionIfStatic: true))
            {
                if (parameter.RefKind == RefKind.Out || parameter.IsDiscard)
                {
                    continue;
                }

                var parameterLogger = GetLocalOrParameterStoreLogger(parameter.Type, parameter, refAssignmentSourceIsLocal: null, _factory.Syntax);
                if (parameterLogger != null)
                {
                    int ordinal = parameter.ContainingSymbol.IsExtensionBlockMember()
                        ? SourceExtensionImplementationMethodSymbol.GetImplementationParameterOrdinal(parameter)
                        : parameter.Ordinal;

                    prologueBuilder.Add(_factory.ExpressionStatement(_factory.Call(receiver: _factory.Local(_scope.ContextVariable), parameterLogger,
                        MakeStoreLoggerArguments(parameterLogger.Parameters[0], parameter, parameter.Type, _factory.Parameter(parameter), refAssignmentSourceIndex: null, _factory.Literal((ushort)ordinal)))));
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

            instrumentation = _factory.CombineInstrumentation(instrumentation, _scope.ContextVariable, instrumentationPrologue, instrumentationEpilogue);

            _scope.Close(isMethodBody);

            static bool getIsStateMachine(MethodSymbol method)
            {
                if (method.IsIterator)
                {
                    return true;
                }

                if (method.IsAsync)
                {
                    return !method.DeclaringCompilation.IsRuntimeAsyncEnabledIn(method);
                }

                return false;
            }
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

            if (!TryGetLocalOrParameterInfo(original.Left, out var targetSymbol, out var targetType, out var targetIndex))
            {
                // Must be local or parameter
                throw ExceptionUtilities.UnexpectedValue(original.Left);
            }

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
                    MakeStoreLoggerArguments(logger.Parameters[0], targetSymbol, targetType, assignment, refAssignmentSourceIndex, targetIndex))
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
                Debug.Assert(!parameterSymbol.IsDiscard);

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

        private ImmutableArray<BoundExpression> MakeStoreLoggerArguments(
            ParameterSymbol parameter,
            Symbol targetSymbol,
            TypeSymbol targetType,
            BoundExpression value,
            BoundExpression? refAssignmentSourceIndex,
            BoundExpression index)
        {
            Debug.Assert(index is BoundParameterId or BoundLocalId or BoundLiteral);
            if (refAssignmentSourceIndex != null)
            {
                return ImmutableArray.Create(_factory.Sequence(new[] { value }, refAssignmentSourceIndex), index);
            }

            Debug.Assert(parameter.RefKind == RefKind.None);

            if (parameter.Type.IsVoidPointer() && !targetType.IsPointerOrFunctionPointer())
            {
                // address of assigned value to be passed to LogStore*Unmanaged:
                Debug.Assert(!parameter.Type.IsManagedTypeNoUseSiteDiagnostics);

                var addressOf = value is BoundLocal or BoundParameter ?
                    (BoundExpression)new BoundAddressOfOperator(_factory.Syntax, value, isManaged: false, parameter.Type) :
                    _factory.Sequence(new[] { value }, new BoundAddressOfOperator(_factory.Syntax, VariableRead(targetSymbol), isManaged: false, parameter.Type));

                return ImmutableArray.Create(addressOf, _factory.Sizeof(targetType), index);
            }

            if (parameter.Type.SpecialType == SpecialType.System_String && targetType.SpecialType != SpecialType.System_String)
            {
                var toStringMethod = GetSpecialMethodSymbol(SpecialMember.System_Object__ToString, value.Syntax);

                BoundExpression toString;
                if (toStringMethod is null)
                {
                    // arbitrary string, won't happen in practice
                    toString = _factory.Literal("");
                }
                else
                {
                    // value won't be null:
                    Debug.Assert(targetType.IsStructType());
                    toString = _factory.Call(value, toStringMethod);
                }

                return ImmutableArray.Create(toString, index);
            }

            Conversion c = _factory.ClassifyEmitConversion(value, parameter.Type);
            Debug.Assert(c.IsNumeric || c.IsReference || c.IsIdentity || c.IsPointer || c.IsBoxing || c.IsEnumeration);
            return ImmutableArray.Create(_factory.Convert(parameter.Type, value, c), index);
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
                    MakeStoreLoggerArguments(logger.Parameters[0], targetSymbol, targetType, VariableRead(targetSymbol), refAssignmentSourceIndex: null, targetIndex)));

            rewrittenFilterPrologue = _factory.StatementList(
                (rewrittenFilterPrologue != null) ?
                    ImmutableArray.Create<BoundStatement>(logCallStatement, rewrittenFilterPrologue) :
                    ImmutableArray.Create<BoundStatement>(logCallStatement));
        }

        public override BoundExpression InstrumentCall(BoundCall original, BoundExpression rewritten)
        {
            ImmutableArray<BoundExpression> arguments = original.Arguments;
            MethodSymbol method = original.Method;
            bool adjustForExtensionBlockMethod = method.IsExtensionBlockMember() && !method.IsStatic;
            ImmutableArray<RefKind> argumentRefKindsOpt = NullableWalker.AdjustArgumentRefKindsIfNeeded(original.ArgumentRefKindsOpt, adjustForExtensionBlockMethod, method, arguments.Length);

            if (adjustForExtensionBlockMethod)
            {
                Debug.Assert(original.ReceiverOpt is not null);
                arguments = [original.ReceiverOpt, .. arguments];
            }

            return InstrumentCall(base.InstrumentCall(original, rewritten), arguments, argumentRefKindsOpt);
        }

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

            // Record outbound assignments
            for (int i = 0; i < arguments.Length; i++)
            {
                if (refKinds[i] is not (RefKind.Ref or RefKind.Out))
                {
                    // not writable reference
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
                    continue;
                }

                builder.Add(_factory.Call(
                    receiver: _factory.Local(_scope.ContextVariable),
                    logger,
                    MakeStoreLoggerArguments(logger.Parameters[0], targetSymbol, targetType, VariableRead(targetSymbol), refAssignmentSourceIndex: null, targetIndex)));
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
