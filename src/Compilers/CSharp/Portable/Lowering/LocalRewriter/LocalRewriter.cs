// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
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
        private readonly LoweredDynamicOperationFactory _dynamicFactory;
        private bool _sawLambdas;
        private bool _sawLocalFunctions;
        private bool _inExpressionLambda;

        private bool _sawAwait;
        private bool _sawAwaitInExceptionHandler;
        private readonly DiagnosticBag _diagnostics;
        private readonly Instrumenter _instrumenter;
        private readonly BoundStatement _rootStatement;

        private Dictionary<BoundValuePlaceholderBase, BoundExpression> _placeholderReplacementMapDoNotUseDirectly;

        private LocalRewriter(
            CSharpCompilation compilation,
            MethodSymbol containingMethod,
            int containingMethodOrdinal,
            BoundStatement rootStatement,
            NamedTypeSymbol containingType,
            SyntheticBoundNodeFactory factory,
            SynthesizedSubmissionFields previousSubmissionFields,
            bool allowOmissionOfConditionalCalls,
            DiagnosticBag diagnostics,
            Instrumenter instrumenter)
        {
            _compilation = compilation;
            _factory = factory;
            _factory.CurrentFunction = containingMethod;
            Debug.Assert(TypeSymbol.Equals(factory.CurrentType, (containingType ?? containingMethod.ContainingType), TypeCompareKind.ConsiderEverything2));
            _dynamicFactory = new LoweredDynamicOperationFactory(factory, containingMethodOrdinal);
            _previousSubmissionFields = previousSubmissionFields;
            _allowOmissionOfConditionalCalls = allowOmissionOfConditionalCalls;
            _diagnostics = diagnostics;

            Debug.Assert(instrumenter != null);
            _instrumenter = instrumenter;
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
            bool instrumentForDynamicAnalysis,
            ref ImmutableArray<SourceSpan> dynamicAnalysisSpans,
            DebugDocumentProvider debugDocumentProvider,
            DiagnosticBag diagnostics,
            out bool sawLambdas,
            out bool sawLocalFunctions,
            out bool sawAwaitInExceptionHandler)
        {
            Debug.Assert(statement != null);
            Debug.Assert(compilationState != null);

            try
            {
                var factory = new SyntheticBoundNodeFactory(method, statement.Syntax, compilationState, diagnostics);
                DynamicAnalysisInjector dynamicInstrumenter = instrumentForDynamicAnalysis ? DynamicAnalysisInjector.TryCreate(method, statement, factory, diagnostics, debugDocumentProvider, Instrumenter.NoOp) : null;

                // We don’t want IL to differ based upon whether we write the PDB to a file/stream or not.
                // Presence of sequence points in the tree affects final IL, therefore, we always generate them.
                var localRewriter = new LocalRewriter(compilation, method, methodOrdinal, statement, containingType, factory, previousSubmissionFields, allowOmissionOfConditionalCalls, diagnostics,
                                                      dynamicInstrumenter != null ? new DebugInfoInjector(dynamicInstrumenter) : DebugInfoInjector.Singleton);

                var loweredStatement = (BoundStatement)localRewriter.Visit(statement);
                sawLambdas = localRewriter._sawLambdas;
                sawLocalFunctions = localRewriter._sawLocalFunctions;
                sawAwaitInExceptionHandler = localRewriter._sawAwaitInExceptionHandler;
                if (dynamicInstrumenter != null)
                {
                    dynamicAnalysisSpans = dynamicInstrumenter.DynamicAnalysisSpans;
                }
#if DEBUG
                LocalRewritingValidator.Validate(loweredStatement);
#endif
                return loweredStatement;
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                sawLambdas = sawLocalFunctions = sawAwaitInExceptionHandler = false;
                return new BoundBadStatement(statement.Syntax, ImmutableArray.Create<BoundNode>(statement), hasErrors: true);
            }
        }

        private bool Instrument
        {
            get
            {
                return !_inExpressionLambda;
            }
        }

        private PEModuleBuilder EmitModule
        {
            get { return _factory.CompilationState.ModuleBuilderOpt; }
        }

        public override BoundNode Visit(BoundNode node)
        {
            if (node == null)
            {
                return node;
            }
            Debug.Assert(!node.HasErrors, "nodes with errors should not be lowered");

            BoundExpression expr = node as BoundExpression;
            if (expr != null)
            {
                return VisitExpressionImpl(expr);
            }

            return node.Accept(this);
        }

        private BoundExpression VisitExpression(BoundExpression node)
        {
            if (node == null)
            {
                return node;
            }
            Debug.Assert(!node.HasErrors, "nodes with errors should not be lowered");

            return VisitExpressionImpl(node);
        }

        private BoundStatement VisitStatement(BoundStatement node)
        {
            if (node == null)
            {
                return node;
            }
            Debug.Assert(!node.HasErrors, "nodes with errors should not be lowered");

            return (BoundStatement)node.Accept(this);
        }

        private BoundExpression VisitExpressionImpl(BoundExpression node)
        {
            ConstantValue constantValue = node.ConstantValue;
            if (constantValue != null)
            {
                TypeSymbol type = node.Type;
                if (type?.IsNullableType() != true)
                {
                    return MakeLiteral(node.Syntax, constantValue, type);
                }
            }

            var visited = VisitExpressionWithStackGuard(node);

            // If you *really* need to change the type, consider using an indirect method
            // like compound assignment does (extra flag only passed when it is an expression
            // statement means that this constraint is not violated).
            // Dynamic type will be erased in emit phase. It is considered equivalent to Object in lowered bound trees.
            // Unused deconstructions are lowered to produce a return value that isn't a tuple type.
            Debug.Assert(visited == null || visited.HasErrors || ReferenceEquals(visited.Type, node.Type) ||
                    visited.Type.Equals(node.Type, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes) ||
                    IsUnusedDeconstruction(node));

            if (visited != null && visited != node)
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

        public override BoundNode VisitLambda(BoundLambda node)
        {
            _sawLambdas = true;
            CheckRefReadOnlySymbols(node.Symbol);

            var oldContainingSymbol = _factory.CurrentFunction;
            try
            {
                _factory.CurrentFunction = node.Symbol;
                return base.VisitLambda(node);
            }
            finally
            {
                _factory.CurrentFunction = oldContainingSymbol;
            }
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            _sawLocalFunctions = true;
            CheckRefReadOnlySymbols(node.Symbol);

            var typeParameters = node.Symbol.TypeParameters;
            if (typeParameters.Any(typeParameter => typeParameter.HasUnmanagedTypeConstraint))
            {
                _factory.CompilationState.ModuleBuilderOpt?.EnsureIsUnmanagedAttributeExists();
            }

            bool constraintsNeedNullableAttribute = typeParameters.Any(
               typeParameter => (typeParameter.HasReferenceTypeConstraint && typeParameter.ReferenceTypeConstraintIsNullable != null) ||
                                typeParameter.ConstraintTypesNoUseSiteDiagnostics.Any(
                                    typeConstraint => typeConstraint.NeedsNullableAttribute()));

            bool returnTypeNeedsNullableAttribute = node.Symbol.ReturnType.NeedsNullableAttribute();
            bool parametersNeedNullableAttribute = node.Symbol.ParameterTypes.Any(parameter => parameter.NeedsNullableAttribute());

            if (constraintsNeedNullableAttribute || returnTypeNeedsNullableAttribute || parametersNeedNullableAttribute)
            {
                _factory.CompilationState.ModuleBuilderOpt?.EnsureNullableAttributeExists();
            }

            var oldContainingSymbol = _factory.CurrentFunction;
            try
            {
                _factory.CurrentFunction = node.Symbol;
                return base.VisitLocalFunctionStatement(node);
            }
            finally
            {
                _factory.CurrentFunction = oldContainingSymbol;
            }
        }

        public override BoundNode VisitDeconstructValuePlaceholder(BoundDeconstructValuePlaceholder node)
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
            var value = _placeholderReplacementMapDoNotUseDirectly[placeholder];
            AssertPlaceholderReplacement(placeholder, value);
            return value;
        }

        [Conditional("DEBUG")]
        private static void AssertPlaceholderReplacement(BoundValuePlaceholderBase placeholder, BoundExpression value)
        {
            Debug.Assert(value.Type.Equals(placeholder.Type, TypeCompareKind.AllIgnoreOptions));
        }

        /// <summary>
        /// Sets substitution used by the rewriter for a placeholder node.
        /// Each occurrence of the placeholder node is replaced with the node returned.
        /// Throws if there is already a substitution.
        /// </summary>
        private void AddPlaceholderReplacement(BoundValuePlaceholderBase placeholder, BoundExpression value)
        {
            AssertPlaceholderReplacement(placeholder, value);

            if ((object)_placeholderReplacementMapDoNotUseDirectly == null)
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
            Debug.Assert((object)placeholder != null);
            bool removed = _placeholderReplacementMapDoNotUseDirectly.Remove(placeholder);

            Debug.Assert(removed);
        }

        public override sealed BoundNode VisitOutDeconstructVarPendingInference(OutDeconstructVarPendingInference node)
        {
            // OutDeconstructVarPendingInference nodes are only used within initial binding, but don't survive past that stage
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitDeconstructionVariablePendingInference(DeconstructionVariablePendingInference node)
        {
            // DeconstructionVariablePendingInference nodes are only used within initial binding, but don't survive past that stage
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            // Cannot recurse into BadExpression children since the BadExpression
            // may represent being unable to use the child as an lvalue or rvalue.
            return node;
        }

        private static BoundExpression BadExpression(BoundExpression node)
        {
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
            return new BoundBadExpression(syntax, LookupResultKind.NotReferencable, ImmutableArray<Symbol>.Empty, children, resultType);
        }

        private bool TryGetWellKnownTypeMember<TSymbol>(SyntaxNode syntax, WellKnownMember member, out TSymbol symbol, bool isOptional = false, Location location = null) where TSymbol : Symbol
        {
            Debug.Assert((syntax != null) ^ (location != null));

            symbol = (TSymbol)Binder.GetWellKnownTypeMember(_compilation, member, _diagnostics, syntax: syntax, isOptional: isOptional, location: location);
            return ((object)symbol != null);
        }

        /// <summary>
        /// This function provides a false sense of security, it is likely going to surprise you when the requested member is missing.
        /// Recommendation: Do not use, use <see cref="TryGetSpecialTypeMethod(SyntaxNode, SpecialMember, out MethodSymbol)"/> instead!
        /// If used, a unit-test with a missing member is absolutely a must have.
        /// </summary>
        private MethodSymbol UnsafeGetSpecialTypeMethod(SyntaxNode syntax, SpecialMember specialMember)
        {
            return UnsafeGetSpecialTypeMethod(syntax, specialMember, _compilation, _diagnostics);
        }

        /// <summary>
        /// This function provides a false sense of security, it is likely going to surprise you when the requested member is missing.
        /// Recommendation: Do not use, use <see cref="TryGetSpecialTypeMethod(SyntaxNode, SpecialMember, CSharpCompilation, DiagnosticBag, out MethodSymbol)"/> instead!
        /// If used, a unit-test with a missing member is absolutely a must have.
        /// </summary>
        private static MethodSymbol UnsafeGetSpecialTypeMethod(SyntaxNode syntax, SpecialMember specialMember, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            MethodSymbol method;
            if (TryGetSpecialTypeMethod(syntax, specialMember, compilation, diagnostics, out method))
            {
                return method;
            }
            else
            {
                MemberDescriptor descriptor = SpecialMembers.GetDescriptor(specialMember);
                SpecialType type = (SpecialType)descriptor.DeclaringTypeId;
                TypeSymbol container = compilation.Assembly.GetSpecialType(type);
                TypeSymbol returnType = new ExtendedErrorTypeSymbol(compilation: compilation, name: descriptor.Name, errorInfo: null, arity: descriptor.Arity);
                return new ErrorMethodSymbol(container, returnType, "Missing");
            }
        }

        private bool TryGetSpecialTypeMethod(SyntaxNode syntax, SpecialMember specialMember, out MethodSymbol method)
        {
            return TryGetSpecialTypeMethod(syntax, specialMember, _compilation, _diagnostics, out method);
        }

        private static bool TryGetSpecialTypeMethod(SyntaxNode syntax, SpecialMember specialMember, CSharpCompilation compilation, DiagnosticBag diagnostics, out MethodSymbol method)
        {
            return Binder.TryGetSpecialTypeMember(compilation, specialMember, syntax, diagnostics, out method);
        }

        public override BoundNode VisitTypeOfOperator(BoundTypeOfOperator node)
        {
            Debug.Assert((object)node.GetTypeFromHandle == null);

            var sourceType = (BoundTypeExpression)this.Visit(node.SourceType);
            var type = this.VisitType(node.Type);

            // Emit needs this helper
            MethodSymbol getTypeFromHandle;
            if (!TryGetWellKnownTypeMember(node.Syntax, WellKnownMember.System_Type__GetTypeFromHandle, out getTypeFromHandle))
            {
                return new BoundTypeOfOperator(node.Syntax, sourceType, null, type, hasErrors: true);
            }

            return node.Update(sourceType, getTypeFromHandle, type);
        }

        public override BoundNode VisitRefTypeOperator(BoundRefTypeOperator node)
        {
            Debug.Assert((object)node.GetTypeFromHandle == null);

            var operand = (BoundExpression)this.Visit(node.Operand);
            var type = this.VisitType(node.Type);

            // Emit needs this helper
            MethodSymbol getTypeFromHandle;
            if (!TryGetWellKnownTypeMember(node.Syntax, WellKnownMember.System_Type__GetTypeFromHandle, out getTypeFromHandle))
            {
                return new BoundRefTypeOperator(node.Syntax, operand, null, type, hasErrors: true);
            }

            return node.Update(operand, getTypeFromHandle, type);
        }

        public override BoundNode VisitTypeOrInstanceInitializers(BoundTypeOrInstanceInitializers node)
        {
            ImmutableArray<BoundStatement> originalStatements = node.Statements;
            ArrayBuilder<BoundStatement> statements = ArrayBuilder<BoundStatement>.GetInstance(node.Statements.Length);
            foreach (var initializer in originalStatements)
            {
                if (IsFieldOrPropertyInitializer(initializer))
                {
                    if (initializer.Kind == BoundKind.Block)
                    {
                        var block = (BoundBlock)initializer;
                        statements.Add(block.Update(block.Locals, block.LocalFunctions,
                                                    ImmutableArray.Create(RewriteExpressionStatement((BoundExpressionStatement)block.Statements.Single(),
                                                                                                     suppressInstrumentation: true))));
                    }
                    else
                    {
                        statements.Add(RewriteExpressionStatement((BoundExpressionStatement)initializer, suppressInstrumentation: true));
                    }
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
                if (statements[i] == null || (optimize && IsFieldOrPropertyInitializer(originalStatements[i]) && ShouldOptimizeOutInitializer(statements[i])))
                {
                    optimizedInitializers++;
                    if (!_factory.CurrentFunction.IsStatic)
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
                    BoundStatement rewritten = statements[i];

                    if (rewritten != null)
                    {
                        if (IsFieldOrPropertyInitializer(originalStatements[i]))
                        {
                            BoundStatement original = originalStatements[i];
                            if (Instrument && !original.WasCompilerGenerated)
                            {
                                rewritten = _instrumenter.InstrumentFieldOrPropertyInitializer(original, rewritten);
                            }
                        }

                        statements[remaining] = rewritten;
                        remaining++;
                    }
                }

                statements.Count = remaining;
                rewrittenStatements = statements.ToImmutableAndFree();
            }

            return new BoundStatementList(node.Syntax, rewrittenStatements, node.HasErrors);
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            // https://github.com/dotnet/roslyn/issues/30620
            // If the array access index is of type System.Index or System.Range
            // we need to emit code as if there were a real indexer, instead
            // of a simple array element access.

            if (node.Indices.Length != 1)
            {
                return base.VisitArrayAccess(node);
            }

            TypeSymbol rawIndexType = node.Indices[0].Type;
            if (!(TypeSymbol.Equals(rawIndexType, _compilation.GetWellKnownType(WellKnownType.System_Index), TypeCompareKind.ConsiderEverything2) ||
                  TypeSymbol.Equals(rawIndexType, _compilation.GetWellKnownType(WellKnownType.System_Range), TypeCompareKind.ConsiderEverything2)))
            {
                return base.VisitArrayAccess(node);
            }

            var syntax = node.Syntax;
            var F = _factory;
            var indexLocal = F.StoreToTemp(
                VisitExpression(node.Indices[0]),
                out BoundAssignmentOperator indexAssign);
            var arrayLocal = F.StoreToTemp(
                VisitExpression(node.Expression),
                out BoundAssignmentOperator arrayAssign);
            var indexType = VisitType(node.Indices[0].Type);

            var indexValueSymbol = (PropertySymbol)F.WellKnownMember(WellKnownMember.System_Index__Value);
            var indexFromEndSymbol = (PropertySymbol)F.WellKnownMember(WellKnownMember.System_Index__FromEnd);

            BoundExpression resultExpr;
            if (TypeSymbol.Equals(indexType, _compilation.GetWellKnownType(WellKnownType.System_Index), TypeCompareKind.ConsiderEverything2))
            {

                // array[Index] is translated to:
                // index.FromEnd ? array[array.Length - index.Value] : array[index.Value]

                var indexValueExpr = F.Property(indexLocal, indexValueSymbol);

                resultExpr = F.Sequence(
                    ImmutableArray.Create<LocalSymbol>(
                        indexLocal.LocalSymbol,
                        arrayLocal.LocalSymbol),
                    ImmutableArray.Create<BoundExpression>(
                        indexAssign,
                        arrayAssign),
                    F.Conditional(
                        F.Property(indexLocal, indexFromEndSymbol),
                        F.ArrayAccess(arrayLocal, ImmutableArray.Create<BoundExpression>(F.Binary(
                            BinaryOperatorKind.Subtraction,
                            F.SpecialType(SpecialType.System_Int32),
                            F.ArrayLength(arrayLocal),
                            indexValueExpr))),
                        F.ArrayAccess(arrayLocal, ImmutableArray.Create(indexValueExpr)),
                        node.Type));
            }
            else if (TypeSymbol.Equals(indexType, _compilation.GetWellKnownType(WellKnownType.System_Range), TypeCompareKind.ConsiderEverything2))
            {
                // array[Range] is translated to:
                // var start = range.Start.FromEnd ? array.Length - range.Start.Value : range.Start.Value;
                // var end = range.End.FromEnd ? array.Length - range.End.Value : range.End.Value;
                // var length = end - start;
                // var newArr = new T[length];
                // Array.Copy(array, start, newArr, 0, length);
                // push newArray

                var rangeStartSymbol = (PropertySymbol)F.WellKnownMember(WellKnownMember.System_Range__Start);
                var rangeEndSymbol = (PropertySymbol)F.WellKnownMember(WellKnownMember.System_Range__End);
                var arrayCopySymbol = F.WellKnownMethod(WellKnownMember.System_Array__Copy);

                var startLocal = F.StoreToTemp(
                    F.Conditional(
                        F.Property(F.Property(indexLocal, rangeStartSymbol), indexFromEndSymbol),
                        F.Binary(
                            BinaryOperatorKind.Subtraction,
                            F.SpecialType(SpecialType.System_Int32),
                            F.ArrayLength(arrayLocal),
                            F.Property(F.Property(indexLocal, rangeStartSymbol), indexValueSymbol)),
                        F.Property(F.Property(indexLocal, rangeStartSymbol), indexValueSymbol),
                        F.SpecialType(SpecialType.System_Int32)),
                    out BoundAssignmentOperator startAssign);
                var endLocal = F.StoreToTemp(
                    F.Conditional(
                        F.Property(F.Property(indexLocal, rangeEndSymbol), indexFromEndSymbol),
                        F.Binary(
                            BinaryOperatorKind.Subtraction,
                            F.SpecialType(SpecialType.System_Int32),
                            F.ArrayLength(arrayLocal),
                            F.Property(F.Property(indexLocal, rangeEndSymbol), indexValueSymbol)),
                        F.Property(F.Property(indexLocal, rangeEndSymbol), indexValueSymbol),
                        F.SpecialType(SpecialType.System_Int32)),
                    out BoundAssignmentOperator endAssign);
                var lengthLocal = F.StoreToTemp(
                    F.Binary(BinaryOperatorKind.Subtraction, F.SpecialType(SpecialType.System_Int32), endLocal, startLocal),
                    out BoundAssignmentOperator lengthAssign);
                var elementType = ((ArrayTypeSymbol)node.Type).ElementType.TypeSymbol;
                var newArrLocal = F.StoreToTemp(F.Array(elementType, lengthLocal), out BoundAssignmentOperator newArrAssign);
                var copyExpr = F.Call(null, arrayCopySymbol, ImmutableArray.Create<BoundExpression>(
                    arrayLocal,
                    startLocal,
                    newArrLocal,
                    F.Literal(0),
                    lengthLocal));
                resultExpr = F.Sequence(
                    ImmutableArray.Create(
                        indexLocal.LocalSymbol,
                        arrayLocal.LocalSymbol,
                        startLocal.LocalSymbol,
                        endLocal.LocalSymbol,
                        lengthLocal.LocalSymbol,
                        newArrLocal.LocalSymbol),
                    ImmutableArray.Create<BoundExpression>(
                        indexAssign,
                        arrayAssign,
                        startAssign,
                        endAssign,
                        lengthAssign,
                        newArrAssign,
                        copyExpr),
                    newArrLocal);
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
            }
            return resultExpr;
        }

        internal static bool IsFieldOrPropertyInitializer(BoundStatement initializer)
        {
            var syntax = initializer.Syntax;

            if (syntax is ExpressionSyntax && syntax?.Parent.Kind() == SyntaxKind.EqualsValueClause) // Should be the initial value.
            {
                switch (syntax.Parent?.Parent.Kind())
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

            BoundAssignmentOperator assignment = ((BoundExpressionStatement)statement).Expression as BoundAssignmentOperator;
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

        // There are two situations in which the language permits passing rvalues by reference.
        // (technically there are 4, but we can ignore COM and dynamic here, since that results in byval semantics regardless of the parameter ref kind)
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
        // NB: The readonliness is not considered here.
        //     We only care about possible introduction of aliasing. I.E. RValue->LValue change.
        //     Even if we start with a readonly variable, it cannot be lowered into a writeable one,
        //     with one exception - spilling of the value into a local, which is ok.
        //
        internal static bool CanBePassedByReference(BoundExpression expr)
        {
            if (expr.ConstantValue != null)
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

                case BoundKind.EventAccess:
                    var eventAccess = (BoundEventAccess)expr;
                    if (eventAccess.IsUsableAsField)
                    {
                        return eventAccess.EventSymbol.IsStatic ||
                            CanBePassedByReference(eventAccess.ReceiverOpt);
                    }

                    return false;

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    if (!fieldAccess.FieldSymbol.IsStatic)
                    {
                        return CanBePassedByReference(fieldAccess.ReceiverOpt);
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
            }

            return false;
        }

        private void CheckRefReadOnlySymbols(MethodSymbol symbol)
        {
            if (symbol.ReturnsByRefReadonly ||
                symbol.Parameters.Any(p => p.RefKind == RefKind.In))
            {
                _factory.CompilationState.ModuleBuilderOpt?.EnsureIsReadOnlyAttributeExists();
            }
        }

#if DEBUG
        /// <summary>
        /// Note: do not use a static/singleton instance of this type, as it holds state.
        /// </summary>
        private sealed class LocalRewritingValidator : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
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

            public override BoundNode VisitUsingStatement(BoundUsingStatement node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode VisitAwaitableValuePlaceholder(BoundAwaitableValuePlaceholder node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode VisitIfStatement(BoundIfStatement node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode VisitDeconstructionVariablePendingInference(DeconstructionVariablePendingInference node)
            {
                Fail(node);
                return null;
            }

            public override BoundNode VisitDeconstructValuePlaceholder(BoundDeconstructValuePlaceholder node)
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
