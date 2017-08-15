// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            _factory.CurrentMethod = containingMethod;
            Debug.Assert(factory.CurrentType == (containingType ?? containingMethod.ContainingType));
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
            // stackalloc bound nodes have no type, and they are updated to the appropriate type after lowering
            Debug.Assert(visited == null || visited.HasErrors || ReferenceEquals(visited.Type, node.Type) ||
                    visited.Type.Equals(node.Type, TypeCompareKind.IgnoreDynamicAndTupleNames) ||
                    node.Kind == BoundKind.StackAllocArrayCreation);

            return visited;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            _sawLambdas = true;
            CheckRefReadOnlySymbols(node.Symbol);

            var oldContainingSymbol = _factory.CurrentMethod;
            try
            {
                _factory.CurrentMethod = node.Symbol;
                return base.VisitLambda(node);
            }
            finally
            {
                _factory.CurrentMethod = oldContainingSymbol;
            }
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            _sawLocalFunctions = true;
            CheckRefReadOnlySymbols(node.Symbol);

            var oldContainingSymbol = _factory.CurrentMethod;
            try
            {
                _factory.CurrentMethod = node.Symbol;
                return base.VisitLocalFunctionStatement(node);
            }
            finally
            {
                _factory.CurrentMethod = oldContainingSymbol;
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

        private bool TryGetWellKnownTypeMember<TSymbol>(SyntaxNode syntax, WellKnownMember member, out TSymbol symbol, bool isOptional = false) where TSymbol : Symbol
        {
            symbol = (TSymbol)Binder.GetWellKnownTypeMember(_compilation, member, _diagnostics, syntax: syntax, isOptional: isOptional);
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
                    statements.Add(RewriteExpressionStatement((BoundExpressionStatement)initializer, suppressInstrumentation: true));
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
                    if (!_factory.CurrentMethod.IsStatic)
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
                            var original = (BoundExpressionStatement)originalStatements[i];
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

        internal static bool IsFieldOrPropertyInitializer(BoundStatement initializer)
        {
            var syntax = initializer.Syntax;

            if (syntax is ExpressionSyntax && syntax?.Parent.Kind() == SyntaxKind.EqualsValueClause) // Should be the initial value.
            {
                switch (syntax.Parent?.Parent.Kind())
                {
                    case SyntaxKind.VariableDeclarator:
                    case SyntaxKind.PropertyDeclaration:
                        return (initializer as BoundExpressionStatement)?.Expression.Kind == BoundKind.AssignmentOperator;
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

        /// <summary>
        /// Receivers of struct methods are required to be at least RValues but can be assignable variables.
        /// Whether the mutations from the method are propagated back to the 
        /// receiver instance is conditional on whether the receiver is a variable that can be assigned. 
        /// If not, then the invocation is performed on a copy.
        /// 
        /// An inconvenient situation may arise when the receiver is an RValue expression (like a ternary operator),
        /// which is trivially reduced during lowering to one of its operands and 
        /// such operand happens to be an assignable variable (like a local). That operation alone would 
        /// expose the operand to mutations while it would not be exposed otherwise.
        /// I.E. the transformation becomes semantically observable.
        /// 
        /// To prevent such situations, we will wrap the operand into a node whose only 
        /// purpose is to never be an assignable expression.
        /// </summary>
        private static BoundExpression EnsureNotAssignableIfUsedAsMethodReceiver(BoundExpression expr)
        {
            // Leave as-is where receiver mutations cannot happen.
            if (!WouldBeAssignableIfUsedAsMethodReceiver(expr))
            {
                return expr;
            }

            return new BoundConversion(
                expr.Syntax,
                expr,
                Conversion.IdentityValue,
                @checked: false,
                explicitCastInCode: true,
                constantValueOpt: null,
                type: expr.Type)
            { WasCompilerGenerated = true };
        }

        internal static bool WouldBeAssignableIfUsedAsMethodReceiver(BoundExpression receiver)
        {
            // - reference type receivers are byval
            // - special value types (int32, Nullable<T>, . .) do not have mutating members
            if (receiver.Type.IsReferenceType ||
                receiver.Type.OriginalDefinition.SpecialType != SpecialType.None)
            {
                return false;
            }

            switch (receiver.Kind)
            {
                case BoundKind.Parameter:
                case BoundKind.Local:
                case BoundKind.ArrayAccess:
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.RefValueOperator:
                case BoundKind.PseudoVariable:
                case BoundKind.FieldAccess:
                    return true;

                case BoundKind.Call:
                    return ((BoundCall)receiver).Method.RefKind == RefKind.Ref;
            }

            return false;
        }

        private void CheckRefReadOnlySymbols(MethodSymbol symbol)
        {
            var foundRefReadOnly = false;

            if (symbol.ReturnsByRefReadonly)
            {
                foundRefReadOnly = true;
            }
            else
            {
                foreach (var parameter in symbol.Parameters)
                {
                    if (parameter.RefKind == RefKind.RefReadOnly)
                    {
                        foundRefReadOnly = true;
                        break;
                    }
                }
            }

            if (foundRefReadOnly)
            {
                _factory.CompilationState.ModuleBuilderOpt?.EnsureIsReadOnlyAttributeExists();
            }
        }
    }
}
