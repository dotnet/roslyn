using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Instrumentation
    {
        internal static BoundBlock InjectInstrumentation(MethodSymbol method, BoundBlock methodBody, DebugDocumentProvider debugDocumentProvider)
        {
            if (methodBody != null)
            {
                ArrayBuilder<Cci.SequencePoint> pointsBuilder = new ArrayBuilder<Cci.SequencePoint>();
                BoundTreeRewriter collector = new InstrumentationInjectionWalker(pointsBuilder, debugDocumentProvider);
                BoundBlock newMethodBody = (BoundBlock)collector.Visit(methodBody);

                ImmutableArray<Cci.SequencePoint> points = pointsBuilder.ToImmutableAndFree();
                return newMethodBody;
            }

            return null;
        }
    }

    internal sealed class InstrumentationCollectionWalker : BoundTreeWalkerWithStackGuard
    {
        private readonly ArrayBuilder<Cci.SequencePoint> _pointsBuilder;
        private readonly DebugDocumentProvider _debugDocumentProvider;
        private readonly HashSet<BoundStatement> _statementsToSkip = new HashSet<BoundStatement>();

        public InstrumentationCollectionWalker(ArrayBuilder<Cci.SequencePoint> pointsBuilder, DebugDocumentProvider debugDocumentProvider)
        {
            _pointsBuilder = pointsBuilder;
            _debugDocumentProvider = debugDocumentProvider;
        }

        public override BoundNode Visit(BoundNode operation)
        {
            if (operation == null)
            {
                return null;
            }

            BoundStatement statement = operation as BoundStatement;
            if (statement != null && !_statementsToSkip.Contains(statement))
            {
                if (statement.Kind == BoundKind.SequencePointWithSpan)
                {
                    BoundSequencePointWithSpan sequence = (BoundSequencePointWithSpan)statement;
                    if (sequence.StatementOpt != null)
                    {
                        _statementsToSkip.Add(sequence.StatementOpt);
                    }
                }

                FileLinePositionSpan lineSpan = statement.Syntax.GetLocation().GetMappedLineSpan();
                string path = lineSpan.Path;
                if (path == "")
                {
                    path = statement.Syntax.SyntaxTree.FilePath;
                }

                _pointsBuilder.Add(new Cci.SequencePoint(_debugDocumentProvider.Invoke(path, ""), 0, lineSpan.Span.Start.Line, lineSpan.Span.Start.Character, lineSpan.Span.End.Line, lineSpan.Span.End.Character));
            }

            base.Visit(operation);
            return null;
        }
    }

    internal sealed class InstrumentationInjectionWalker : BoundTreeRewriterWithStackGuard
    {
        private readonly ArrayBuilder<Cci.SequencePoint> _pointsBuilder;
        private readonly DebugDocumentProvider _debugDocumentProvider;
        private readonly HashSet<BoundStatement> _statementsToSkip = new HashSet<BoundStatement>();

        public InstrumentationInjectionWalker(ArrayBuilder<Cci.SequencePoint> pointsBuilder, DebugDocumentProvider debugDocumentProvider)
        {
            _pointsBuilder = pointsBuilder;
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
            MarkStatementToSkip(node.Body);
            return CollectDynamicAnalysis(base.VisitLabeledStatement(node));
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
            MarkStatementToSkip(node.StatementOpt);
            return CollectDynamicAnalysis(base.VisitSequencePointWithSpan(node));
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

        private void MarkStatementToSkip(BoundStatement statement)
        {
            if (statement != null)
            {
                _statementsToSkip.Add(statement);
            }
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
            if (_statementsToSkip.Contains(statement))
            {
                return statement;
            }

            FileLinePositionSpan lineSpan = statement.Syntax.GetLocation().GetMappedLineSpan();
            string path = lineSpan.Path;
            if (path == "")
            {
                path = statement.Syntax.SyntaxTree.FilePath;
            }

            _pointsBuilder.Add(new Cci.SequencePoint(_debugDocumentProvider.Invoke(path, ""), 0, lineSpan.Span.Start.Line, lineSpan.Span.Start.Character, lineSpan.Span.End.Line, lineSpan.Span.End.Character));

            return statement;
        }
    }
}
