using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class AwaitLoweringRewriterPass1 : BoundTreeRewriter
    {
        private readonly SyntheticBoundNodeFactory F;

        public AwaitLoweringRewriterPass1(SyntheticBoundNodeFactory F)
        {
            this.F = F;
        }

        public override BoundNode VisitSpillSequence(BoundSpillSequence node)
        {
            ReadOnlyArray<BoundStatement> statements = (ReadOnlyArray<BoundStatement>)this.VisitList(node.Statements);
            BoundExpression value = (BoundExpression)this.Visit(node.Value);
            TypeSymbol type = this.VisitType(node.Type);

            if (value.Kind != BoundKind.SpillSequence)
            {
                return node.Update(node.Locals, node.SpillTemps, node.SpillFields, statements, value, type);
            }

            var valAwait = (BoundSpillSequence)value;

            return node.Update(
                node.Locals.Concat(valAwait.Locals),
                node.SpillTemps.Concat(valAwait.SpillTemps),
                node.SpillFields,
                statements.Concat(valAwait.Statements),
                valAwait.Value,
                valAwait.Type);
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            ReadOnlyArray<BoundExpression> sideEffects = (ReadOnlyArray<BoundExpression>)this.VisitList(node.SideEffects);
            BoundExpression value = (BoundExpression)this.Visit(node.Value);
            TypeSymbol type = this.VisitType(node.Type);

            if (!RequiresSpill(sideEffects) && value.Kind != BoundKind.SpillSequence)
            {
                return node.Update(node.Locals, sideEffects, value, type);
            }

            var spillBuilder = new SpillBuilder();

            spillBuilder.Locals.AddRange(node.Locals);

            foreach (var sideEffect in sideEffects)
            {
                spillBuilder.Statements.Add(
                    (sideEffect.Kind == BoundKind.SpillSequence)
                    ? RewriteSpillSequenceAsBlock((BoundSpillSequence)sideEffect)
                    : F.ExpressionStatement(sideEffect));
            }

            BoundExpression newValue;
            if (value.Kind == BoundKind.SpillSequence)
            {
                var awaitEffect = (BoundSpillSequence)value;
                spillBuilder.AddSpill(awaitEffect);
                newValue = awaitEffect.Value;
            }
            else
            {
                newValue = value;
            }

            return spillBuilder.BuildSequenceAndFree(F, newValue);
        }

        private bool RequiresSpill(params BoundExpression[] nodes)
        {
            foreach (var node in nodes)
            {
                if (RequiresSpill(node))
                {
                    return true;
                }
            }

            return false;
        }

        private bool RequiresSpill(ReadOnlyArray<BoundExpression> arguments)
        {
            foreach (var arg in arguments)
            {
                if (RequiresSpill(arg))
                {
                    return true;
                }
            }

            return false;
        }

        private bool RequiresSpill(BoundExpression node)
        {
            if (node == null)
            {
                return false;
            }

            return
                node.Kind == BoundKind.SpillSequence ||
                (node.Kind == BoundKind.ArrayInitialization && RequiresSpill(((BoundArrayInitialization)node).Initializers)) ||
                (node.Kind == BoundKind.ArgListOperator && RequiresSpill(((BoundArgListOperator)node).Arguments));
        }
        
        private BoundStatement RewriteSpillSequenceAsBlock(BoundSpillSequence spillSequence)
        {
            var awaitStatements = spillSequence.Statements;

            if (spillSequence.Value != null)
            {
                awaitStatements = awaitStatements.Append(F.ExpressionStatement(spillSequence.Value));
            }

            return F.SpillBlock(
                spillSequence.Locals,
                spillSequence.SpillTemps,
                awaitStatements);
        }

        private BoundStatement RewriteSpillSequenceAsBlock(BoundSpillSequence spillSequence, BoundStatement value)
        {
            return F.SpillBlock(
                spillSequence.Locals,
                spillSequence.SpillTemps,
                spillSequence.Statements.Append(value));
        }

        private static BoundSpillSequence RewriteSpillSequence(BoundSpillSequence spill, BoundExpression value)
        {
            return spill.Update(spill.Locals, spill.SpillTemps, spill.SpillFields, spill.Statements, value, value.Type);
        }

        private BoundStatement GenerateSpillInit(BoundSpillTemp spillTemp)
        {
            if (spillTemp.Expr.Kind == BoundKind.SpillSequence)
            {
                var spill = (BoundSpillSequence)spillTemp.Expr;
                return F.SpillBlock(
                    spill.Locals,
                    spill.SpillTemps,
                    spill.Statements.Append(
                        F.Assignment(spillTemp, spill.Value)));
            }
            else
            {
                return F.Assignment(spillTemp, spillTemp.Expr);
            }
        }
    }
}