﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal interface IBoundLambdaOrFunction
    {
        MethodSymbol Symbol { get; }
        SyntaxNode Syntax { get; }
        BoundBlock Body { get; }
        bool WasCompilerGenerated { get; }
    }

    internal sealed partial class BoundLocalFunctionStatement : IBoundLambdaOrFunction
    {
        MethodSymbol IBoundLambdaOrFunction.Symbol { get { return Symbol; } }

        SyntaxNode IBoundLambdaOrFunction.Syntax { get { return Syntax; } }
    }

    internal sealed partial class BoundLambda : IBoundLambdaOrFunction
    {
        public MessageID MessageID { get { return Syntax.Kind() == SyntaxKind.AnonymousMethodExpression ? MessageID.IDS_AnonMethod : MessageID.IDS_Lambda; } }

        private readonly bool _inferredFromSingleType;
        private readonly RefKind _refKind;
        private readonly TypeSymbol _inferredReturnType;
        private readonly HashSet<DiagnosticInfo> _inferredReturnTypeUseSiteDiagnostics;

#if DEBUG
        private readonly bool _hasInferredReturnType;
#endif

        public bool InferredFromSingleType
        {
            get
            {
                return _inferredFromSingleType;
            }
        }

        public RefKind RefKind
        {
            get
            {
                return _refKind;
            }
        }

        MethodSymbol IBoundLambdaOrFunction.Symbol { get { return Symbol; } }

        SyntaxNode IBoundLambdaOrFunction.Syntax { get { return Syntax; } }

        public BoundLambda(SyntaxNode syntax, BoundBlock body, ImmutableArray<Diagnostic> diagnostics, Binder binder, TypeSymbol delegateType, bool inferReturnType)
            : this(syntax, (LambdaSymbol)binder.ContainingMemberOrLambda, body, diagnostics, binder, delegateType)
        {
            if (inferReturnType)
            {
                this._inferredReturnType = InferReturnType(
                    this.Body,
                    this.Binder,
                    delegateType,
                    this.Symbol.IsAsync,
                    ref this._inferredReturnTypeUseSiteDiagnostics,
                    out this._refKind,
                    out this._inferredFromSingleType);

#if DEBUG
                _hasInferredReturnType = true;
#endif
            }

            Debug.Assert(
                syntax.IsAnonymousFunction() ||                                                                 // lambda expressions
                syntax is ExpressionSyntax && LambdaUtilities.IsLambdaBody(syntax, allowReducedLambdas: true) || // query lambdas
                LambdaUtilities.IsQueryPairLambda(syntax)                                                       // "pair" lambdas in queries
            );
        }

        public TypeSymbol InferredReturnType(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
#if DEBUG
            Debug.Assert(_hasInferredReturnType);
#endif

            if (!_inferredReturnTypeUseSiteDiagnostics.IsNullOrEmpty())
            {
                if (useSiteDiagnostics == null)
                {
                    useSiteDiagnostics = new HashSet<DiagnosticInfo>();
                }

                foreach (var info in _inferredReturnTypeUseSiteDiagnostics)
                {
                    useSiteDiagnostics.Add(info);
                }
            }

            return _inferredReturnType;
        }

        /// <summary>
        /// Behavior of this function should be kept aligned with <see cref="UnboundLambdaState.ReturnInferenceCacheKey"/>.
        /// </summary>
        private static TypeSymbol InferReturnType(
            BoundBlock block,
            Binder binder,
            TypeSymbol delegateType,
            bool isAsync,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            out RefKind refKind,
            out bool inferredFromSingleType)
        {
            int numberOfDistinctReturns;
            var resultTypes = BlockReturns.GetReturnTypes(block, out refKind, out numberOfDistinctReturns);

            inferredFromSingleType = numberOfDistinctReturns < 2;

            TypeSymbol bestResultType;
            if (resultTypes.IsDefaultOrEmpty)
            {
                bestResultType = null;
            }
            else if (resultTypes.Length == 1)
            {
                bestResultType = resultTypes[0];
            }
            else
            {
                bestResultType = BestTypeInferrer.InferBestType(resultTypes, binder.Conversions, ref useSiteDiagnostics);
            }

            if (!isAsync)
            {
                return bestResultType;
            }

            // For async lambdas, the return type is the return type of the
            // delegate Invoke method if Invoke has a Task-like return type.
            // Otherwise the return type is Task or Task<T>.
            NamedTypeSymbol taskType = null;
            var delegateReturnType = delegateType?.GetDelegateType()?.DelegateInvokeMethod?.ReturnType as NamedTypeSymbol;
            if ((object)delegateReturnType != null && delegateReturnType.SpecialType != SpecialType.System_Void)
            {
                object builderType;
                if (delegateReturnType.IsCustomTaskType(out builderType))
                {
                    taskType = delegateReturnType.ConstructedFrom;
                }
            }

            if (resultTypes.IsEmpty)
            {
                // No return statements have expressions; use delegate InvokeMethod
                // or infer type Task if delegate type not available.
                return (object)taskType != null && taskType.Arity == 0 ?
                    taskType :
                    binder.Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task);
            }

            if ((object)bestResultType == null || bestResultType.SpecialType == SpecialType.System_Void)
            {
                // If the best type was 'void', ERR_CantReturnVoid is reported while binding the "return void"
                // statement(s).
                return null;
            }

            // Some non-void best type T was found; use delegate InvokeMethod
            // or infer type Task<T> if delegate type not available.
            var taskTypeT = (object)taskType != null && taskType.Arity == 1 ?
                taskType :
                binder.Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T);
            return taskTypeT.Construct(bestResultType);
        }

        internal sealed class BlockReturns : BoundTreeWalker
        {
            private readonly ArrayBuilder<TypeSymbol> _types;
            private bool _hasReturnWithoutArgument;
            private RefKind refKind;

            private BlockReturns()
            {
                _types = ArrayBuilder<TypeSymbol>.GetInstance();
            }

            public static ImmutableArray<TypeSymbol> GetReturnTypes(BoundBlock block, out RefKind refKind, out int numberOfDistinctReturns)
            {
                var inferrer = new BlockReturns();
                inferrer.Visit(block);
                refKind = inferrer.refKind;
                var result = inferrer._types.ToImmutableAndFree();
                numberOfDistinctReturns = result.Length;
                if (inferrer._hasReturnWithoutArgument)
                {
                    numberOfDistinctReturns += 1;
                }

                return result;
            }

            public override BoundNode Visit(BoundNode node)
            {
                if (!(node is BoundExpression))
                {
                    return base.Visit(node);
                }

                return null;
            }

            protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
            {
                // Do not recurse into nested lambdas; we don't want their returns.
                return null;
            }

            public override BoundNode VisitReturnStatement(BoundReturnStatement node)
            {
                if (node.RefKind != RefKind.None)
                {
                    refKind = node.RefKind;
                }

                var expression = node.ExpressionOpt;
                if (expression != null)
                {
                    var returnType = expression.Type;
                    // This is potentially inefficient if there are a large number of returns each with
                    // a different type. This seems unlikely.
                    if (!_types.Contains(returnType))
                    {
                        _types.Add(returnType);
                    }
                }
                else
                {
                    _hasReturnWithoutArgument = true;
                }

                return null;
            }
        }
    }

    internal partial class UnboundLambda
    {
        public UnboundLambda(
            CSharpSyntaxNode syntax,
            Binder binder,
            ImmutableArray<RefKind> refKinds,
            ImmutableArray<TypeSymbol> types,
            ImmutableArray<string> names,
            bool isAsync,
            bool hasErrors = false)
            : base(BoundKind.UnboundLambda, syntax, null, hasErrors || !types.IsDefault && types.Any(SymbolKind.ErrorType))
        {
            Debug.Assert(binder != null);
            Debug.Assert(syntax.IsAnonymousFunction());
            this.Data = new PlainUnboundLambdaState(this, binder, names, types, refKinds, isAsync);
        }

        public MessageID MessageID { get { return Data.MessageID; } }
        public BoundLambda Bind(NamedTypeSymbol delegateType) { return Data.Bind(delegateType); }
        public BoundLambda BindForErrorRecovery() { return Data.BindForErrorRecovery(); }
        public BoundLambda BindForReturnTypeInference(NamedTypeSymbol delegateType) { return Data.BindForReturnTypeInference(delegateType); }
        public bool HasSignature { get { return Data.HasSignature; } }
        public bool HasExplicitlyTypedParameterList { get { return Data.HasExplicitlyTypedParameterList; } }
        public int ParameterCount { get { return Data.ParameterCount; } }
        public TypeSymbol InferReturnType(NamedTypeSymbol delegateType, ref HashSet<DiagnosticInfo> useSiteDiagnostics) { return Data.InferReturnType(delegateType, ref useSiteDiagnostics); }
        public RefKind RefKind(int index) { return Data.RefKind(index); }
        public void GenerateAnonymousFunctionConversionError(DiagnosticBag diagnostics, TypeSymbol targetType) { Data.GenerateAnonymousFunctionConversionError(diagnostics, targetType); }
        public bool GenerateSummaryErrors(DiagnosticBag diagnostics) { return Data.GenerateSummaryErrors(diagnostics); }
        public bool IsAsync { get { return Data.IsAsync; } }
        public TypeSymbol ParameterType(int index) { return Data.ParameterType(index); }
        public Location ParameterLocation(int index) { return Data.ParameterLocation(index); }
        public string ParameterName(int index) { return Data.ParameterName(index); }
    }

    internal abstract class UnboundLambdaState
    {
        private UnboundLambda _unboundLambda; // we would prefer this readonly, but we have an initialization cycle.
        protected readonly Binder binder;
        private readonly ConcurrentDictionary<NamedTypeSymbol, BoundLambda> _bindingCache = new ConcurrentDictionary<NamedTypeSymbol, BoundLambda>();

        private readonly ConcurrentDictionary<ReturnInferenceCacheKey, BoundLambda> _returnInferenceCache = new ConcurrentDictionary<ReturnInferenceCacheKey, BoundLambda>();

        private BoundLambda _errorBinding;

        public UnboundLambdaState(Binder binder, UnboundLambda unboundLambdaOpt)
        {
            Debug.Assert(binder != null);

            // might be initialized later (for query lambdas)
            _unboundLambda = unboundLambdaOpt;
            this.binder = binder;
        }

        public void SetUnboundLambda(UnboundLambda unbound)
        {
            Debug.Assert(unbound != null);
            Debug.Assert(_unboundLambda == null);
            _unboundLambda = unbound;
        }

        public UnboundLambda UnboundLambda => _unboundLambda;

        public abstract MessageID MessageID { get; }
        public abstract string ParameterName(int index);
        public abstract bool HasSignature { get; }
        public abstract bool HasExplicitlyTypedParameterList { get; }
        public abstract int ParameterCount { get; }
        public abstract bool IsAsync { get; }
        public abstract Location ParameterLocation(int index);
        public abstract TypeSymbol ParameterType(int index);
        //public abstract SyntaxToken ParameterIdentifier(int index);
        public abstract RefKind RefKind(int index);
        protected abstract BoundBlock BindLambdaBody(LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, DiagnosticBag diagnostics);

        public virtual void GenerateAnonymousFunctionConversionError(DiagnosticBag diagnostics, TypeSymbol targetType)
        {
            this.binder.GenerateAnonymousFunctionConversionError(diagnostics, _unboundLambda.Syntax, _unboundLambda, targetType);
        }

        // Returns the inferred return type, or null if none can be inferred.
        public BoundLambda Bind(NamedTypeSymbol delegateType)
        {
            BoundLambda result;
            if (!_bindingCache.TryGetValue(delegateType, out result))
            {
                result = ReallyBind(delegateType);
                _bindingCache.TryAdd(delegateType, result);
            }

            return result;
        }

        internal IEnumerable<TypeSymbol> InferredReturnTypes()
        {
            bool any = false;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            foreach (var lambda in _returnInferenceCache.Values)
            {
                var type = lambda.InferredReturnType(ref useSiteDiagnostics);
                if ((object)type != null)
                {
                    any = true;
                    yield return type;
                }
            }

            if (!any)
            {
                var type = BindForErrorRecovery().InferredReturnType(ref useSiteDiagnostics);
                if ((object)type != null)
                {
                    yield return type;
                }
            }
        }

        private static MethodSymbol DelegateInvokeMethod(NamedTypeSymbol delegateType)
        {
            return delegateType.GetDelegateType()?.DelegateInvokeMethod;
        }

        private static TypeSymbol DelegateReturnType(MethodSymbol invokeMethod, out RefKind refKind)
        {
            if ((object)invokeMethod == null)
            {
                refKind = Microsoft.CodeAnalysis.RefKind.None;
                return null;
            }
            refKind = invokeMethod.RefKind;
            return invokeMethod.ReturnType;
        }

        private bool DelegateNeedsReturn(MethodSymbol invokeMethod)
        {
            if ((object)invokeMethod == null || invokeMethod.ReturnsVoid)
            {
                return false;
            }

            if (IsAsync && invokeMethod.ReturnType.IsNonGenericTaskType(this.binder.Compilation))
            {
                return false;
            }

            return true;
        }

        private BoundLambda ReallyBind(NamedTypeSymbol delegateType)
        {
            var invokeMethod = DelegateInvokeMethod(delegateType);
            RefKind refKind;
            var returnType = DelegateReturnType(invokeMethod, out refKind);

            LambdaSymbol lambdaSymbol;
            Binder lambdaBodyBinder;
            BoundBlock block;

            var diagnostics = DiagnosticBag.GetInstance();

            // when binding for real (not for return inference), there is still
            // a good chance that we could reuse a body of a lambda previously bound for 
            // return type inference.
            var cacheKey = ReturnInferenceCacheKey.Create(delegateType, IsAsync);

            BoundLambda returnInferenceLambda;
            if (_returnInferenceCache.TryGetValue(cacheKey, out returnInferenceLambda) && returnInferenceLambda.InferredFromSingleType)
            {
                lambdaSymbol = returnInferenceLambda.Symbol;
                if ((object)LambdaSymbol.InferenceFailureReturnType != lambdaSymbol.ReturnType &&
                    lambdaSymbol.ReturnType == returnType && lambdaSymbol.RefKind == refKind)
                {
                    lambdaBodyBinder = returnInferenceLambda.Binder;
                    block = returnInferenceLambda.Body;
                    diagnostics.AddRange(returnInferenceLambda.Diagnostics);

                    goto haveLambdaBodyAndBinders;
                }
            }

            lambdaSymbol = new LambdaSymbol(
                binder.Compilation,
                binder.ContainingMemberOrLambda,
                _unboundLambda,
                cacheKey.ParameterTypes,
                cacheKey.ParameterRefKinds,
                refKind,
                returnType);
            lambdaBodyBinder = new ExecutableCodeBinder(_unboundLambda.Syntax, lambdaSymbol, ParameterBinder(lambdaSymbol, binder));
            block = BindLambdaBody(lambdaSymbol, lambdaBodyBinder, diagnostics);

            ((ExecutableCodeBinder)lambdaBodyBinder).ValidateIteratorMethods(diagnostics);
            ValidateUnsafeParameters(diagnostics, cacheKey.ParameterTypes);

        haveLambdaBodyAndBinders:

            bool reachableEndpoint = ControlFlowPass.Analyze(binder.Compilation, lambdaSymbol, block, diagnostics);
            if (reachableEndpoint)
            {
                if (DelegateNeedsReturn(invokeMethod))
                {
                    // Not all code paths return a value in {0} of type '{1}'
                    diagnostics.Add(ErrorCode.ERR_AnonymousReturnExpected, lambdaSymbol.DiagnosticLocation, this.MessageID.Localize(), delegateType);
                }
                else
                {
                    block = FlowAnalysisPass.AppendImplicitReturn(block, lambdaSymbol);
                }
            }

            if (IsAsync && !ErrorFacts.PreventsSuccessfulDelegateConversion(diagnostics))
            {
                if ((object)returnType != null && // Can be null if "delegateType" is not actually a delegate type.
                    returnType.SpecialType != SpecialType.System_Void &&
                    !returnType.IsNonGenericTaskType(binder.Compilation) &&
                    !returnType.IsGenericTaskType(binder.Compilation))
                {
                    // Cannot convert async {0} to delegate type '{1}'. An async {0} may return void, Task or Task&lt;T&gt;, none of which are convertible to '{1}'.
                    diagnostics.Add(ErrorCode.ERR_CantConvAsyncAnonFuncReturns, lambdaSymbol.DiagnosticLocation, lambdaSymbol.MessageID.Localize(), delegateType);
                }
            }

            if (IsAsync)
            {
                Debug.Assert(lambdaSymbol.IsAsync);
                SourceOrdinaryMethodSymbol.ReportAsyncParameterErrors(lambdaSymbol.Parameters, diagnostics, lambdaSymbol.DiagnosticLocation);
            }

            var result = new BoundLambda(_unboundLambda.Syntax, block, diagnostics.ToReadOnlyAndFree(), lambdaBodyBinder, delegateType, inferReturnType: false)
            { WasCompilerGenerated = _unboundLambda.WasCompilerGenerated };

            return result;
        }

        private void ValidateUnsafeParameters(DiagnosticBag diagnostics, ImmutableArray<TypeSymbol> targetParameterTypes)
        {
            // It is legal to use a delegate type that has unsafe parameter types inside
            // a safe context if the anonymous method has no parameter list!
            //
            // unsafe delegate void D(int* p);
            // class C { D d = delegate {}; }
            //
            // is legal even if C is not an unsafe context because no int* is actually used.

            if (this.HasSignature)
            {
                // NOTE: we can get here with targetParameterTypes.Length > ParameterCount
                // in a case where we are binding for error reporting purposes 
                var numParametersToCheck = Math.Min(targetParameterTypes.Length, ParameterCount);
                for (int i = 0; i < numParametersToCheck; i++)
                {
                    if (targetParameterTypes[i].IsUnsafe())
                    {
                        this.binder.ReportUnsafeIfNotAllowed(this.ParameterLocation(i), diagnostics);
                    }
                }
            }
        }

        private BoundLambda ReallyInferReturnType(NamedTypeSymbol delegateType, ImmutableArray<TypeSymbol> parameterTypes, ImmutableArray<RefKind> parameterRefKinds)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var lambdaSymbol = new LambdaSymbol(binder.Compilation, binder.ContainingMemberOrLambda, _unboundLambda, parameterTypes, parameterRefKinds, refKind: Microsoft.CodeAnalysis.RefKind.None, returnType: null);
            Binder lambdaBodyBinder = new ExecutableCodeBinder(_unboundLambda.Syntax, lambdaSymbol, ParameterBinder(lambdaSymbol, binder));
            var block = BindLambdaBody(lambdaSymbol, lambdaBodyBinder, diagnostics);

            var result = new BoundLambda(_unboundLambda.Syntax, block, diagnostics.ToReadOnlyAndFree(), lambdaBodyBinder, delegateType, inferReturnType: true)
            { WasCompilerGenerated = _unboundLambda.WasCompilerGenerated };

            HashSet<DiagnosticInfo> useSiteDiagnostics = null; // TODO: figure out if this should be somehow merged into BoundLambda.Diagnostics.
            TypeSymbol returnType = result.InferredReturnType(ref useSiteDiagnostics) ?? LambdaSymbol.InferenceFailureReturnType;
            lambdaSymbol.SetInferredReturnType(result.RefKind, returnType);

            return result;
        }

        public BoundLambda BindForReturnTypeInference(NamedTypeSymbol delegateType)
        {
            var cacheKey = ReturnInferenceCacheKey.Create(delegateType, IsAsync);

            BoundLambda result;
            if (!_returnInferenceCache.TryGetValue(cacheKey, out result))
            {
                result = ReallyInferReturnType(delegateType, cacheKey.ParameterTypes, cacheKey.ParameterRefKinds);
                _returnInferenceCache.TryAdd(cacheKey, result);
            }

            return result;
        }

        /// <summary>
        /// Behavior of this key should be kept aligned with <see cref="BoundLambda.InferReturnType"/>.
        /// </summary>
        internal class ReturnInferenceCacheKey
        {
            public readonly ImmutableArray<TypeSymbol> ParameterTypes;
            public readonly ImmutableArray<RefKind> ParameterRefKinds;
            public readonly NamedTypeSymbol TaskLikeReturnTypeOpt;

            public static readonly ReturnInferenceCacheKey Empty = new ReturnInferenceCacheKey(ImmutableArray<TypeSymbol>.Empty, ImmutableArray<RefKind>.Empty, null);

            private ReturnInferenceCacheKey(ImmutableArray<TypeSymbol> parameterTypes, ImmutableArray<RefKind> parameterRefKinds, NamedTypeSymbol taskLikeReturnTypeOpt)
            {
                Debug.Assert(parameterTypes.Length == parameterRefKinds.Length);
                Debug.Assert((object)taskLikeReturnTypeOpt == null || ((object)taskLikeReturnTypeOpt == taskLikeReturnTypeOpt.ConstructedFrom && taskLikeReturnTypeOpt.IsCustomTaskType(out var builderArgument)));
                this.ParameterTypes = parameterTypes;
                this.ParameterRefKinds = parameterRefKinds;
                this.TaskLikeReturnTypeOpt = taskLikeReturnTypeOpt;
            }

            public override bool Equals(object obj)
            {
                if ((object)this == obj)
                {
                    return true;
                }

                var other = obj as ReturnInferenceCacheKey;

                return (object)other != null &&
                    other.ParameterTypes.SequenceEqual(this.ParameterTypes) &&
                    other.ParameterRefKinds.SequenceEqual(this.ParameterRefKinds) &&
                    other.TaskLikeReturnTypeOpt == this.TaskLikeReturnTypeOpt;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(TaskLikeReturnTypeOpt?.GetHashCode() ?? 0, Hash.CombineValues(this.ParameterTypes));
            }

            public static ReturnInferenceCacheKey Create(NamedTypeSymbol delegateType, bool isAsync)
            {
                // delegateType or DelegateInvokeMethod can be null in cases of malformed delegates
                // in such case we would want something trivial with no parameters
                var parameterTypes = ImmutableArray<TypeSymbol>.Empty;
                var parameterRefKinds = ImmutableArray<RefKind>.Empty;
                NamedTypeSymbol taskLikeReturnTypeOpt = null;
                MethodSymbol invoke = DelegateInvokeMethod(delegateType);
                if ((object)invoke != null)
                {
                    int parameterCount = invoke.ParameterCount;
                    if (parameterCount > 0)
                    {
                        var typesBuilder = ArrayBuilder<TypeSymbol>.GetInstance(parameterCount);
                        var refKindsBuilder = ArrayBuilder<RefKind>.GetInstance(parameterCount);

                        foreach (var p in invoke.Parameters)
                        {
                            refKindsBuilder.Add(p.RefKind);
                            typesBuilder.Add(p.Type);
                        }

                        parameterTypes = typesBuilder.ToImmutableAndFree();
                        parameterRefKinds = refKindsBuilder.ToImmutableAndFree();
                    }

                    if (isAsync)
                    {
                        var delegateReturnType = invoke.ReturnType as NamedTypeSymbol;
                        if ((object)delegateReturnType != null && delegateReturnType.SpecialType != SpecialType.System_Void)
                        {
                            if (delegateReturnType.IsCustomTaskType(out var builderType))
                            {
                                taskLikeReturnTypeOpt = delegateReturnType.ConstructedFrom;
                            }
                        }
                    }
                }

                if (parameterTypes.IsEmpty && parameterRefKinds.IsEmpty && (object)taskLikeReturnTypeOpt == null)
                {
                    return Empty;
                }

                return new ReturnInferenceCacheKey(parameterTypes, parameterRefKinds, taskLikeReturnTypeOpt);
            }
        }

        public TypeSymbol InferReturnType(NamedTypeSymbol delegateType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return BindForReturnTypeInference(delegateType).InferredReturnType(ref useSiteDiagnostics);
        }

        public virtual Binder ParameterBinder(LambdaSymbol lambdaSymbol, Binder binder)
        {
            return new WithLambdaParametersBinder(lambdaSymbol, binder);
        }
        // UNDONE: [MattWar]
        // UNDONE: Here we enable the consumer of an unbound lambda that could not be 
        // UNDONE: successfully converted to a best bound lambda to do error recovery 
        // UNDONE: by either picking an existing binding, or by binding the body using
        // UNDONE: error types for parameter types as necessary. This is not exactly
        // UNDONE: the strategy we discussed in the design meeting; rather there we
        // UNDONE: decided to do this more the way we did it in the native compiler:
        // UNDONE: there we wrote a post-processing pass that searched the tree for
        // UNDONE: unbound lambdas and did this sort of replacement on them, so that
        // UNDONE: we never observed an unbound lambda in the tree.
        // UNDONE:
        // UNDONE: I think that is a reasonable approach but it is not implemented yet.
        // UNDONE: When we figure out precisely where that rewriting pass should go, 
        // UNDONE: we can use the gear implemented in this method as an implementation
        // UNDONE: detail of it.

        public BoundLambda BindForErrorRecovery()
        {
            // It is possible that either (1) we never did a binding, because
            // we've got code like "var x = (z)=>{int y = 123; M(y, z);};" or 
            // (2) we did a bunch of bindings but none of them turned out to
            // be the one we wanted. In such a situation we still want 
            // IntelliSense to work on y in the body of the lambda, and 
            // possibly to make a good guess as to what M means even if we
            // don't know the type of z.

            if (_errorBinding == null)
            {
                Interlocked.CompareExchange(ref _errorBinding, ReallyBindForErrorRecovery(), null);
            }

            return _errorBinding;
        }

        private BoundLambda ReallyBindForErrorRecovery()
        {
            // If we have bindings, we can use heuristics to choose one.
            // If not, we can assign error types to all the parameters
            // and bind.

            return
                GuessBestBoundLambda(_bindingCache.Values)
                ?? GuessBestBoundLambda(_returnInferenceCache.Values)
                ?? ReallyInferReturnType(null, ImmutableArray<TypeSymbol>.Empty, ImmutableArray<RefKind>.Empty);
        }

        private static BoundLambda GuessBestBoundLambda(ICollection<BoundLambda> candidates)
        {
            switch (candidates.Count)
            {
                case 0:
                    return null;
                case 1:
                    return candidates.First();
                default:
                    // Prefer candidates with fewer diagnostics.
                    IEnumerable<BoundLambda> minDiagnosticsGroup = candidates.GroupBy(lambda => lambda.Diagnostics.Length).OrderBy(group => group.Key).First();

                    // If multiple candidates have the same number of diagnostics, order them by delegate type name.
                    // It's not great, but it should be stable.
                    return minDiagnosticsGroup
                        .OrderBy(lambda => GetLambdaSortString(lambda.Symbol))
                        .FirstOrDefault();
            }
        }

        private static string GetLambdaSortString(LambdaSymbol lambda)
        {
            var builder = PooledStringBuilder.GetInstance();

            foreach (var parameter in lambda.Parameters)
            {
                builder.Builder.Append(parameter.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            }

            if ((object)lambda.ReturnType != null)
            {
                builder.Builder.Append(lambda.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            var result = builder.ToStringAndFree();
            return result;
        }

        public bool GenerateSummaryErrors(DiagnosticBag diagnostics)
        {
            // It is highly likely that "the same" error will be given for two different
            // bindings of the same lambda but with different values for the parameters
            // of the error. For example, if we have x=>x.Blah() where x could be int
            // or string, then the two errors will be "int does not have member Blah" and 
            // "string does not have member Blah", but the locations and errors numbers
            // will be the same.
            //
            // We should first see if there is a set of errors that are "the same" by
            // this definition that occur in every lambda binding; if there are then
            // those are the errors we should report.
            //
            // If there are no errors that are common to *every* binding then we
            // can report the complete set of errors produced by every binding. However,
            // we still wish to avoid duplicates, so we will use the same logic for
            // building the union as the intersection; two errors with the same code
            // and location are to be treated as the same error and only reported once,
            // regardless of how that error is parameterized.
            //
            // The question then rears its head: when given two of "the same" error
            // to report that are nevertheless different in their arguments, which one
            // do we choose? To the user it hardly matters; either one points to the
            // right location in source code. But it surely matters to our testing team;
            // we do not want to be in a position where some small change to our internal
            // representation of lambdas causes tests to break because errors are reported
            // differently.
            //
            // What we need to do is find a *repeatable* arbitrary way to choose between
            // two errors; we can for example simply take the one that is lower in alphabetical
            // order when converted to a string.

            var convBags = from boundLambda in _bindingCache.Values select boundLambda.Diagnostics;
            var retBags = from boundLambda in _returnInferenceCache.Values select boundLambda.Diagnostics;
            var allBags = convBags.Concat(retBags);

            FirstAmongEqualsSet<Diagnostic> intersection = null;
            foreach (ImmutableArray<Diagnostic> bag in allBags)
            {
                if (intersection == null)
                {
                    intersection = CreateFirstAmongEqualsSet(bag);
                }
                else
                {
                    intersection.IntersectWith(bag);
                }
            }

            if (intersection != null)
            {
                if (PreventsSuccessfulDelegateConversion(intersection))
                {
                    diagnostics.AddRange(intersection);
                    return true;
                }
            }

            FirstAmongEqualsSet<Diagnostic> union = null;

            foreach (ImmutableArray<Diagnostic> bag in allBags)
            {
                if (union == null)
                {
                    union = CreateFirstAmongEqualsSet(bag);
                }
                else
                {
                    union.UnionWith(bag);
                }
            }

            if (union != null)
            {
                if (PreventsSuccessfulDelegateConversion(union))
                {
                    diagnostics.AddRange(union);
                    return true;
                }
            }

            return false;
        }

        private static bool PreventsSuccessfulDelegateConversion(FirstAmongEqualsSet<Diagnostic> set)
        {
            foreach (var diagnostic in set)
            {
                if (ErrorFacts.PreventsSuccessfulDelegateConversion((ErrorCode)diagnostic.Code))
                {
                    return true;
                }
            }
            return false;
        }

        private static FirstAmongEqualsSet<Diagnostic> CreateFirstAmongEqualsSet(ImmutableArray<Diagnostic> bag)
        {
            // For the purposes of lambda error reporting we wish to compare 
            // diagnostics for equality only considering their code and location,
            // but not other factors such as the values supplied for the 
            // parameters of the diagnostic.
            return new FirstAmongEqualsSet<Diagnostic>(
                bag,
                CommonDiagnosticComparer.Instance,
                CanonicallyCompareDiagnostics);
        }

        /// <summary>
        /// What we need to do is find a *repeatable* arbitrary way to choose between
        /// two errors; we can for example simply take the one that is lower in alphabetical
        /// order when converted to a string.  As an optimization, we compare error codes
        /// first and skip string comparison if they differ.
        /// </summary>
        private static int CanonicallyCompareDiagnostics(Diagnostic x, Diagnostic y)
        {
            ErrorCode xCode = (ErrorCode)x.Code;
            ErrorCode yCode = (ErrorCode)y.Code;

            int codeCompare = xCode.CompareTo(yCode);

            // ToString fails for a diagnostic with an error code that does not prevent successful delegate conversion.
            // Also, the order doesn't matter, since all such diagnostics will be dropped.
            if (!ErrorFacts.PreventsSuccessfulDelegateConversion(xCode) || !ErrorFacts.PreventsSuccessfulDelegateConversion(yCode))
            {
                return codeCompare;
            }

            // Optimization: don't bother 
            return codeCompare == 0 ? string.CompareOrdinal(x.ToString(), y.ToString()) : codeCompare;
        }
    }

    internal class PlainUnboundLambdaState : UnboundLambdaState
    {
        private readonly ImmutableArray<string> _parameterNames;
        private readonly ImmutableArray<TypeSymbol> _parameterTypes;
        private readonly ImmutableArray<RefKind> _parameterRefKinds;
        private readonly bool _isAsync;

        internal PlainUnboundLambdaState(
            UnboundLambda unboundLambda,
            Binder binder,
            ImmutableArray<string> parameterNames,
            ImmutableArray<TypeSymbol> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            bool isAsync)
            : base(binder, unboundLambda)
        {
            _parameterNames = parameterNames;
            _parameterTypes = parameterTypes;
            _parameterRefKinds = parameterRefKinds;
            _isAsync = isAsync;
        }

        public override bool HasSignature { get { return !_parameterNames.IsDefault; } }

        public override bool HasExplicitlyTypedParameterList { get { return !_parameterTypes.IsDefault; } }

        public override int ParameterCount { get { return _parameterNames.IsDefault ? 0 : _parameterNames.Length; } }

        public override bool IsAsync { get { return _isAsync; } }

        public override MessageID MessageID { get { return this.UnboundLambda.Syntax.Kind() == SyntaxKind.AnonymousMethodExpression ? MessageID.IDS_AnonMethod : MessageID.IDS_Lambda; } }

        private CSharpSyntaxNode Body
        {
            get
            {
                return UnboundLambda.Syntax.AnonymousFunctionBody();
            }
        }

        public override Location ParameterLocation(int index)
        {
            Debug.Assert(HasSignature && 0 <= index && index < ParameterCount);
            var syntax = UnboundLambda.Syntax;
            switch (syntax.Kind())
            {
                default:
                case SyntaxKind.SimpleLambdaExpression:
                    return ((SimpleLambdaExpressionSyntax)syntax).Parameter.Identifier.GetLocation();
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ((ParenthesizedLambdaExpressionSyntax)syntax).ParameterList.Parameters[index].Identifier.GetLocation();
                case SyntaxKind.AnonymousMethodExpression:
                    return ((AnonymousMethodExpressionSyntax)syntax).ParameterList.Parameters[index].Identifier.GetLocation();
            }
        }


        private bool IsExpressionLambda { get { return Body.Kind() != SyntaxKind.Block; } }

        public override string ParameterName(int index)
        {
            Debug.Assert(!_parameterNames.IsDefault && 0 <= index && index < _parameterNames.Length);
            return _parameterNames[index];
        }

        public override RefKind RefKind(int index)
        {
            Debug.Assert(0 <= index && index < _parameterTypes.Length);
            return _parameterRefKinds.IsDefault ? Microsoft.CodeAnalysis.RefKind.None : _parameterRefKinds[index];
        }

        public override TypeSymbol ParameterType(int index)
        {
            Debug.Assert(this.HasExplicitlyTypedParameterList);
            Debug.Assert(0 <= index && index < _parameterTypes.Length);
            return _parameterTypes[index];
        }

        protected override BoundBlock BindLambdaBody(LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, DiagnosticBag diagnostics)
        {
            if (this.IsExpressionLambda)
            {
                return lambdaBodyBinder.BindLambdaExpressionAsBlock((ExpressionSyntax)this.Body, diagnostics);
            }
            else
            {
                return lambdaBodyBinder.BindEmbeddedBlock((BlockSyntax)this.Body, diagnostics);
            }
        }
    }
}
