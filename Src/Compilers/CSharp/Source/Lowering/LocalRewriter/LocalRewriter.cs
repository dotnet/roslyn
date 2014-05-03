// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter : BoundTreeRewriter
    {
        private readonly bool generateDebugInfo;
        private readonly CSharpCompilation compilation;
        private readonly SyntheticBoundNodeFactory factory;
        private readonly SynthesizedSubmissionFields previousSubmissionFields;
        private readonly LoweredDynamicOperationFactory dynamicFactory;
        private bool sawLambdas;
        private bool inExpressionLambda;

        private bool sawAwait;
        private bool sawAwaitInExceptionHandler;
        private readonly DiagnosticBag diagnostics;

        private LocalRewriter(bool generateDebugInfo, MethodSymbol containingMethod, NamedTypeSymbol containingType, SyntheticBoundNodeFactory factory, SynthesizedSubmissionFields previousSubmissionFields, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            this.generateDebugInfo = generateDebugInfo && containingMethod.GenerateDebugInfo;
            this.compilation = compilation;
            this.factory = factory;
            this.factory.CurrentMethod = containingMethod;
            Debug.Assert(factory.CurrentClass == (containingType ?? containingMethod.ContainingType));
            this.dynamicFactory = new LoweredDynamicOperationFactory(factory);
            this.previousSubmissionFields = previousSubmissionFields;
            this.diagnostics = diagnostics;
        }

        /// <summary>
        /// Lower a block of code by performing local rewritings.
        /// </summary>
        public static BoundStatement Rewrite(
            CSharpCompilation compilation,
            bool generateDebugInfo,
            MethodSymbol containingSymbol,
            NamedTypeSymbol containingType,
            BoundStatement statement,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics,
            SynthesizedSubmissionFields previousSubmissionFields,
            out bool sawLambdas,
            out bool sawDynamicOperations,
            out bool sawAwaitInExceptionHandler)
        {
            Debug.Assert(statement != null);
            Debug.Assert(compilationState != null);

            try
            {
                var factory = new SyntheticBoundNodeFactory(containingSymbol, statement.Syntax, compilationState, diagnostics);
                var localRewriter = new LocalRewriter(generateDebugInfo, containingSymbol, containingType, factory, previousSubmissionFields, compilation, diagnostics);
                var loweredStatement = (BoundStatement)localRewriter.Visit(statement);
                sawLambdas = localRewriter.sawLambdas;
                sawAwaitInExceptionHandler = localRewriter.sawAwaitInExceptionHandler;
                sawDynamicOperations = localRewriter.dynamicFactory.GeneratedDynamicOperations;
                var block = loweredStatement as BoundBlock;
                var result = (block == null) ? loweredStatement : InsertPrologueSequencePoint(block, containingSymbol);
                return result;
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                sawLambdas = sawDynamicOperations = sawAwaitInExceptionHandler = false;
                return new BoundBadStatement(statement.Syntax, ImmutableArray.Create<BoundNode>(statement), hasErrors: true);
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
                    // we basically need to get a span for the whole declaration, but not the body -
                    //  "[SomeAttribute] public MyCtorName(params int[] values): base()" 
                    var asSourceMethod = method.ConstructedFrom as SourceMethodSymbol;
                    if ((object)asSourceMethod != null)
                    {
                        var syntax = asSourceMethod.BlockSyntax;

                        if (syntax != null)
                        {
                            var start = syntax.Parent.SpanStart;
                            var end = syntax.OpenBraceToken.GetPreviousToken().Span.End;

                            // just wrap it. We do not need to force a nop. there will either be
                            // code between the method start and the first statement or we do not
                            // care about this SP
                            return new BoundSequencePointWithSpan(
                                syntax,
                                body,
                                TextSpan.FromBounds(start, end));
                        }
                    }
                }
            }

            return body;
        }

        private PEModuleBuilder EmitModule
        {
            get { return this.factory.CompilationState.ModuleBuilderOpt; }
        }

        private BoundStatement AddSequencePoint(BoundStatement node)
        {
            if (this.generateDebugInfo && !node.WasCompilerGenerated)
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
                if (((object)type == null || !type.IsNullableType()))
                {
                    return MakeLiteral(node.Syntax, constantValue, type);
                }
            }

            var visited = (BoundExpression)base.Visit(node);

            // If you *really* need to change the type, consider using an indirect method
            // like compound assignment does (extra flag only passed when it is an expression
            // statement means that this constraint is not violated).
            // Dynamic type will be erased in emit phase. It is considered equivalent to Object in lowered bound trees.
            Debug.Assert(visited == null || visited.HasErrors || ReferenceEquals(visited.Type, node.Type) || visited.Type.Equals(node.Type, ignoreDynamic: true));

            return visited;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            this.sawLambdas = true;
            var oldContainingSymbol = this.factory.CurrentMethod;
            try
            {
                this.factory.CurrentMethod = node.Symbol;
                return base.VisitLambda(node);
            }
            finally
            {
                this.factory.CurrentMethod = oldContainingSymbol;
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
            symbol = (TSymbol)Binder.GetWellKnownTypeMember(this.compilation, member, diagnostics, syntax: syntax, isOptional: isOptional);
            return ((object)symbol != null);
        }

        private MethodSymbol GetSpecialTypeMethod(CSharpSyntaxNode syntax, SpecialMember specialMember)
        {
            MethodSymbol method;
            if (TryGetSpecialTypeMember<MethodSymbol>(syntax, specialMember, out method))
            {
                return method;
            }
            else
            {
                MemberDescriptor descriptor = SpecialMembers.GetDescriptor(specialMember);
                SpecialType type = (SpecialType)descriptor.DeclaringTypeId;
                TypeSymbol container = this.compilation.Assembly.GetSpecialType(type);
                TypeSymbol returnType = new ExtendedErrorTypeSymbol(compilation: this.compilation, name: descriptor.Name, errorInfo: null, arity: descriptor.Arity);
                return new ErrorMethodSymbol(container, returnType, "Missing");
            }
        }

        private bool TryGetSpecialTypeMember<TSymbol>(CSharpSyntaxNode syntax, SpecialMember specialMember, out TSymbol symbol) where TSymbol : Symbol
        {
            symbol = (TSymbol)this.compilation.Assembly.GetSpecialTypeMember(specialMember);
            if ((object)symbol == null)
            {
                MemberDescriptor descriptor = SpecialMembers.GetDescriptor(specialMember);
                SpecialType type = (SpecialType)descriptor.DeclaringTypeId;
                diagnostics.Add(ErrorCode.ERR_MissingPredefinedMember, syntax.Location, type.GetMetadataName(), descriptor.Name);
                return false;
            }
            else
            {
                var useSiteDiagnostic = symbol.GetUseSiteDiagnosticForSymbolOrContainingType();
                if (useSiteDiagnostic != null)
                {
                    Symbol.ReportUseSiteDiagnostic(useSiteDiagnostic, diagnostics, new SourceLocation(syntax));
                }
            }

            return true;
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

            if (compilation.Options.Optimize)
            {
                // TODO: this part may conflict with InitializerRewriter.Rewrite in how it handles 
                //       the first field initializer (see 'if (i == 0)'...) which seems suspicious
                ArrayBuilder<BoundStatement> statements = ArrayBuilder<BoundStatement>.GetInstance();
                bool anyNonDefault = false;

                foreach (var initializer in rewrittenStatements)
                {
                    if (ShouldOptimizeOutInitializer(initializer))
                    {
                        if (this.factory.CurrentMethod.IsStatic)
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
                Debug.Assert(false, "initializer does not initialize a field?");
                return false;
            }

            BoundAssignmentOperator assignment = ((BoundExpressionStatement)statement).Expression as BoundAssignmentOperator;
            if (assignment == null)
            {
                Debug.Assert(false, "initializer does not initialize a field?");
                return false;
            }

            Debug.Assert(assignment.Left.Kind == BoundKind.FieldAccess);

            BoundExpression rhs = assignment.Right;
            return rhs.IsDefaultValue();
        }
    }
}