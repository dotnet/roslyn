// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter : BoundTreeRewriterWithStackGuard
    {
        private readonly CSharpCompilation _compilation;
        private readonly SyntheticBoundNodeFactory _factory;
        private readonly SynthesizedSubmissionFields _previousSubmissionFields;
        private readonly bool _allowOmissionOfConditionalCalls;
        private LoweredDynamicOperationFactory _dynamicFactory;
        private bool _sawLambdas;
        private int _availableLocalFunctionOrdinal;
        private readonly int _topLevelMethodOrdinal;
        private DelegateCacheRewriter? _lazyDelegateCacheRewriter;
        private bool _inExpressionLambda;

        /// <summary>
        /// Additional locals that will be added to the outermost block of the current method, lambda,
        /// or local function. This is used for inline array temporaries where the scope of the
        /// temporary must be at least as wide as the scope of references to that temporary.
        /// </summary>
        private ArrayBuilder<LocalSymbol>? _additionalLocals;

        /// <summary>
        /// The original body of the current lambda or local function body, or null if not currently lowering a lambda.
        /// </summary>
        private BoundBlock? _currentLambdaBody;

        private bool _sawAwait;
        private bool _sawAwaitInExceptionHandler;
        private bool _needsSpilling;
        private readonly BindingDiagnosticBag _diagnostics;
        private readonly BoundStatement _rootStatement;

        private Dictionary<BoundValuePlaceholderBase, BoundExpression>? _placeholderReplacementMapDoNotUseDirectly;

        private LocalRewriter(
            CSharpCompilation compilation,
            MethodSymbol containingMethod,
            int containingMethodOrdinal,
            BoundStatement rootStatement,
            NamedTypeSymbol? containingType,
            SyntheticBoundNodeFactory factory,
            SynthesizedSubmissionFields previousSubmissionFields,
            bool allowOmissionOfConditionalCalls,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(factory.InstrumentationState != null);

            _compilation = compilation;
            _factory = factory;
            _factory.CurrentFunction = containingMethod;

            Debug.Assert(TypeSymbol.Equals(factory.CurrentType, (containingType ?? containingMethod.ContainingType), TypeCompareKind.ConsiderEverything2));
            _dynamicFactory = new LoweredDynamicOperationFactory(factory, containingMethodOrdinal);
            _previousSubmissionFields = previousSubmissionFields;
            _allowOmissionOfConditionalCalls = allowOmissionOfConditionalCalls;
            _topLevelMethodOrdinal = containingMethodOrdinal;
            _diagnostics = diagnostics;
            _rootStatement = rootStatement;
        }

        /// <summary>
        /// Lower a block of code by performing local rewritings.
        /// </summary>
        public static BoundStatement Rewrite(
            CSharpCompilation compilation,
            MethodSymbol method,
            int methodOrdinal,
            NamedTypeSymbol containingType,
            BoundStatement statement,
            TypeCompilationState compilationState,
            SynthesizedSubmissionFields previousSubmissionFields,
            bool allowOmissionOfConditionalCalls,
            MethodInstrumentation instrumentation,
            DebugDocumentProvider debugDocumentProvider,
            BindingDiagnosticBag diagnostics,
            out ImmutableArray<SourceSpan> codeCoverageSpans,
            out bool sawLambdas,
            out bool sawLocalFunctions,
            out bool sawAwaitInExceptionHandler)
        {
            Debug.Assert(statement != null);
            Debug.Assert(compilationState != null);

            try
            {
                var instrumentationState = new InstrumentationState();
                var factory = new SyntheticBoundNodeFactory(method, statement.Syntax, compilationState, diagnostics, instrumentationState);

                // create chain of instrumenters:

                var instrumenter = Instrumenter.NoOp;

                if (instrumentation.Kinds.Contains(InstrumentationKindExtensions.LocalStateTracing) &&
                    LocalStateTracingInstrumenter.TryCreate(method, statement, factory, diagnostics, instrumenter, out var localStateTracingInstrumenter))
                {
                    instrumenter = localStateTracingInstrumenter;
                }

                CodeCoverageInstrumenter? codeCoverageInstrumenter = null;
                if (instrumentation.Kinds.Contains(InstrumentationKind.TestCoverage) &&
                    CodeCoverageInstrumenter.TryCreate(method, statement, factory, diagnostics, debugDocumentProvider, instrumenter, out codeCoverageInstrumenter))
                {
                    instrumenter = codeCoverageInstrumenter;
                }

                StackOverflowProbingInstrumenter? stackOverflowProbingInstrumenter = null;
                if (instrumentation.Kinds.Contains(InstrumentationKind.StackOverflowProbing) &&
                    StackOverflowProbingInstrumenter.TryCreate(method, factory, instrumenter, out stackOverflowProbingInstrumenter))
                {
                    instrumenter = stackOverflowProbingInstrumenter;
                }

                ModuleCancellationInstrumenter? moduleCancellationInstrumenter = null;
                if (instrumentation.Kinds.Contains(InstrumentationKind.ModuleCancellation) &&
                    ModuleCancellationInstrumenter.TryCreate(method, factory, instrumenter, out moduleCancellationInstrumenter))
                {
                    instrumenter = moduleCancellationInstrumenter;
                }

                instrumentationState.Instrumenter = DebugInfoInjector.Create(instrumenter);

                // We don't want IL to differ based upon whether we write the PDB to a file/stream or not.
                // Presence of sequence points in the tree affects final IL, therefore, we always generate them.
                var localRewriter = new LocalRewriter(compilation, method, methodOrdinal, statement, containingType, factory, previousSubmissionFields, allowOmissionOfConditionalCalls, diagnostics);
                statement.CheckLocalsDefined();
                var loweredStatement = localRewriter.VisitStatement(statement);
                Debug.Assert(loweredStatement is { });
                // PROTOTYPE: Add _recursionDepth to LocalsScanner and avoid recursing in the same way we're currently doing in StackOptimizerPass1.
                //loweredStatement.CheckLocalsDefined();
                sawLambdas = localRewriter._sawLambdas;
                sawLocalFunctions = localRewriter._availableLocalFunctionOrdinal != 0;
                sawAwaitInExceptionHandler = localRewriter._sawAwaitInExceptionHandler;

                if (localRewriter._needsSpilling && !loweredStatement.HasErrors)
                {
                    // Move spill sequences to a top-level statement. This handles "lifting" await and the switch expression.
                    var spilledStatement = SpillSequenceSpiller.Rewrite(loweredStatement, method, compilationState, diagnostics);
                    // PROTOTYPE: Same comment as above.
                    //spilledStatement.CheckLocalsDefined();
                    loweredStatement = spilledStatement;
                }

                codeCoverageSpans = codeCoverageInstrumenter?.DynamicAnalysisSpans ?? ImmutableArray<SourceSpan>.Empty;
#if DEBUG
                // PROTOTYPE: Same comment as above.
                //LocalRewritingValidator.Validate(loweredStatement);
                localRewriter.AssertNoPlaceholderReplacements();
#endif
                return loweredStatement;
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                sawLambdas = sawLocalFunctions = sawAwaitInExceptionHandler = false;
                codeCoverageSpans = ImmutableArray<SourceSpan>.Empty;
                return new BoundBadStatement(statement.Syntax, ImmutableArray.Create<BoundNode>(statement), hasErrors: true);
            }
        }

        internal SyntheticBoundNodeFactory Factory
            => _factory;

        internal BoundBlock? CurrentLambdaBody
            => _currentLambdaBody;

        internal BoundStatement CurrentMethodBody
            => _rootStatement;

        private InstrumentationState InstrumentationState
            => _factory.InstrumentationState!;

        private bool Instrument
            => !InstrumentationState.IsSuppressed;

        private Instrumenter Instrumenter
            => InstrumentationState.Instrumenter;

        private PEModuleBuilder? EmitModule
        {
            get { return _factory.CompilationState.ModuleBuilderOpt; }
        }

        /// <summary>
        /// Return the translated node, or null if no code is necessary in the translation.
        /// </summary>
        public override BoundNode? Visit(BoundNode? node)
        {
            if (node == null)
            {
                return node;
            }
            Debug.Assert(!node.HasErrors, "nodes with errors should not be lowered");

            BoundExpression? expr = node as BoundExpression;
            if (expr != null)
            {
                return VisitExpressionImpl(expr);
            }

            return node.Accept(this);
        }

        [return: NotNullIfNotNull(nameof(node))]
        private BoundExpression? VisitExpression(BoundExpression? node)
        {
            if (node == null)
            {
                return node;
            }
            Debug.Assert(!node.HasErrors, "nodes with errors should not be lowered");

            // https://github.com/dotnet/roslyn/issues/47682
            return VisitExpressionImpl(node)!;
        }

        private BoundStatement? VisitStatement(BoundStatement? node)
        {
            if (node == null)
            {
                return node;
            }
            Debug.Assert(!node.HasErrors, "nodes with errors should not be lowered");

            return (BoundStatement?)node.Accept(this);
        }

        private BoundExpression? VisitExpressionImpl(BoundExpression node)
        {
            if (node is BoundNameOfOperator nameofOperator)
            {
                Debug.Assert(!nameofOperator.WasCompilerGenerated);
                var nameofIdentiferSyntax = (IdentifierNameSyntax)((InvocationExpressionSyntax)nameofOperator.Syntax).Expression;
                if (this._compilation.TryGetInterceptor(nameofIdentiferSyntax) is not null)
                {
                    this._diagnostics.Add(ErrorCode.ERR_InterceptorCannotInterceptNameof, nameofIdentiferSyntax.Location);
                }
            }

            ConstantValue? constantValue = node.ConstantValueOpt;
            if (constantValue != null)
            {
                TypeSymbol? type = node.Type;
                if (type?.IsNullableType() != true)
                {
                    var result = MakeLiteral(node.Syntax, constantValue, type);

                    if (node.WasCompilerGenerated)
                    {
                        result.MakeCompilerGenerated();
                    }

                    return result;
                }
            }

            var visited = VisitExpressionWithStackGuard(node);

            // If you *really* need to change the type, consider using an indirect method
            // like compound assignment does (extra flag only passed when it is an expression
            // statement means that this constraint is not violated).
            // Dynamic type will be erased in emit phase. It is considered equivalent to Object in lowered bound trees.
            // Unused deconstructions are lowered to produce a return value that isn't a tuple type.
            Debug.Assert(visited == null || visited.HasErrors || ReferenceEquals(visited.Type, node.Type) ||
                    visited.Type is { } && visited.Type.Equals(node.Type, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes) ||
                    IsUnusedDeconstruction(node));

            if (visited != null &&
                visited != node &&
                node.Kind is not (BoundKind.ImplicitReceiver or BoundKind.ObjectOrCollectionValuePlaceholder or BoundKind.ValuePlaceholder))
            {
                if (!CanBePassedByReference(node) && CanBePassedByReference(visited))
                {
                    visited = RefAccessMustMakeCopy(visited);
                }
            }

            return visited;
        }

        private static BoundExpression RefAccessMustMakeCopy(BoundExpression visited)
        {
            visited = new BoundPassByCopy(
                        visited.Syntax,
                        visited,
                        type: visited.Type);

            return visited;
        }

        private static bool IsUnusedDeconstruction(BoundExpression node)
        {
            return node.Kind == BoundKind.DeconstructionAssignmentOperator && !((BoundDeconstructionAssignmentOperator)node).IsUsed;
        }

        public override BoundNode? VisitParameter(BoundParameter node)
        {
            if (node.ParameterSymbol.ContainingSymbol is SynthesizedPrimaryConstructor primaryCtor &&
                primaryCtor.GetCapturedParameters().TryGetValue(node.ParameterSymbol, out var field))
            {
                Debug.Assert(CanBePassedByReference(node));
                var result = new BoundFieldAccess(node.Syntax, new BoundThisReference(node.Syntax, primaryCtor.ContainingType), field, ConstantValue.NotAvailable, LookupResultKind.Viable, node.Type);
                Debug.Assert(CanBePassedByReference(result));
                return result;
            }

            return base.VisitParameter(node);
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            Debug.Assert(_factory.ModuleBuilderOpt is { });

            var delegateType = node.Type.GetDelegateType();

            if (delegateType?.IsAnonymousType == true && delegateType.ContainingModule == _compilation.SourceModule &&
                delegateType.DelegateInvokeMethod() is MethodSymbol delegateInvoke &&
                delegateInvoke.Parameters.Any(static (p) => p.IsParamsCollection))
            {
                Location location;
                if (node.Symbol.Parameters.LastOrDefault(static (p) => p.IsParamsCollection) is { } parameter)
                {
                    location = ParameterHelpers.GetParameterLocation(parameter);
                }
                else
                {
                    location = node.Syntax.Location;
                }

                _factory.ModuleBuilderOpt.EnsureParamCollectionAttributeExists(_diagnostics, location);
            }

            _sawLambdas = true;

            var lambda = node.Symbol;
            CheckRefReadOnlySymbols(lambda);

            var oldContainingSymbol = _factory.CurrentFunction;
            var oldInstrumenter = InstrumentationState.Instrumenter;
            var oldLambdaBody = _currentLambdaBody;
            var oldAdditionalLocals = _additionalLocals;
            try
            {
                _currentLambdaBody = node.Body;
                _additionalLocals = null;

                _factory.CurrentFunction = lambda;
                if (lambda.IsDirectlyExcludedFromCodeCoverage)
                {
                    InstrumentationState.RemoveCodeCoverageInstrumenter();
                }

                return base.VisitLambda(node)!;
            }
            finally
            {
                _factory.CurrentFunction = oldContainingSymbol;
                InstrumentationState.Instrumenter = oldInstrumenter;
                _currentLambdaBody = oldLambdaBody;
                _additionalLocals = oldAdditionalLocals;
            }
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            int localFunctionOrdinal = _availableLocalFunctionOrdinal++;

            var localFunction = node.Symbol;
            CheckRefReadOnlySymbols(localFunction);

            if (_factory.CompilationState.ModuleBuilderOpt is { } moduleBuilder)
            {
                var typeParameters = localFunction.TypeParameters;
                if (typeParameters.Any(static typeParameter => typeParameter.HasUnmanagedTypeConstraint))
                {
                    moduleBuilder.EnsureIsUnmanagedAttributeExists();
                }

                if (_compilation.ShouldEmitNativeIntegerAttributes())
                {
                    if (hasReturnTypeOrParameter(localFunction, static t => t.ContainsNativeIntegerWrapperType()) ||
                        typeParameters.Any(static t => t.ConstraintTypesNoUseSiteDiagnostics.Any(static t => t.ContainsNativeIntegerWrapperType())))
                    {
                        moduleBuilder.EnsureNativeIntegerAttributeExists();
                    }
                }

                if (_factory.CompilationState.Compilation.ShouldEmitNullableAttributes(localFunction))
                {
                    bool constraintsNeedNullableAttribute = typeParameters.Any(
                       static typeParameter => ((SourceTypeParameterSymbolBase)typeParameter).ConstraintsNeedNullableAttribute());

                    if (constraintsNeedNullableAttribute || hasReturnTypeOrParameter(localFunction, static t => t.NeedsNullableAttribute()))
                    {
                        moduleBuilder.EnsureNullableAttributeExists();
                    }
                }

                static bool hasReturnTypeOrParameter(LocalFunctionSymbol localFunction, Func<TypeWithAnnotations, bool> predicate) =>
                    predicate(localFunction.ReturnTypeWithAnnotations) || localFunction.ParameterTypesWithAnnotations.Any(predicate);
            }

            var oldContainingSymbol = _factory.CurrentFunction;
            var oldInstrumenter = InstrumentationState.Instrumenter;
            var oldDynamicFactory = _dynamicFactory;
            var oldLambdaBody = _currentLambdaBody;
            var oldAdditionalLocals = _additionalLocals;
            try
            {
                _currentLambdaBody = node.Body;
                _additionalLocals = null;
                _factory.CurrentFunction = localFunction;

                if (localFunction.IsDirectlyExcludedFromCodeCoverage)
                {
                    InstrumentationState.RemoveCodeCoverageInstrumenter();
                }

                if (localFunction.IsGenericMethod)
                {
                    // Each generic local function gets its own dynamic factory because it 
                    // needs its own container to cache dynamic call-sites. That type (the container) "inherits"
                    // local function's type parameters as well as type parameters of all containing methods.
                    _dynamicFactory = new LoweredDynamicOperationFactory(_factory, _dynamicFactory.MethodOrdinal, localFunctionOrdinal);
                }

                return base.VisitLocalFunctionStatement(node)!;
            }
            finally
            {
                _factory.CurrentFunction = oldContainingSymbol;
                InstrumentationState.Instrumenter = oldInstrumenter;
                _dynamicFactory = oldDynamicFactory;
                _currentLambdaBody = oldLambdaBody;
                _additionalLocals = oldAdditionalLocals;
            }
        }

        public override BoundNode VisitDefaultLiteral(BoundDefaultLiteral node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitUnconvertedObjectCreationExpression(BoundUnconvertedObjectCreationExpression node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitValuePlaceholder(BoundValuePlaceholder node)
        {
            return PlaceholderReplacement(node);
        }

        public override BoundNode VisitDeconstructValuePlaceholder(BoundDeconstructValuePlaceholder node)
        {
            return PlaceholderReplacement(node);
        }

        public override BoundNode VisitObjectOrCollectionValuePlaceholder(BoundObjectOrCollectionValuePlaceholder node)
        {
            if (_inExpressionLambda)
            {
                // Expression trees do not include the 'this' argument for members.
                return node;
            }
            return PlaceholderReplacement(node);
        }

        public override BoundNode VisitInterpolatedStringArgumentPlaceholder(BoundInterpolatedStringArgumentPlaceholder node)
            => PlaceholderReplacement(node);

        public override BoundNode? VisitInterpolatedStringHandlerPlaceholder(BoundInterpolatedStringHandlerPlaceholder node)
            => PlaceholderReplacement(node);

        public override BoundNode? VisitCollectionExpressionSpreadExpressionPlaceholder(BoundCollectionExpressionSpreadExpressionPlaceholder node)
        {
            return PlaceholderReplacement(node);
        }

        /// <summary>
        /// Returns substitution currently used by the rewriter for a placeholder node.
        /// Each occurrence of the placeholder node is replaced with the node returned.
        /// Throws if there is no substitution.
        /// </summary>
        private BoundExpression PlaceholderReplacement(BoundValuePlaceholderBase placeholder)
        {
            Debug.Assert(_placeholderReplacementMapDoNotUseDirectly is { });
            var value = _placeholderReplacementMapDoNotUseDirectly[placeholder];
            AssertPlaceholderReplacement(placeholder, value);
            return value;
        }

        [Conditional("DEBUG")]
        private static void AssertPlaceholderReplacement(BoundValuePlaceholderBase placeholder, BoundExpression value)
        {
            Debug.Assert(value.Type is { } && (value.Type.Equals(placeholder.Type, TypeCompareKind.AllIgnoreOptions) || value.HasErrors));
        }

#if DEBUG
        [Conditional("DEBUG")]
        private void AssertNoPlaceholderReplacements()
        {
            if (_placeholderReplacementMapDoNotUseDirectly is not null)
            {
                Debug.Assert(_placeholderReplacementMapDoNotUseDirectly.Count == 0);
            }
        }
#endif

        /// <summary>
        /// Sets substitution used by the rewriter for a placeholder node.
        /// Each occurrence of the placeholder node is replaced with the node returned.
        /// Throws if there is already a substitution.
        /// </summary>
        private void AddPlaceholderReplacement(BoundValuePlaceholderBase placeholder, BoundExpression value)
        {
            AssertPlaceholderReplacement(placeholder, value);

            if (_placeholderReplacementMapDoNotUseDirectly is null)
            {
                _placeholderReplacementMapDoNotUseDirectly = new Dictionary<BoundValuePlaceholderBase, BoundExpression>();
            }

            _placeholderReplacementMapDoNotUseDirectly.Add(placeholder, value);
        }

        /// <summary>
        /// Removes substitution currently used by the rewriter for a placeholder node.
        /// Asserts if there isn't already a substitution.
        /// </summary>
        private void RemovePlaceholderReplacement(BoundValuePlaceholderBase placeholder)
        {
            Debug.Assert(placeholder is { });
            Debug.Assert(_placeholderReplacementMapDoNotUseDirectly is { });
            bool removed = _placeholderReplacementMapDoNotUseDirectly.Remove(placeholder);

            Debug.Assert(removed);
        }

        public sealed override BoundNode VisitOutDeconstructVarPendingInference(OutDeconstructVarPendingInference node)
        {
            // OutDeconstructVarPendingInference nodes are only used within initial binding, but don't survive past that stage
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitDeconstructionVariablePendingInference(DeconstructionVariablePendingInference node)
        {
            // DeconstructionVariablePendingInference nodes are only used within initial binding, but don't survive past that stage
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            // Cannot recurse into BadExpression children since the BadExpression
            // may represent being unable to use the child as an lvalue or rvalue.
            return node;
        }

        private static BoundExpression BadExpression(BoundExpression node)
        {
            Debug.Assert(node.Type is { });
            return BadExpression(node.Syntax, node.Type, ImmutableArray.Create(node));
        }

        private static BoundExpression BadExpression(SyntaxNode syntax, TypeSymbol resultType, BoundExpression child)
        {
            return BadExpression(syntax, resultType, ImmutableArray.Create(child));
        }

        private static BoundExpression BadExpression(SyntaxNode syntax, TypeSymbol resultType, BoundExpression child1, BoundExpression child2)
        {
            return BadExpression(syntax, resultType, ImmutableArray.Create(child1, child2));
        }

        private static BoundExpression BadExpression(SyntaxNode syntax, TypeSymbol resultType, ImmutableArray<BoundExpression> children)
        {
            return new BoundBadExpression(syntax, LookupResultKind.NotReferencable, ImmutableArray<Symbol?>.Empty, children, resultType);
        }

        private bool TryGetWellKnownTypeMember<TSymbol>(SyntaxNode? syntax, WellKnownMember member, [NotNullWhen(true)] out TSymbol? symbol, bool isOptional = false, Location? location = null) where TSymbol : Symbol
        {
            Debug.Assert((syntax != null) ^ (location != null));

            symbol = (TSymbol?)Binder.GetWellKnownTypeMember(_compilation, member, _diagnostics, syntax: syntax, isOptional: isOptional, location: location);
            return symbol is { };
        }

        /// <summary>
        /// This function provides a false sense of security, it is likely going to surprise you when the requested member is missing.
        /// Recommendation: Do not use, use <see cref="TryGetSpecialTypeMethod(SyntaxNode, SpecialMember, out MethodSymbol, bool)"/> instead!
        /// If used, a unit-test with a missing member is absolutely a must have.
        /// </summary>
        private MethodSymbol UnsafeGetSpecialTypeMethod(SyntaxNode syntax, SpecialMember specialMember)
        {
            return UnsafeGetSpecialTypeMethod(syntax, specialMember, _compilation, _diagnostics);
        }

        /// <summary>
        /// This function provides a false sense of security, it is likely going to surprise you when the requested member is missing.
        /// Recommendation: Do not use, use <see cref="TryGetSpecialTypeMethod(SyntaxNode, SpecialMember, CSharpCompilation, BindingDiagnosticBag, out MethodSymbol, bool)"/> instead!
        /// If used, a unit-test with a missing member is absolutely a must have.
        /// </summary>
        private static MethodSymbol UnsafeGetSpecialTypeMethod(SyntaxNode syntax, SpecialMember specialMember, CSharpCompilation compilation, BindingDiagnosticBag diagnostics)
        {
            MethodSymbol method;
            if (TryGetSpecialTypeMethod(syntax, specialMember, compilation, diagnostics, out method))
            {
                return method;
            }
            else
            {
                MemberDescriptor descriptor = SpecialMembers.GetDescriptor(specialMember);
                ExtendedSpecialType type = descriptor.DeclaringSpecialType;
                TypeSymbol container = compilation.Assembly.GetSpecialType(type);
                TypeSymbol returnType = new ExtendedErrorTypeSymbol(compilation: compilation, name: descriptor.Name, errorInfo: null, arity: descriptor.Arity);
                return new ErrorMethodSymbol(container, returnType, "Missing");
            }
        }

        private bool TryGetSpecialTypeMethod(SyntaxNode syntax, SpecialMember specialMember, out MethodSymbol method, bool isOptional = false)
        {
            return TryGetSpecialTypeMethod(syntax, specialMember, _compilation, _diagnostics, out method, isOptional);
        }

        private static bool TryGetSpecialTypeMethod(SyntaxNode syntax, SpecialMember specialMember, CSharpCompilation compilation, BindingDiagnosticBag diagnostics, out MethodSymbol method, bool isOptional = false)
        {
            return Binder.TryGetSpecialTypeMember(compilation, specialMember, syntax, diagnostics, out method, isOptional);
        }

        public override BoundNode VisitTypeOfOperator(BoundTypeOfOperator node)
        {
            Debug.Assert(node.Type.ExtendedSpecialType == InternalSpecialType.System_Type ||
                         TypeSymbol.Equals(node.Type, _compilation.GetWellKnownType(WellKnownType.System_Type), TypeCompareKind.AllIgnoreOptions));
            Debug.Assert(node.GetTypeFromHandle is null);

            var sourceType = (BoundTypeExpression?)this.Visit(node.SourceType);
            Debug.Assert(sourceType is { });
            var type = this.VisitType(node.Type);

            // Emit needs this helper
            MethodSymbol? getTypeFromHandle;
            bool tryGetResult;

            if (node.Type.ExtendedSpecialType == InternalSpecialType.System_Type)
            {
                tryGetResult = TryGetSpecialTypeMethod(node.Syntax, SpecialMember.System_Type__GetTypeFromHandle, out getTypeFromHandle);
            }
            else
            {
                tryGetResult = TryGetWellKnownTypeMember(node.Syntax, WellKnownMember.System_Type__GetTypeFromHandle, out getTypeFromHandle);
            }

            if (!tryGetResult)
            {
                return new BoundTypeOfOperator(node.Syntax, sourceType, null, type, hasErrors: true);
            }

            Debug.Assert(getTypeFromHandle is not null);
            Debug.Assert(TypeSymbol.Equals(type, getTypeFromHandle.ReturnType, TypeCompareKind.AllIgnoreOptions));
            return node.Update(sourceType, getTypeFromHandle, type);
        }

        public override BoundNode VisitRefTypeOperator(BoundRefTypeOperator node)
        {
            Debug.Assert(node.GetTypeFromHandle is null);

            var operand = this.VisitExpression(node.Operand);
            var type = this.VisitType(node.Type);

            // Emit needs this helper
            MethodSymbol? getTypeFromHandle;
            if (!TryGetWellKnownTypeMember(node.Syntax, WellKnownMember.System_Type__GetTypeFromHandle, out getTypeFromHandle))
            {
                return new BoundRefTypeOperator(node.Syntax, operand, null, type, hasErrors: true);
            }

            return node.Update(operand, getTypeFromHandle, type);
        }

        private BoundStatement? RewriteFieldOrPropertyInitializer(BoundStatement initializer)
        {
            // If _additionalLocals is null, this must be the outermost block of the current function.
            // If so, create a collection where child statements can insert inline array temporaries,
            // and add those temporaries to the generated block.
            var previousLocals = _additionalLocals;
            if (previousLocals is null)
            {
                _additionalLocals = ArrayBuilder<LocalSymbol>.GetInstance();
            }

            try
            {
                if (initializer.Kind == BoundKind.Block)
                {
                    var block = (BoundBlock)initializer;

                    var statement = RewriteExpressionStatement((BoundExpressionStatement)block.Statements.Single(), suppressInstrumentation: true);
                    Debug.Assert(statement is { });
                    var locals = block.Locals;
                    if (previousLocals is null)
                    {
                        locals = locals.AddRange(_additionalLocals!);
                    }
                    return block.Update(locals, block.LocalFunctions, block.HasUnsafeModifier, block.Instrumentation, ImmutableArray.Create(statement));
                }
                else
                {
                    var statement = RewriteExpressionStatement((BoundExpressionStatement)initializer, suppressInstrumentation: true);
                    if (statement is null || previousLocals is { } || _additionalLocals!.Count == 0)
                    {
                        return statement;
                    }
                    return new BoundBlock(
                        statement.Syntax,
                        _additionalLocals.ToImmutable(),
                        ImmutableArray.Create(statement));
                }
            }
            finally
            {
                if (previousLocals is null)
                {
                    _additionalLocals!.Free();
                    _additionalLocals = previousLocals;
                }
            }
        }

        public override BoundNode VisitTypeOrInstanceInitializers(BoundTypeOrInstanceInitializers node)
        {
            ImmutableArray<BoundStatement> originalStatements = node.Statements;
            var statements = ArrayBuilder<BoundStatement?>.GetInstance(node.Statements.Length);
            foreach (var initializer in originalStatements)
            {
                if (IsFieldOrPropertyInitializer(initializer))
                {
                    statements.Add(RewriteFieldOrPropertyInitializer(initializer));
                }
                else
                {
                    statements.Add(VisitStatement(initializer));
                }
            }

            int optimizedInitializers = 0;
            bool optimize = _compilation.Options.OptimizationLevel == OptimizationLevel.Release;

            for (int i = 0; i < statements.Count; i++)
            {
                var stmt = statements[i];
                if (stmt == null || (optimize && IsFieldOrPropertyInitializer(originalStatements[i]) && ShouldOptimizeOutInitializer(stmt)))
                {
                    optimizedInitializers++;
                    if (_factory.CurrentFunction?.IsStatic == false)
                    {
                        // NOTE: Dev11 removes static initializers if ONLY all of them are optimized out
                        statements[i] = null;
                    }
                }
            }

            ImmutableArray<BoundStatement> rewrittenStatements;

            if (optimizedInitializers == statements.Count)
            {
                // all are optimized away
                rewrittenStatements = ImmutableArray<BoundStatement>.Empty;
                statements.Free();
            }
            else
            {
                // instrument remaining statements
                int remaining = 0;
                for (int i = 0; i < statements.Count; i++)
                {
                    BoundStatement? rewritten = statements[i];

                    if (rewritten != null)
                    {
                        if (IsFieldOrPropertyInitializer(originalStatements[i]))
                        {
                            BoundStatement original = originalStatements[i];
                            if (Instrument && !original.WasCompilerGenerated)
                            {
                                rewritten = Instrumenter.InstrumentFieldOrPropertyInitializer(original, rewritten);
                            }
                        }

                        statements[remaining] = rewritten;
                        remaining++;
                    }
                }

                statements.Count = remaining; // trim any trailing nulls
                rewrittenStatements = statements.ToImmutableAndFree()!;
            }

            return new BoundStatementList(node.Syntax, rewrittenStatements, node.HasErrors);
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            // An array access expression can be indexed using any of the following types:
            //   * an integer primitive
            //   * a System.Index
            //   * a System.Range
            // The last two are only supported on SZArrays. For those cases we need to
            // lower into the appropriate helper methods.

            if (node.Indices.Length != 1)
            {
                return base.VisitArrayAccess(node)!;
            }

            var indexType = VisitType(node.Indices[0].Type);
            var F = _factory;

            BoundNode resultExpr;
            if (TypeSymbol.Equals(
                indexType,
                _compilation.GetWellKnownType(WellKnownType.System_Range),
                TypeCompareKind.ConsiderEverything))
            {
                // array[Range] is compiled to:
                // System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray(array, Range)

                Debug.Assert(node.Expression.Type is { TypeKind: TypeKind.Array });
                var elementType = ((ArrayTypeSymbol)node.Expression.Type).ElementTypeWithAnnotations;

                resultExpr = F.Call(
                    receiver: null,
                    F.WellKnownMethod(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__GetSubArray_T)
                        .Construct(ImmutableArray.Create(elementType)),
                    ImmutableArray.Create(
                        VisitExpression(node.Expression),
                        VisitExpression(node.Indices[0])));
            }
            else
            {
                resultExpr = base.VisitArrayAccess(node)!;
            }
            return resultExpr;
        }

        internal static bool IsFieldOrPropertyInitializer(BoundStatement initializer)
        {
            var syntax = initializer.Syntax;

            if (syntax.IsKind(SyntaxKind.Parameter))
            {
                // This is an initialization of a generated property based on record parameter.
                return true;
            }

            if (syntax is ExpressionSyntax { Parent: { } parent } && parent.Kind() == SyntaxKind.EqualsValueClause) // Should be the initial value.
            {
                Debug.Assert(parent.Parent is { });
                switch (parent.Parent.Kind())
                {
                    case SyntaxKind.VariableDeclarator:
                    case SyntaxKind.PropertyDeclaration:

                        switch (initializer.Kind)
                        {
                            case BoundKind.Block:
                                var block = (BoundBlock)initializer;
                                if (block.Statements.Length == 1)
                                {
                                    initializer = (BoundStatement)block.Statements.First();
                                    if (initializer.Kind == BoundKind.ExpressionStatement)
                                    {
                                        goto case BoundKind.ExpressionStatement;
                                    }
                                }
                                break;

                            case BoundKind.ExpressionStatement:
                                return ((BoundExpressionStatement)initializer).Expression.Kind == BoundKind.AssignmentOperator;

                        }
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the initializer is a field initializer which should be optimized out
        /// </summary>
        private static bool ShouldOptimizeOutInitializer(BoundStatement initializer)
        {
            BoundStatement statement = initializer;

            if (statement.Kind != BoundKind.ExpressionStatement)
            {
                return false;
            }

            BoundAssignmentOperator? assignment = ((BoundExpressionStatement)statement).Expression as BoundAssignmentOperator;
            if (assignment == null)
            {
                return false;
            }

            Debug.Assert(assignment.Left.Kind == BoundKind.FieldAccess);

            var lhsField = ((BoundFieldAccess)assignment.Left).FieldSymbol;
            if (!lhsField.IsStatic && lhsField.ContainingType.IsStructType())
            {
                return false;
            }

            BoundExpression rhs = assignment.Right;
            return rhs.IsDefaultValue();
        }

        // There are three situations in which the language permits passing rvalues by reference.
        // (technically there are 5, but we can ignore COM and dynamic here, since that results in byval semantics regardless of the parameter ref kind)
        //
        // #1: Receiver of a struct/generic method call.
        //
        // The language only requires that receivers of method calls must be readable (RValues are ok).
        //
        // However the underlying implementation passes receivers of struct methods by reference.
        // In such situations it may be possible for the call to cause or observe writes to the receiver variable.
        // As a result it is not valid to replace receiver variable with a reference to it or the other way around.
        //
        // Example1:
        //        static int x = 123;
        //        async static Task<string> Test1()
        //        {
        //            // cannot capture "x" by value, since write in M1 is observable
        //            return x.ToString(await M1());
        //        }
        //
        //        async static Task<string> M1()
        //        {
        //            x = 42;
        //            await Task.Yield();
        //            return "";
        //        }
        //
        // Example2:
        //        static int x = 123;
        //        static string Test1()
        //        {
        //            // cannot replace value of "x" with a reference to "x"
        //            // since that would make the method see the mutations in M1();
        //            return (x + 0).ToString(M1());
        //        }
        //
        //        static string M1()
        //        {
        //            x = 42;
        //            return "";
        //        }
        //
        // #2: Ordinary byval argument passed to an "in" parameter.
        //
        // The language only requires that ordinary byval arguments must be readable (RValues are ok).
        // However if the target parameter is an "in" parameter, the underlying implementation passes by reference.
        //
        // Example:
        //        static int x = 123;
        //        static void Main(string[] args)
        //        {
        //            // cannot replace value of "x" with a direct reference to x
        //            // since Test will see unexpected changes due to aliasing.
        //            Test(x + 0);
        //        }
        //
        //        static void Test(in int y)
        //        {
        //            Console.WriteLine(y);
        //            x = 42;
        //            Console.WriteLine(y);
        //        }
        //
        // #3: Ordinary byval interpolated string expression passed to a "ref" interpolated string handler value type.
        //
        // Interpolated string expressions passed to a builder type are lowered into a handler form. When the handler type
        // is a value type (struct, or type parameter constrained to struct (though the latter will fail to bind today because
        // there's no constructor)), the final handler instance type is passed by reference if the parameter is by reference.
        //
        // Example:
        //        M($""); // Language lowers this to a sequence of creating CustomHandler, appending all values, and evaluating to the builder
        //        static void M(ref CustomHandler c) { }
        //
        // NB: The readonliness is not considered here.
        //     We only care about possible introduction of aliasing. I.E. RValue->LValue change.
        //     Even if we start with a readonly variable, it cannot be lowered into a writeable one,
        //     with one exception - spilling of the value into a local, which is ok.
        //
        internal static bool CanBePassedByReference(BoundExpression expr)
        {
            if (expr.ConstantValueOpt != null)
            {
                return false;
            }

            switch (expr.Kind)
            {
                case BoundKind.Parameter:
                case BoundKind.Local:
                case BoundKind.ArrayAccess:
                case BoundKind.ThisReference:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PointerElementAccess:
                case BoundKind.RefValueOperator:
                case BoundKind.PseudoVariable:
                case BoundKind.DiscardExpression:
                    return true;

                case BoundKind.DeconstructValuePlaceholder:
                    // we will consider that placeholder always represents a temp local
                    // the assumption should be confirmed or changed when https://github.com/dotnet/roslyn/issues/24160 is fixed
                    return true;

                case BoundKind.InterpolatedStringArgumentPlaceholder:
                    // An argument placeholder is always a reference to some type of temp local,
                    // either representing a user-typed expression that went through this path
                    // itself when it was originally visited, or the trailing out parameter that
                    // is passed by out.
                    return true;

                case BoundKind.InterpolatedStringHandlerPlaceholder:
                    // A handler placeholder is the receiver of the interpolated string AppendLiteral
                    // or AppendFormatted calls, and should never be defensively copied.
                    return true;

                case BoundKind.CollectionExpressionSpreadExpressionPlaceholder:
                    // Used for Length or Count properties only which are effectively readonly.
                    return true;

                case BoundKind.EventAccess:
                    var eventAccess = (BoundEventAccess)expr;
                    if (eventAccess.IsUsableAsField)
                    {
                        if (eventAccess.EventSymbol.IsStatic)
                            return true;

                        Debug.Assert(eventAccess.ReceiverOpt is { });
                        Debug.Assert(eventAccess.ReceiverOpt.Type is { });
                        return !eventAccess.ReceiverOpt.Type.IsValueType || CanBePassedByReference(eventAccess.ReceiverOpt);
                    }

                    return false;

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    if (!fieldAccess.FieldSymbol.IsStatic)
                    {
                        Debug.Assert(fieldAccess.ReceiverOpt is { });
                        Debug.Assert(fieldAccess.ReceiverOpt.Type is { });
                        return !fieldAccess.ReceiverOpt.Type.IsValueType || CanBePassedByReference(fieldAccess.ReceiverOpt);
                    }

                    return true;

                case BoundKind.Sequence:
                    return CanBePassedByReference(((BoundSequence)expr).Value);

                case BoundKind.AssignmentOperator:
                    return ((BoundAssignmentOperator)expr).IsRef;

                case BoundKind.ConditionalOperator:
                    return ((BoundConditionalOperator)expr).IsRef;

                case BoundKind.Call:
                    return ((BoundCall)expr).Method.RefKind != RefKind.None;

                case BoundKind.PropertyAccess:
                    return ((BoundPropertyAccess)expr).PropertySymbol.RefKind != RefKind.None;

                case BoundKind.IndexerAccess:
                    return ((BoundIndexerAccess)expr).Indexer.RefKind != RefKind.None;

                case BoundKind.ImplicitIndexerAccess:
                    return CanBePassedByReference(((BoundImplicitIndexerAccess)expr).IndexerOrSliceAccess);

                case BoundKind.ImplicitIndexerReceiverPlaceholder:
                    // That placeholder is always replaced with a temp local
                    return true;

                case BoundKind.InlineArrayAccess:
                    return ((BoundInlineArrayAccess)expr) is { IsValue: false, GetItemOrSliceHelper: WellKnownMember.System_Span_T__get_Item or WellKnownMember.System_ReadOnlySpan_T__get_Item };

                case BoundKind.ImplicitIndexerValuePlaceholder:
                    // Implicit Index or Range indexers only have by-value parameters:
                    // this[int], Slice(int, int), Substring(int, int)
                    return false;

                case BoundKind.ListPatternReceiverPlaceholder:
                case BoundKind.SlicePatternReceiverPlaceholder:
                case BoundKind.SlicePatternRangePlaceholder:
                case BoundKind.ListPatternIndexPlaceholder:
                    throw ExceptionUtilities.UnexpectedValue(expr.Kind);

                case BoundKind.Conversion:
                    return expr is BoundConversion { Conversion: { IsInterpolatedStringHandler: true }, Type: { IsValueType: true } };
            }

            Debug.Assert(expr is not BoundValuePlaceholderBase, $"Placeholder kind {expr.Kind} must be handled explicitly");

            return false;
        }

        private void CheckRefReadOnlySymbols(MethodSymbol symbol)
        {
            if (symbol.ReturnsByRefReadonly ||
                symbol.Parameters.Any(static p => p.RefKind == RefKind.In))
            {
                _factory.CompilationState.ModuleBuilderOpt?.EnsureIsReadOnlyAttributeExists();
            }
        }

        private CompoundUseSiteInfo<AssemblySymbol> GetNewCompoundUseSiteInfo()
        {
            return new CompoundUseSiteInfo<AssemblySymbol>(_diagnostics, _compilation.Assembly);
        }

#if DEBUG
        /// <summary>
        /// Note: do not use a static/singleton instance of this type, as it holds state.
        /// Consider generating this type from BoundNodes.xml for easier maintenance.
        /// </summary>
        private sealed class LocalRewritingValidator : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            public override BoundNode? Visit(BoundNode? node)
            {
                if (node is BoundIfStatement)
                {
                    Fail(node);
                    return null;
                }

                return base.Visit(node);
            }

            /// <summary>
            /// Asserts that no unexpected nodes survived local rewriting.
            /// </summary>
            public static void Validate(BoundNode node)
            {
                try
                {
                    new LocalRewritingValidator().Visit(node);
                }
                catch (InsufficientExecutionStackException)
                {
                    // Intentionally ignored to let the overflow get caught in a more crucial visitor
                }
            }

            public override BoundNode? VisitDefaultLiteral(BoundDefaultLiteral node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitUsingStatement(BoundUsingStatement node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitDeconstructionVariablePendingInference(DeconstructionVariablePendingInference node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitValuePlaceholder(BoundValuePlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitDeconstructValuePlaceholder(BoundDeconstructValuePlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitDisposableValuePlaceholder(BoundDisposableValuePlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitImplicitIndexerValuePlaceholder(BoundImplicitIndexerValuePlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitImplicitIndexerReceiverPlaceholder(BoundImplicitIndexerReceiverPlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitListPatternIndexPlaceholder(BoundListPatternIndexPlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitListPatternReceiverPlaceholder(BoundListPatternReceiverPlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitSlicePatternRangePlaceholder(BoundSlicePatternRangePlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitSlicePatternReceiverPlaceholder(BoundSlicePatternReceiverPlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitInterpolatedStringArgumentPlaceholder(BoundInterpolatedStringArgumentPlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode? VisitInterpolatedStringHandlerPlaceholder(BoundInterpolatedStringHandlerPlaceholder node)
            {
                Fail(node);
                return null;
            }

            private void Fail(BoundNode node)
            {
                Debug.Assert(false, $"Bound nodes of kind {node.Kind} should not survive past local rewriting");
            }
        }
#endif
    }
}
