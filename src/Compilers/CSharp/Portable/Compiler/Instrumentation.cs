using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Instrumentation
    {
        internal static BoundBlock InjectInstrumentation(MethodSymbol method, BoundBlock methodBody, int methodOrdinal, TypeCompilationState compilationState, CSharpCompilation compilation, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider)
        {
            if (methodBody != null)
            {
                // Create the symbol for the instrumentation payload.
                SyntheticBoundNodeFactory payloadArrayFactory = new SyntheticBoundNodeFactory(method, methodBody.Syntax, compilationState, diagnostics);
                TypeSymbol boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
                TypeSymbol payloadElementType = boolType;
                ArrayTypeSymbol payloadArrayType = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, payloadElementType);
                SynthesizedFieldSymbol instrumentationPayload = new SynthesizedFieldSymbol(method.ContainingType, payloadArrayType, method.Name + "*instrumentation*" + methodOrdinal.ToString(), isStatic: true);
                payloadArrayFactory.AddField(method.ContainingType, instrumentationPayload);
                
                // Synthesize the instrumentation and collect the points of interest.

                ArrayBuilder<Cci.SequencePoint> pointsBuilder = new ArrayBuilder<Cci.SequencePoint>();
                BoundTreeRewriter collector = new InstrumentationInjectionWalker(method, pointsBuilder, instrumentationPayload, compilationState, diagnostics, debugDocumentProvider);
                BoundBlock newMethodBody = (BoundBlock)collector.Visit(methodBody);

                ImmutableArray<Cci.SequencePoint> points = pointsBuilder.ToImmutableAndFree();

                // Synthesize the initialization of the instrumentation payload array. It should actually be done either statically or with concurrency-safe code.
                //
                // if (payloadArray == null)
                // {
                //     payloadArray = new PayloadType[] { default0, default1, ... defaultN };
                //     Instrumentation.AddPayload(method, payloadArray);
                //
                //
                // but should be
                //
                // if (payloadArray == null)
                // {
                //     payloadArray = new PayloadType[] { default0, default1, ... defaultN };
                //     if (Interlocked.CompareExchange(ref payloadArray, payloadArray, null) == null)
                //         Instrumentation.AddPayload(method, payloadArray);
                // }

                ArrayBuilder<BoundExpression> elementsBuilder = new ArrayBuilder<BoundExpression>(points.Length);
                for (int i = 0; i < points.Length; i++)
                {
                    elementsBuilder.Add(payloadArrayFactory.Literal(false));
                }

                BoundStatement payloadAssignment =
                    payloadArrayFactory.Assignment(
                        payloadArrayFactory.Field(null, instrumentationPayload),
                        payloadArrayFactory.Array(payloadElementType, elementsBuilder.ToImmutableAndFree()));

                BoundExpression payloadNullTest =
                    payloadArrayFactory.Binary(BinaryOperatorKind.ObjectEqual, boolType, payloadArrayFactory.Field(null, instrumentationPayload), payloadArrayFactory.Null(payloadArrayType));

                BoundStatement payloadIf =
                    payloadArrayFactory.If(payloadNullTest, payloadArrayFactory.Block(ImmutableArray.Create(payloadAssignment)));

                ImmutableArray<BoundStatement> newStatements = newMethodBody.Statements.Insert(0, payloadIf);
                newMethodBody = newMethodBody.Update(newMethodBody.Locals, newMethodBody.LocalFunctions, newStatements);

                return newMethodBody;
            }

            return null;
        }
    }

    internal sealed class InstrumentationInjectionWalker : BoundTreeRewriterWithStackGuard
    {
        private readonly MethodSymbol _method;
        private readonly ArrayBuilder<Cci.SequencePoint> _pointsBuilder;
        private readonly FieldSymbol _payload;
        private readonly TypeCompilationState _compilationState;
        private readonly DiagnosticBag _diagnostics;
        private readonly DebugDocumentProvider _debugDocumentProvider;

        public InstrumentationInjectionWalker(MethodSymbol method, ArrayBuilder<Cci.SequencePoint> pointsBuilder, FieldSymbol payload, TypeCompilationState compilationState, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider)
        {
            _method = method;
            _pointsBuilder = pointsBuilder;
            _payload = payload;
            _compilationState = compilationState;
            _diagnostics = diagnostics;
            _debugDocumentProvider = debugDocumentProvider;
        }

        public override BoundNode VisitBadStatement(BoundBadStatement node)
        {
            return CollectDynamicAnalysis(base.VisitBadStatement(node));
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            return CollectDynamicAnalysis(base.VisitBlock(node));
        }

        public override BoundNode VisitBreakStatement(BoundBreakStatement node)
        {
            return CollectDynamicAnalysis(base.VisitBreakStatement(node));
        }

        public override BoundNode VisitContinueStatement(BoundContinueStatement node)
        {
            return CollectDynamicAnalysis(base.VisitContinueStatement(node));
        }

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            return CollectDynamicAnalysis(base.VisitDoStatement(node));
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            return CollectDynamicAnalysis(base.VisitExpressionStatement(node));
        }

        public override BoundNode VisitFixedStatement(BoundFixedStatement node)
        {
            return CollectDynamicAnalysis(base.VisitFixedStatement(node));
        }

        public override BoundNode VisitForEachStatement(BoundForEachStatement node)
        {
            return CollectDynamicAnalysis(base.VisitForEachStatement(node));
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            return CollectDynamicAnalysis(base.VisitForStatement(node));
        }

        public override BoundNode VisitGotoStatement(BoundGotoStatement node)
        {
            return CollectDynamicAnalysis(base.VisitGotoStatement(node));
        }

        public override BoundNode VisitIfStatement(BoundIfStatement node)
        {
            return CollectDynamicAnalysis(base.VisitIfStatement(node));
        }

        public override BoundNode VisitLabeledStatement(BoundLabeledStatement node)
        {
            // This construct can be ignored in favor of the underlying statement.
            return base.VisitLabeledStatement(node);
        }

        public override BoundNode VisitLabelStatement(BoundLabelStatement node)
        {
            return CollectDynamicAnalysis(base.VisitLabelStatement(node));
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            return CollectDynamicAnalysis(base.VisitLocalDeclaration(node));
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            return CollectDynamicAnalysis(base.VisitLocalFunctionStatement(node));
        }

        public override BoundNode VisitLockStatement(BoundLockStatement node)
        {
            return CollectDynamicAnalysis(base.VisitLockStatement(node));
        }

        public override BoundNode VisitMultipleLocalDeclarations(BoundMultipleLocalDeclarations node)
        {
            return CollectDynamicAnalysis(base.VisitMultipleLocalDeclarations(node));
        }

        public override BoundNode VisitNoOpStatement(BoundNoOpStatement node)
        {
            return CollectDynamicAnalysis(base.VisitNoOpStatement(node));
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            return CollectDynamicAnalysis(base.VisitReturnStatement(node));
        }

        public override BoundNode VisitSequencePointWithSpan(BoundSequencePointWithSpan node)
        {
            // This construct can be ignored in favor of the underlying statement.
            return base.VisitSequencePointWithSpan(node);
        }

        public override BoundNode VisitStatementList(BoundStatementList node)
        {
            return CollectDynamicAnalysis(base.VisitStatementList(node));
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            return CollectDynamicAnalysis(base.VisitSwitchStatement(node));
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            return CollectDynamicAnalysis(base.VisitThrowStatement(node));
        }

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            return CollectDynamicAnalysis(base.VisitTryStatement(node));
        }

        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            return CollectDynamicAnalysis(base.VisitUsingStatement(node));
        }

        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            return CollectDynamicAnalysis(base.VisitWhileStatement(node));
        }

        public override BoundNode VisitYieldBreakStatement(BoundYieldBreakStatement node)
        {
            return CollectDynamicAnalysis(base.VisitYieldBreakStatement(node));
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            return CollectDynamicAnalysis(base.VisitYieldReturnStatement(node));
        }

        private BoundNode CollectDynamicAnalysis(BoundNode node)
        {
            BoundStatement statement = node as BoundStatement;
            if (statement != null)
            {
                return CollectDynamicAnalysis(statement);
            }

            return node;
        }

        private BoundNode CollectDynamicAnalysis(BoundStatement statement)
        {
            if (statement.WasCompilerGenerated)
            {
                return statement;
            }

            FileLinePositionSpan lineSpan = statement.Syntax.GetLocation().GetMappedLineSpan();
            string path = lineSpan.Path;
            if (path == "")
            {
                path = statement.Syntax.SyntaxTree.FilePath;
            }

            int pointsIndex = _pointsBuilder.Count;
            _pointsBuilder.Add(new Cci.SequencePoint(_debugDocumentProvider.Invoke(path, ""), 0, lineSpan.Span.Start.Line, lineSpan.Span.Start.Character, lineSpan.Span.End.Line, lineSpan.Span.End.Character));

            // Generate "_payload[pointIndex] = true".

            SyntheticBoundNodeFactory statementFactory = new SyntheticBoundNodeFactory(_method, statement.Syntax, _compilationState, _diagnostics);
            BoundArrayAccess payloadCell = statementFactory.ArrayAccess(statementFactory.Field(null, _payload), statementFactory.Literal(pointsIndex));
            BoundExpressionStatement cellAssignment = statementFactory.Assignment(payloadCell, statementFactory.Literal(true));
            
            return statementFactory.Block(ImmutableArray.Create(cellAssignment, statement));
        }
    }
}
