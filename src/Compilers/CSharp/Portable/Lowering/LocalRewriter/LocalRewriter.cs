﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RuntimeMembers;

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
        private bool _inExpressionLambda;

        private bool _sawAwait;
        private bool _sawAwaitInExceptionHandler;
        private readonly DiagnosticBag _diagnostics;

        private LocalRewriter(
            CSharpCompilation compilation,
            MethodSymbol containingMethod,
            int containingMethodOrdinal,
            NamedTypeSymbol containingType,
            SyntheticBoundNodeFactory factory,
            SynthesizedSubmissionFields previousSubmissionFields,
            bool allowOmissionOfConditionalCalls,
            DiagnosticBag diagnostics)
        {
            _compilation = compilation;
            _factory = factory;
            _factory.CurrentMethod = containingMethod;
            Debug.Assert(factory.CurrentType == (containingType ?? containingMethod.ContainingType));
            _dynamicFactory = new LoweredDynamicOperationFactory(factory, containingMethodOrdinal);
            _previousSubmissionFields = previousSubmissionFields;
            _allowOmissionOfConditionalCalls = allowOmissionOfConditionalCalls;
            _diagnostics = diagnostics;
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
            DiagnosticBag diagnostics,
            out bool sawLambdas,
            out bool sawAwaitInExceptionHandler)
        {
            Debug.Assert(statement != null);
            Debug.Assert(compilationState != null);

            try
            {
                var factory = new SyntheticBoundNodeFactory(method, statement.Syntax, compilationState, diagnostics);
                var localRewriter = new LocalRewriter(compilation, method, methodOrdinal, containingType, factory, previousSubmissionFields, allowOmissionOfConditionalCalls, diagnostics);
                var loweredStatement = (BoundStatement)localRewriter.Visit(statement);
                sawLambdas = localRewriter._sawLambdas;
                sawAwaitInExceptionHandler = localRewriter._sawAwaitInExceptionHandler;
                var block = loweredStatement as BoundBlock;
                var result = (block == null) ? loweredStatement : InsertPrologueSequencePoint(block, method);
                return result;
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                sawLambdas = sawAwaitInExceptionHandler = false;
                return new BoundBadStatement(statement.Syntax, ImmutableArray.Create<BoundNode>(statement), hasErrors: true);
            }
        }

        private bool GenerateDebugInfo
        {
            get
            {
                return !_inExpressionLambda;
            }
        }


        // TODO(ngafter): This is a workaround.  Any piece of code that inserts a prologue
        // should be careful to insert any necessary sequence points too.
        private static BoundStatement InsertPrologueSequencePoint(BoundBlock body, MethodSymbol method)
        {
            // we need to insert a debug sequence point here for any prologue code
            // we'll associate it w/ the method declaration
            if (body != null && body.Statements.Length != 0 && !body.HasErrors)
            {
                var first = body.Statements.First();
                if (first.Kind != BoundKind.SequencePoint && first.Kind != BoundKind.SequencePointWithSpan)
                {
                    var asSourceMethod = method.ConstructedFrom as SourceMethodSymbol;
                    if ((object)asSourceMethod != null)
                    {
                        var syntax = asSourceMethod.BodySyntax as BlockSyntax;
                        if (syntax != null)
                        {
                            return AddSequencePoint(syntax, body);
                        }
                    }
                }
            }

            return body;
        }

        private PEModuleBuilder EmitModule
        {
            get { return _factory.CompilationState.ModuleBuilderOpt; }
        }

        private BoundStatement AddSequencePoint(BoundStatement node)
        {
            if (this.GenerateDebugInfo && !node.WasCompilerGenerated)
            {
                node = new BoundSequencePoint(node.Syntax, node);
            }

            return node;
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
            Debug.Assert(visited == null || visited.HasErrors || ReferenceEquals(visited.Type, node.Type) || visited.Type.Equals(node.Type, ignoreDynamic: true));

            return visited;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            _sawLambdas = true;
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

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            // Cannot recurse into BadExpression children since the BadExpression
            // may represent being unable to use the child as an lvalue or rvalue.
            return node;
        }

        private static BoundStatement BadStatement(BoundNode node)
        {
            return (node == null)
                ? new BoundBadStatement(null, default(ImmutableArray<BoundNode>), true)
                : new BoundBadStatement(node.Syntax, ImmutableArray.Create<BoundNode>(node), true);
        }

        private static BoundExpression BadExpression(BoundExpression node)
        {
            return new BoundBadExpression(node.Syntax, LookupResultKind.NotReferencable, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(node), node.Type);
        }

        private bool TryGetWellKnownTypeMember<TSymbol>(CSharpSyntaxNode syntax, WellKnownMember member, out TSymbol symbol, bool isOptional = false) where TSymbol : Symbol
        {
            symbol = (TSymbol)Binder.GetWellKnownTypeMember(_compilation, member, _diagnostics, syntax: syntax, isOptional: isOptional);
            return ((object)symbol != null);
        }

        private MethodSymbol GetSpecialTypeMethod(CSharpSyntaxNode syntax, SpecialMember specialMember)
        {
            MethodSymbol method;
            if (Binder.TryGetSpecialTypeMember(_compilation, specialMember, syntax, _diagnostics, out method))
            {
                return method;
            }
            else
            {
                MemberDescriptor descriptor = SpecialMembers.GetDescriptor(specialMember);
                SpecialType type = (SpecialType)descriptor.DeclaringTypeId;
                TypeSymbol container = _compilation.Assembly.GetSpecialType(type);
                TypeSymbol returnType = new ExtendedErrorTypeSymbol(compilation: _compilation, name: descriptor.Name, errorInfo: null, arity: descriptor.Arity);
                return new ErrorMethodSymbol(container, returnType, "Missing");
            }
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
            ImmutableArray<BoundStatement> rewrittenStatements = (ImmutableArray<BoundStatement>)this.VisitList(node.Statements);
            ImmutableArray<BoundStatement> optimizedStatements = ImmutableArray<BoundStatement>.Empty;

            if (_compilation.Options.OptimizationLevel == OptimizationLevel.Release)
            {
                // TODO: this part may conflict with InitializerRewriter.Rewrite in how it handles 
                //       the first field initializer (see 'if (i == 0)'...) which seems suspicious
                ArrayBuilder<BoundStatement> statements = ArrayBuilder<BoundStatement>.GetInstance();
                bool anyNonDefault = false;

                foreach (var initializer in rewrittenStatements)
                {
                    if (ShouldOptimizeOutInitializer(initializer))
                    {
                        if (_factory.CurrentMethod.IsStatic)
                        {
                            // NOTE: Dev11 removes static initializers if ONLY all of them are optimized out
                            statements.Add(initializer);
                        }
                    }
                    else
                    {
                        statements.Add(initializer);
                        anyNonDefault = true;
                    }
                }

                if (anyNonDefault)
                {
                    optimizedStatements = statements.ToImmutableAndFree();
                }
                else
                {
                    statements.Free();
                }
            }
            else
            {
                optimizedStatements = rewrittenStatements;
            }

            return new BoundStatementList(node.Syntax, optimizedStatements, node.HasErrors);
        }

        /// <summary>
        /// Returns true if the initializer is a field initializer which should be optimized out
        /// </summary>
        private static bool ShouldOptimizeOutInitializer(BoundStatement initializer)
        {
            BoundStatement statement = initializer;

            if (initializer.Kind == BoundKind.SequencePointWithSpan)
            {
                statement = ((BoundSequencePointWithSpan)initializer).StatementOpt;
            }
            else if (initializer.Kind == BoundKind.SequencePoint)
            {
                statement = ((BoundSequencePoint)initializer).StatementOpt;
            }

            if (statement == null || statement.Kind != BoundKind.ExpressionStatement)
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
    }
}
