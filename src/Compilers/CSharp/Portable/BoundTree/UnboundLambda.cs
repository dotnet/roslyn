// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        BoundBlock IBoundLambdaOrFunction.Body { get => this.Body; }
    }

    internal struct InferredLambdaReturnType
    {
        internal readonly int NumExpressions;
        internal readonly bool HadExpressionlessReturn;
        internal readonly RefKind RefKind;
        internal readonly TypeWithAnnotations TypeWithAnnotations;
        internal readonly ImmutableArray<DiagnosticInfo> UseSiteDiagnostics;

        internal InferredLambdaReturnType(
            int numExpressions,
            bool hadExpressionlessReturn,
            RefKind refKind,
            TypeWithAnnotations typeWithAnnotations,
            ImmutableArray<DiagnosticInfo> useSiteDiagnostics)
        {
            NumExpressions = numExpressions;
            HadExpressionlessReturn = hadExpressionlessReturn;
            RefKind = refKind;
            TypeWithAnnotations = typeWithAnnotations;
            UseSiteDiagnostics = useSiteDiagnostics;
        }
    }

    internal sealed partial class BoundLambda : IBoundLambdaOrFunction
    {
        public MessageID MessageID { get { return Syntax.Kind() == SyntaxKind.AnonymousMethodExpression ? MessageID.IDS_AnonMethod : MessageID.IDS_Lambda; } }

        internal InferredLambdaReturnType InferredReturnType { get; private set; }

        MethodSymbol IBoundLambdaOrFunction.Symbol { get { return Symbol; } }

        SyntaxNode IBoundLambdaOrFunction.Syntax { get { return Syntax; } }

        public BoundLambda(SyntaxNode syntax, UnboundLambda unboundLambda, BoundBlock body, ImmutableArray<Diagnostic> diagnostics, Binder binder, TypeSymbol delegateType, InferredLambdaReturnType inferredReturnType)
            : this(syntax, unboundLambda, (LambdaSymbol)binder.ContainingMemberOrLambda, body, diagnostics, binder, delegateType)
        {
            InferredReturnType = inferredReturnType;

            Debug.Assert(
                syntax.IsAnonymousFunction() ||                                                                 // lambda expressions
                syntax is ExpressionSyntax && LambdaUtilities.IsLambdaBody(syntax, allowReducedLambdas: true) || // query lambdas
                LambdaUtilities.IsQueryPairLambda(syntax)                                                       // "pair" lambdas in queries
            );
        }

        public TypeWithAnnotations GetInferredReturnType(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Nullability (and conversions) are ignored.
            return GetInferredReturnType(conversions: null, nullableState: null, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Infer return type. If `nullableState` is non-null, nullability is also inferred and `NullableWalker.Analyze`
        /// uses that state to set the inferred nullability of variables in the enclosing scope. `conversions` is
        /// only needed when nullability is inferred.
        /// </summary>
        public TypeWithAnnotations GetInferredReturnType(ConversionsBase conversions, NullableWalker.VariableState nullableState, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (!InferredReturnType.UseSiteDiagnostics.IsEmpty)
            {
                if (useSiteDiagnostics == null)
                {
                    useSiteDiagnostics = new HashSet<DiagnosticInfo>();
                }
                foreach (var info in InferredReturnType.UseSiteDiagnostics)
                {
                    useSiteDiagnostics.Add(info);
                }
            }
            if (nullableState == null)
            {
                return InferredReturnType.TypeWithAnnotations;
            }
            else
            {
                Debug.Assert(conversions != null);
                // Diagnostics from NullableWalker.Analyze can be dropped here since Analyze
                // will be called again from NullableWalker.ApplyConversion when the
                // BoundLambda is converted to an anonymous function.
                // https://github.com/dotnet/roslyn/issues/31752: Can we avoid generating extra
                // diagnostics? And is this exponential when there are nested lambdas?
                var returnTypes = ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>.GetInstance();
                var diagnostics = DiagnosticBag.GetInstance();
                var delegateType = Type.GetDelegateType();
                var compilation = Binder.Compilation;
                NullableWalker.Analyze(compilation,
                                       lambda: this,
                                       (Conversions)conversions,
                                       diagnostics,
                                       delegateInvokeMethodOpt: delegateType?.DelegateInvokeMethod,
                                       initialState: nullableState,
                                       analyzedNullabilityMapOpt: null,
                                       snapshotBuilderOpt: null,
                                       returnTypes);
                diagnostics.Free();
                var inferredReturnType = InferReturnType(returnTypes, node: this, compilation, conversions, delegateType, Symbol.IsAsync);
                returnTypes.Free();
                return inferredReturnType.TypeWithAnnotations;
            }
        }

        internal LambdaSymbol CreateLambdaSymbol(NamedTypeSymbol delegateType, Symbol containingSymbol) =>
            UnboundLambda.Data.CreateLambdaSymbol(delegateType, containingSymbol);

        internal LambdaSymbol CreateLambdaSymbol(
            Symbol containingSymbol,
            TypeWithAnnotations returnType,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            RefKind refKind)
            => UnboundLambda.Data.CreateLambdaSymbol(
                containingSymbol,
                returnType,
                diagnostics: null,
                parameterTypes,
                parameterRefKinds.IsDefault ? Enumerable.Repeat(RefKind.None, parameterTypes.Length).ToImmutableArray() : parameterRefKinds,
                refKind);

        /// <summary>
        /// Indicates the type of return statement with no expression. Used in InferReturnType.
        /// </summary>
        internal static readonly TypeSymbol NoReturnExpression = new UnsupportedMetadataTypeSymbol();

        /// <summary>
        /// Behavior of this function should be kept aligned with <see cref="UnboundLambdaState.ReturnInferenceCacheKey"/>.
        /// </summary>
        internal static InferredLambdaReturnType InferReturnType(ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)> returnTypes,
            BoundNode node, CSharpCompilation compilation, ConversionsBase conversions, TypeSymbol delegateType, bool isAsync)
        {
            var types = ArrayBuilder<(BoundExpression, TypeWithAnnotations)>.GetInstance();
            bool hasReturnWithoutArgument = false;
            RefKind refKind = RefKind.None;
            foreach (var (returnStatement, type) in returnTypes)
            {
                RefKind rk = returnStatement.RefKind;
                if (rk != RefKind.None)
                {
                    refKind = rk;
                }

                if ((object)type.Type == NoReturnExpression)
                {
                    hasReturnWithoutArgument = true;
                }
                else
                {
                    types.Add((returnStatement.ExpressionOpt, type));
                }
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var bestType = CalculateReturnType(compilation, conversions, delegateType, types, isAsync, node, ref useSiteDiagnostics);
            return new InferredLambdaReturnType(types.Count, hasReturnWithoutArgument, refKind, bestType, useSiteDiagnostics.AsImmutableOrEmpty());
        }

        private static TypeWithAnnotations CalculateReturnType(
            CSharpCompilation compilation,
            ConversionsBase conversions,
            TypeSymbol delegateType,
            ArrayBuilder<(BoundExpression, TypeWithAnnotations resultType)> returns,
            bool isAsync,
            BoundNode node,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            TypeWithAnnotations bestResultType;
            int n = returns.Count;
            switch (n)
            {
                case 0:
                    bestResultType = default;
                    break;
                case 1:
                    bestResultType = returns[0].resultType;
                    break;
                default:
                    // Need to handle ref returns. See https://github.com/dotnet/roslyn/issues/30432
                    if (conversions.IncludeNullability)
                    {
                        bestResultType = NullableWalker.BestTypeForLambdaReturns(returns, compilation, node, (Conversions)conversions);
                    }
                    else
                    {
                        var typesOnly = ArrayBuilder<TypeSymbol>.GetInstance(n);
                        foreach (var (_, resultType) in returns)
                        {
                            typesOnly.Add(resultType.Type);
                        }
                        var bestType = BestTypeInferrer.GetBestType(typesOnly, conversions, ref useSiteDiagnostics);
                        bestResultType = bestType is null ? default : TypeWithAnnotations.Create(bestType);
                        typesOnly.Free();
                    }
                    break;
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
            if ((object)delegateReturnType != null && !delegateReturnType.IsVoidType())
            {
                object builderType;
                if (delegateReturnType.IsCustomTaskType(out builderType))
                {
                    taskType = delegateReturnType.ConstructedFrom;
                }
            }

            if (n == 0)
            {
                // No return statements have expressions; use delegate InvokeMethod
                // or infer type Task if delegate type not available.
                var resultType = (object)taskType != null && taskType.Arity == 0 ?
                    taskType :
                    compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task);
                return TypeWithAnnotations.Create(resultType);
            }

            if (!bestResultType.HasType || bestResultType.IsVoidType())
            {
                // If the best type was 'void', ERR_CantReturnVoid is reported while binding the "return void"
                // statement(s).
                return default;
            }

            // Some non-void best type T was found; use delegate InvokeMethod
            // or infer type Task<T> if delegate type not available.
            var taskTypeT = (object)taskType != null && taskType.Arity == 1 ?
                taskType :
                compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T);
            return TypeWithAnnotations.Create(taskTypeT.Construct(ImmutableArray.Create(bestResultType)));
        }

        internal sealed class BlockReturns : BoundTreeWalker
        {
            private readonly ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)> _builder;

            private BlockReturns(ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)> builder)
            {
                _builder = builder;
            }

            public static void GetReturnTypes(ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)> builder, BoundBlock block)
            {
                var visitor = new BoundLambda.BlockReturns(builder);
                visitor.Visit(block);
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
                // Do not recurse into local functions; we don't want their returns.
                return null;
            }

            public override BoundNode VisitReturnStatement(BoundReturnStatement node)
            {
                var expression = node.ExpressionOpt;
                var type = (expression is null) ?
                    NoReturnExpression :
                    expression.Type?.SetUnknownNullabilityForReferenceTypes();
                _builder.Add((node, TypeWithAnnotations.Create(type)));
                return null;
            }
        }
    }

    internal partial class UnboundLambda
    {
        private readonly NullableWalker.VariableState _nullableState;

        public UnboundLambda(
            CSharpSyntaxNode syntax,
            Binder binder,
            ImmutableArray<RefKind> refKinds,
            ImmutableArray<TypeWithAnnotations> types,
            ImmutableArray<string> names,
            ImmutableArray<bool> discardsOpt,
            bool isAsync,
            bool hasErrors = false)
            : base(BoundKind.UnboundLambda, syntax, null, hasErrors || !types.IsDefault && types.Any(t => t.Type?.Kind == SymbolKind.ErrorType))
        {
            Debug.Assert(binder != null);
            Debug.Assert(syntax.IsAnonymousFunction());
            this.Data = new PlainUnboundLambdaState(this, binder, names, discardsOpt, types, refKinds, isAsync);
        }

        private UnboundLambda(UnboundLambda other, Binder binder, NullableWalker.VariableState nullableState) :
            base(BoundKind.UnboundLambda, other.Syntax, null, other.HasErrors)
        {
            this._nullableState = nullableState;
            this.Data = other.Data;
        }

        internal UnboundLambda WithNullableState(Binder binder, NullableWalker.VariableState nullableState)
        {
            return new UnboundLambda(this, binder, nullableState);
        }

        public MessageID MessageID { get { return Data.MessageID; } }

        public BoundLambda Bind(NamedTypeSymbol delegateType)
            => SuppressIfNeeded(Data.Bind(delegateType));

        public BoundLambda BindForErrorRecovery()
            => SuppressIfNeeded(Data.BindForErrorRecovery());

        public BoundLambda BindForReturnTypeInference(NamedTypeSymbol delegateType)
            => SuppressIfNeeded(Data.BindForReturnTypeInference(delegateType));

        private BoundLambda SuppressIfNeeded(BoundLambda lambda)
            => this.IsSuppressed ? (BoundLambda)lambda.WithSuppression() : lambda;

        public bool HasSignature { get { return Data.HasSignature; } }
        public bool HasExplicitlyTypedParameterList { get { return Data.HasExplicitlyTypedParameterList; } }
        public int ParameterCount { get { return Data.ParameterCount; } }
        public TypeWithAnnotations InferReturnType(ConversionsBase conversions, NamedTypeSymbol delegateType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            => BindForReturnTypeInference(delegateType).GetInferredReturnType(conversions, _nullableState?.Clone(), ref useSiteDiagnostics);

        public RefKind RefKind(int index) { return Data.RefKind(index); }
        public void GenerateAnonymousFunctionConversionError(DiagnosticBag diagnostics, TypeSymbol targetType) { Data.GenerateAnonymousFunctionConversionError(diagnostics, targetType); }
        public bool GenerateSummaryErrors(DiagnosticBag diagnostics) { return Data.GenerateSummaryErrors(diagnostics); }
        public bool IsAsync { get { return Data.IsAsync; } }
        public TypeWithAnnotations ParameterTypeWithAnnotations(int index) { return Data.ParameterTypeWithAnnotations(index); }
        public TypeSymbol ParameterType(int index) { return ParameterTypeWithAnnotations(index).Type; }
        public Location ParameterLocation(int index) { return Data.ParameterLocation(index); }
        public string ParameterName(int index) { return Data.ParameterName(index); }
        public bool ParameterIsDiscard(int index) { return Data.ParameterIsDiscard(index); }
    }

    internal abstract class UnboundLambdaState
    {
        private UnboundLambda _unboundLambda; // we would prefer this readonly, but we have an initialization cycle.
        internal readonly Binder Binder;

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/23582",
            Constraint = "Avoid " + nameof(ConcurrentDictionary<NamedTypeSymbol, BoundLambda>) + " which has a large default size, but this cache is normally small.")]
        private ImmutableDictionary<NamedTypeSymbol, BoundLambda> _bindingCache = ImmutableDictionary<NamedTypeSymbol, BoundLambda>.Empty.WithComparers(TypeSymbol.EqualsConsiderEverything);

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/23582",
            Constraint = "Avoid " + nameof(ConcurrentDictionary<ReturnInferenceCacheKey, BoundLambda>) + " which has a large default size, but this cache is normally small.")]
        private ImmutableDictionary<ReturnInferenceCacheKey, BoundLambda> _returnInferenceCache = ImmutableDictionary<ReturnInferenceCacheKey, BoundLambda>.Empty;

        private BoundLambda _errorBinding;

        public UnboundLambdaState(Binder binder, UnboundLambda unboundLambdaOpt)
        {
            Debug.Assert(binder != null);

            // might be initialized later (for query lambdas)
            _unboundLambda = unboundLambdaOpt;
            this.Binder = binder;
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
        public abstract bool ParameterIsDiscard(int index);
        public abstract bool HasSignature { get; }
        public abstract bool HasExplicitlyTypedParameterList { get; }
        public abstract int ParameterCount { get; }
        public abstract bool IsAsync { get; }
        public abstract Location ParameterLocation(int index);
        public abstract TypeWithAnnotations ParameterTypeWithAnnotations(int index);
        public abstract RefKind RefKind(int index);
        protected abstract BoundBlock BindLambdaBody(LambdaSymbol lambdaSymbol, Binder lambdaBodyBinder, DiagnosticBag diagnostics);

        public virtual void GenerateAnonymousFunctionConversionError(DiagnosticBag diagnostics, TypeSymbol targetType)
        {
            this.Binder.GenerateAnonymousFunctionConversionError(diagnostics, _unboundLambda.Syntax, _unboundLambda, targetType);
        }

        // Returns the inferred return type, or null if none can be inferred.
        public BoundLambda Bind(NamedTypeSymbol delegateType)
        {
            BoundLambda result;
            if (!_bindingCache.TryGetValue(delegateType, out result))
            {
                result = ReallyBind(delegateType);
                result = ImmutableInterlocked.GetOrAdd(ref _bindingCache, delegateType, result);
            }

            return result;
        }

        internal IEnumerable<TypeSymbol> InferredReturnTypes()
        {
            bool any = false;
            foreach (var lambda in _returnInferenceCache.Values)
            {
                var type = lambda.InferredReturnType.TypeWithAnnotations;
                if (type.HasType)
                {
                    any = true;
                    yield return type.Type;
                }
            }

            if (!any)
            {
                var type = BindForErrorRecovery().InferredReturnType.TypeWithAnnotations;
                if (type.HasType)
                {
                    yield return type.Type;
                }
            }
        }

        private static MethodSymbol DelegateInvokeMethod(NamedTypeSymbol delegateType)
        {
            return delegateType.GetDelegateType()?.DelegateInvokeMethod;
        }

        private TypeWithAnnotations DelegateReturnTypeWithAnnotations(MethodSymbol invokeMethod, out RefKind refKind)
        {
            if ((object)invokeMethod == null)
            {
                refKind = CodeAnalysis.RefKind.None;
                return default;
            }
            refKind = invokeMethod.RefKind;
            return invokeMethod.ReturnTypeWithAnnotations;
        }

        private bool DelegateNeedsReturn(MethodSymbol invokeMethod)
        {
            if ((object)invokeMethod == null || invokeMethod.ReturnsVoid)
            {
                return false;
            }

            if (IsAsync && invokeMethod.ReturnType.IsNonGenericTaskType(this.Binder.Compilation))
            {
                return false;
            }

            return true;
        }

        private BoundLambda ReallyBind(NamedTypeSymbol delegateType)
        {
            // When binding for real (not for return inference), we cannot reuse a body of a lambda
            // previously bound for return type inference because we have not converted the returned
            // expression(s) to the return type.

            LambdaSymbol lambdaSymbol = CreateLambdaSymbol(
                delegateType,
                Binder.ContainingMemberOrLambda,
                out MethodSymbol invokeMethod,
                out TypeWithAnnotations returnType,
                out DiagnosticBag diagnostics,
                out ReturnInferenceCacheKey cacheKey);

            CSharpCompilation compilation = Binder.Compilation;
            Binder lambdaBodyBinder = new ExecutableCodeBinder(_unboundLambda.Syntax, lambdaSymbol, ParameterBinder(lambdaSymbol, Binder));

            if (lambdaSymbol.RefKind == CodeAnalysis.RefKind.RefReadOnly)
            {
                compilation.EnsureIsReadOnlyAttributeExists(diagnostics, lambdaSymbol.DiagnosticLocation, modifyCompilation: false);
            }

            var lambdaParameters = lambdaSymbol.Parameters;
            ParameterHelpers.EnsureIsReadOnlyAttributeExists(compilation, lambdaParameters, diagnostics, modifyCompilation: false);

            if (returnType.HasType)
            {
                if (compilation.ShouldEmitNullableAttributes(lambdaSymbol) &&
                    returnType.NeedsNullableAttribute())
                {
                    compilation.EnsureNullableAttributeExists(diagnostics, lambdaSymbol.DiagnosticLocation, modifyCompilation: false);
                    // Note: we don't need to warn on annotations used in #nullable disable context for lambdas, as this is handled in binding already
                }
            }

            ParameterHelpers.EnsureNullableAttributeExists(compilation, lambdaSymbol, lambdaParameters, diagnostics, modifyCompilation: false);
            // Note: we don't need to warn on annotations used in #nullable disable context for lambdas, as this is handled in binding already

            BoundBlock block = BindLambdaBody(lambdaSymbol, lambdaBodyBinder, diagnostics);

            ((ExecutableCodeBinder)lambdaBodyBinder).ValidateIteratorMethods(diagnostics);
            ValidateUnsafeParameters(diagnostics, cacheKey.ParameterTypes);

            bool reachableEndpoint = ControlFlowPass.Analyze(compilation, lambdaSymbol, block, diagnostics);
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
                if (returnType.HasType && // Can be null if "delegateType" is not actually a delegate type.
                    !returnType.IsVoidType() &&
                    !returnType.Type.IsNonGenericTaskType(compilation) &&
                    !returnType.Type.IsGenericTaskType(compilation))
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

            var result = new BoundLambda(_unboundLambda.Syntax, _unboundLambda, block, diagnostics.ToReadOnlyAndFree(), lambdaBodyBinder, delegateType, inferredReturnType: default)
            { WasCompilerGenerated = _unboundLambda.WasCompilerGenerated };

            return result;
        }

        private LambdaSymbol CreateLambdaSymbol(NamedTypeSymbol delegateType, Symbol containingSymbol, out MethodSymbol invokeMethod, out TypeWithAnnotations returnType, out DiagnosticBag diagnostics, out ReturnInferenceCacheKey cacheKey)
        {
            invokeMethod = DelegateInvokeMethod(delegateType);
            returnType = DelegateReturnTypeWithAnnotations(invokeMethod, out RefKind refKind);
            diagnostics = DiagnosticBag.GetInstance();
            cacheKey = ReturnInferenceCacheKey.Create(delegateType, IsAsync);

            return CreateLambdaSymbol(containingSymbol, returnType, diagnostics, cacheKey.ParameterTypes, cacheKey.ParameterRefKinds, refKind);
        }

        internal LambdaSymbol CreateLambdaSymbol(
            Symbol containingSymbol,
            TypeWithAnnotations returnType,
            DiagnosticBag diagnostics,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            RefKind refKind)
            => new LambdaSymbol(
                Binder.Compilation,
                containingSymbol,
                _unboundLambda,
                parameterTypes,
                parameterRefKinds,
                refKind,
                returnType,
                diagnostics);

        internal LambdaSymbol CreateLambdaSymbol(NamedTypeSymbol delegateType, Symbol containingSymbol)
        {
            return CreateLambdaSymbol(delegateType, containingSymbol, out _, out _, out _, out _);
        }

        private void ValidateUnsafeParameters(DiagnosticBag diagnostics, ImmutableArray<TypeWithAnnotations> targetParameterTypes)
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
                    if (targetParameterTypes[i].Type.IsUnsafe())
                    {
                        this.Binder.ReportUnsafeIfNotAllowed(this.ParameterLocation(i), diagnostics);
                    }
                }
            }
        }

        private BoundLambda ReallyInferReturnType(
            NamedTypeSymbol delegateType,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds)
        {
            (var lambdaSymbol, var block, var lambdaBodyBinder, var diagnostics) = BindWithParameterAndReturnType(parameterTypes, parameterRefKinds, returnType: default);
            var returnTypes = ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>.GetInstance();
            BoundLambda.BlockReturns.GetReturnTypes(returnTypes, block);
            var inferredReturnType = BoundLambda.InferReturnType(returnTypes, _unboundLambda, lambdaBodyBinder.Compilation, lambdaBodyBinder.Conversions, delegateType, lambdaSymbol.IsAsync);
            var result = new BoundLambda(
                _unboundLambda.Syntax,
                _unboundLambda,
                block,
                diagnostics.ToReadOnlyAndFree(),
                lambdaBodyBinder,
                delegateType,
                inferredReturnType)
            { WasCompilerGenerated = _unboundLambda.WasCompilerGenerated };

            // TODO: Should InferredReturnType.UseSiteDiagnostics be merged into BoundLambda.Diagnostics?
            var returnType = inferredReturnType.TypeWithAnnotations;
            if (!returnType.HasType)
            {
                bool forErrorRecovery = delegateType is null;
                returnType = (forErrorRecovery && returnTypes.Count == 0)
                    ? TypeWithAnnotations.Create(this.Binder.Compilation.GetSpecialType(SpecialType.System_Void))
                    : TypeWithAnnotations.Create(LambdaSymbol.InferenceFailureReturnType);
            }

            lambdaSymbol.SetInferredReturnType(inferredReturnType.RefKind, returnType);
            returnTypes.Free();
            return result;
        }

        private (LambdaSymbol lambdaSymbol, BoundBlock block, ExecutableCodeBinder lambdaBodyBinder, DiagnosticBag diagnostics) BindWithDelegateAndReturnType(
            NamedTypeSymbol delegateType,
            TypeWithAnnotations returnType)
        {
            var cacheKey = ReturnInferenceCacheKey.Create(delegateType, IsAsync);
            return BindWithParameterAndReturnType(cacheKey.ParameterTypes, cacheKey.ParameterRefKinds, returnType);
        }

        private (LambdaSymbol lambdaSymbol, BoundBlock block, ExecutableCodeBinder lambdaBodyBinder, DiagnosticBag diagnostics) BindWithParameterAndReturnType(
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            TypeWithAnnotations returnType)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var lambdaSymbol = CreateLambdaSymbol(Binder.ContainingMemberOrLambda,
                                                  returnType,
                                                  diagnostics,
                                                  parameterTypes,
                                                  parameterRefKinds,
                                                  CodeAnalysis.RefKind.None);
            var lambdaBodyBinder = new ExecutableCodeBinder(_unboundLambda.Syntax, lambdaSymbol, ParameterBinder(lambdaSymbol, Binder));
            var block = BindLambdaBody(lambdaSymbol, lambdaBodyBinder, diagnostics);
            return (lambdaSymbol, block, lambdaBodyBinder, diagnostics);
        }
        public BoundLambda BindForReturnTypeInference(NamedTypeSymbol delegateType)
        {
            var cacheKey = ReturnInferenceCacheKey.Create(delegateType, IsAsync);

            BoundLambda result;
            if (!_returnInferenceCache.TryGetValue(cacheKey, out result))
            {
                result = ReallyInferReturnType(delegateType, cacheKey.ParameterTypes, cacheKey.ParameterRefKinds);
                result = ImmutableInterlocked.GetOrAdd(ref _returnInferenceCache, cacheKey, result);
            }

            return result;
        }

        /// <summary>
        /// Behavior of this key should be kept aligned with <see cref="BoundLambda.InferReturnType"/>.
        /// </summary>
        private sealed class ReturnInferenceCacheKey
        {
            public readonly ImmutableArray<TypeWithAnnotations> ParameterTypes;
            public readonly ImmutableArray<RefKind> ParameterRefKinds;
            public readonly NamedTypeSymbol TaskLikeReturnTypeOpt;

            public static readonly ReturnInferenceCacheKey Empty = new ReturnInferenceCacheKey(ImmutableArray<TypeWithAnnotations>.Empty, ImmutableArray<RefKind>.Empty, null);

            private ReturnInferenceCacheKey(ImmutableArray<TypeWithAnnotations> parameterTypes, ImmutableArray<RefKind> parameterRefKinds, NamedTypeSymbol taskLikeReturnTypeOpt)
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

                if ((object)other == null ||
                    other.ParameterTypes.Length != this.ParameterTypes.Length ||
                    !TypeSymbol.Equals(other.TaskLikeReturnTypeOpt, this.TaskLikeReturnTypeOpt, TypeCompareKind.ConsiderEverything2))
                {
                    return false;
                }

                for (int i = 0; i < this.ParameterTypes.Length; i++)
                {
                    if (!other.ParameterTypes[i].Equals(this.ParameterTypes[i], TypeCompareKind.ConsiderEverything) ||
                        other.ParameterRefKinds[i] != this.ParameterRefKinds[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                var value = TaskLikeReturnTypeOpt?.GetHashCode() ?? 0;
                foreach (var type in ParameterTypes)
                {
                    value = Hash.Combine(type.Type, value);
                }
                return value;
            }

            public static ReturnInferenceCacheKey Create(NamedTypeSymbol delegateType, bool isAsync)
            {
                // delegateType or DelegateInvokeMethod can be null in cases of malformed delegates
                // in such case we would want something trivial with no parameters
                var parameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
                var parameterRefKinds = ImmutableArray<RefKind>.Empty;
                NamedTypeSymbol taskLikeReturnTypeOpt = null;
                MethodSymbol invoke = DelegateInvokeMethod(delegateType);
                if ((object)invoke != null)
                {
                    int parameterCount = invoke.ParameterCount;
                    if (parameterCount > 0)
                    {
                        var typesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(parameterCount);
                        var refKindsBuilder = ArrayBuilder<RefKind>.GetInstance(parameterCount);

                        foreach (var p in invoke.Parameters)
                        {
                            refKindsBuilder.Add(p.RefKind);
                            typesBuilder.Add(p.TypeWithAnnotations);
                        }

                        parameterTypes = typesBuilder.ToImmutableAndFree();
                        parameterRefKinds = refKindsBuilder.ToImmutableAndFree();
                    }

                    if (isAsync)
                    {
                        var delegateReturnType = invoke.ReturnType as NamedTypeSymbol;
                        if ((object)delegateReturnType != null && !delegateReturnType.IsVoidType())
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
        // UNDONE:
        // UNDONE: Note: that rewriting can now be done in BindToTypeForErrorRecovery.
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
                GuessBestBoundLambda(_bindingCache)
                ?? rebind(GuessBestBoundLambda(_returnInferenceCache))
                ?? rebind(ReallyInferReturnType(null, ImmutableArray<TypeWithAnnotations>.Empty, ImmutableArray<RefKind>.Empty));

            // Rebind a lambda to push target conversions through the return/result expressions
            BoundLambda rebind(BoundLambda lambda)
            {
                if (lambda is null)
                    return null;
                var delegateType = (NamedTypeSymbol)lambda.Type;
                InferredLambdaReturnType inferredReturnType = lambda.InferredReturnType;
                var returnType = inferredReturnType.TypeWithAnnotations;
                var refKind = lambda.Symbol.RefKind;
                if (!returnType.HasType)
                {
                    var invokeMethod = DelegateInvokeMethod(delegateType);
                    returnType = DelegateReturnTypeWithAnnotations(invokeMethod, out refKind);
                    if (!returnType.HasType || returnType.Type.ContainsTypeParameter())
                    {
                        var t = (inferredReturnType.HadExpressionlessReturn || inferredReturnType.NumExpressions == 0)
                            ? this.Binder.Compilation.GetSpecialType(SpecialType.System_Void)
                            : this.Binder.CreateErrorType();
                        returnType = TypeWithAnnotations.Create(t);
                        refKind = CodeAnalysis.RefKind.None;
                    }
                }

                (var lambdaSymbol, var block, var lambdaBodyBinder, var diagnostics) = BindWithDelegateAndReturnType(delegateType, returnType);
                return new BoundLambda(
                    _unboundLambda.Syntax,
                    _unboundLambda,
                    block,
                    diagnostics.ToReadOnlyAndFree(),
                    lambdaBodyBinder,
                    delegateType,
                    new InferredLambdaReturnType(inferredReturnType.NumExpressions, inferredReturnType.HadExpressionlessReturn, refKind, returnType, ImmutableArray<DiagnosticInfo>.Empty))
                { WasCompilerGenerated = _unboundLambda.WasCompilerGenerated };
            }
        }

        private static BoundLambda GuessBestBoundLambda<T>(ImmutableDictionary<T, BoundLambda> candidates)
        {
            switch (candidates.Count)
            {
                case 0:
                    return null;
                case 1:
                    return candidates.First().Value;
                default:
                    // Prefer candidates with fewer diagnostics.
                    IEnumerable<KeyValuePair<T, BoundLambda>> minDiagnosticsGroup = candidates.GroupBy(lambda => lambda.Value.Diagnostics.Length).OrderBy(group => group.Key).First();

                    // If multiple candidates have the same number of diagnostics, order them by delegate type name.
                    // It's not great, but it should be stable.
                    return minDiagnosticsGroup
                        .OrderBy(lambda => GetLambdaSortString(lambda.Value.Symbol))
                        .FirstOrDefault()
                        .Value;
            }
        }

        private static string GetLambdaSortString(LambdaSymbol lambda)
        {
            var builder = PooledStringBuilder.GetInstance();

            foreach (var parameter in lambda.Parameters)
            {
                builder.Builder.Append(parameter.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            }

            if (lambda.ReturnTypeWithAnnotations.HasType)
            {
                builder.Builder.Append(lambda.ReturnTypeWithAnnotations.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
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

            var convBags = from boundLambda in _bindingCache select boundLambda.Value.Diagnostics;
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
        /// two errors; we can for example simply take the one whose arguments are lower in alphabetical
        /// order when converted to a string.  As an optimization, we compare error codes
        /// first and skip string comparison if they differ.
        /// </summary>
        private static int CanonicallyCompareDiagnostics(Diagnostic x, Diagnostic y)
        {
            // Optimization: don't bother 
            if (x.Code != y.Code)
                return x.Code - y.Code;

            var nx = x.Arguments?.Count ?? 0;
            var ny = y.Arguments?.Count ?? 0;
            for (int i = 0, n = Math.Min(nx, ny); i < n; i++)
            {
                object argx = x.Arguments[i];
                object argy = y.Arguments[i];

                int argCompare = string.CompareOrdinal(argx?.ToString(), argy?.ToString());
                if (argCompare != 0)
                    return argCompare;
            }

            return nx - ny;
        }
    }

    internal class PlainUnboundLambdaState : UnboundLambdaState
    {
        private readonly ImmutableArray<string> _parameterNames;
        private readonly ImmutableArray<bool> _parameterIsDiscardOpt;
        private readonly ImmutableArray<TypeWithAnnotations> _parameterTypesWithAnnotations;
        private readonly ImmutableArray<RefKind> _parameterRefKinds;
        private readonly bool _isAsync;

        internal PlainUnboundLambdaState(
            UnboundLambda unboundLambda,
            Binder binder,
            ImmutableArray<string> parameterNames,
            ImmutableArray<bool> parameterIsDiscardOpt,
            ImmutableArray<TypeWithAnnotations> parameterTypesWithAnnotations,
            ImmutableArray<RefKind> parameterRefKinds,
            bool isAsync)
            : base(binder, unboundLambda)
        {
            _parameterNames = parameterNames;
            _parameterIsDiscardOpt = parameterIsDiscardOpt;
            _parameterTypesWithAnnotations = parameterTypesWithAnnotations;
            _parameterRefKinds = parameterRefKinds;
            _isAsync = isAsync;
        }

        internal bool HasNames { get { return !_parameterNames.IsDefault; } }

        internal bool HasTypes { get { return !_parameterTypesWithAnnotations.IsDefault; } }

        public override bool HasSignature { get { return !_parameterNames.IsDefault; } }

        public override bool HasExplicitlyTypedParameterList { get { return !_parameterTypesWithAnnotations.IsDefault; } }

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

        public override bool ParameterIsDiscard(int index)
        {
            return _parameterIsDiscardOpt.IsDefault ? false : _parameterIsDiscardOpt[index];
        }

        public override RefKind RefKind(int index)
        {
            Debug.Assert(0 <= index && index < _parameterTypesWithAnnotations.Length);
            return _parameterRefKinds.IsDefault ? Microsoft.CodeAnalysis.RefKind.None : _parameterRefKinds[index];
        }

        public override TypeWithAnnotations ParameterTypeWithAnnotations(int index)
        {
            Debug.Assert(this.HasExplicitlyTypedParameterList);
            Debug.Assert(0 <= index && index < _parameterTypesWithAnnotations.Length);
            return _parameterTypesWithAnnotations[index];
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
