using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class AwaitLoweringRewriterPass1
    {
        // All nodes with a single child expression are rewritten in the same manner.
        //
        // If the node is an expression and the child contains an await expression, then the rewriting transforms:
        //     Expression(Spill(sideEffects, Value))
        // to:
        //     Spill(sideEffects, Expression(Value))
        //
        // If the node is a statement and the child contains an await expression, then the rewriting transforms:
        //     ExpressionStatement(Spill(sideEffects, Value))
        // to:
        //     Block(sideEffects, ExpressionStatement(Value))
        //
        // If the child expression does not contain an await expression then no rewriting is performed.

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            BoundExpression expression = (BoundExpression)this.Visit(node.Expression);

            if (expression.Kind != BoundKind.SpillSequence)
            {
                return node.Update(expression);
            }

            return RewriteSpillSequenceAsBlock((BoundSpillSequence)expression);
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            BoundExpression expression = (BoundExpression)this.Visit(node.ExpressionOpt);

            if (expression == null || expression.Kind != BoundKind.SpillSequence)
            {
                return node.Update(expression);
            }

            var spillSeq = (BoundSpillSequence)expression;
            return RewriteSpillSequenceAsBlock(spillSeq, node.Update(spillSeq.Value));
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            BoundExpression operand = (BoundExpression)this.Visit(node.Operand);
            TypeSymbol type = this.VisitType(node.Type);

            if (operand.Kind != BoundKind.SpillSequence)
            {
                return node.Update(node.OperatorKind, operand, node.ConstantValueOpt, node.MethodOpt, node.ResultKind, type);
            }

            var spill = (BoundSpillSequence)operand;
            return RewriteSpillSequence(spill,
                node.Update(
                    node.OperatorKind,
                    spill.Value,
                    node.ConstantValueOpt,
                    node.MethodOpt,
                    node.ResultKind,
                    type));
        }

        public override BoundNode VisitConditionalGoto(BoundConditionalGoto node)
        {
            BoundExpression condition = (BoundExpression)this.Visit(node.Condition);

            if (condition.Kind != BoundKind.SpillSequence)
            {
                return node.Update(condition, node.JumpIfTrue, node.Label);
            }

            var spill = (BoundSpillSequence)condition;
            return RewriteSpillSequenceAsBlock(spill,
                node.Update(spill.Value, node.JumpIfTrue, node.Label));
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            BoundExpression operand = (BoundExpression)this.Visit(node.Operand);
            TypeSymbol type = this.VisitType(node.Type);

            if (operand.Kind != BoundKind.SpillSequence)
            {
                return node.Update(operand, node.ConversionKind, node.SymbolOpt, node.Checked, node.ExplicitCastInCode, node.IsExtensionMethod, node.IsArrayIndex, node.ConstantValueOpt, node.ResultKind, type);
            }

            var spill = (BoundSpillSequence)operand;
            return RewriteSpillSequence(spill,
                node.Update(
                    spill.Value,
                    node.ConversionKind,
                    node.SymbolOpt,
                    node.Checked,
                    node.ExplicitCastInCode,
                    node.IsExtensionMethod,
                    node.IsArrayIndex,
                    node.ConstantValueOpt,
                    node.ResultKind,
                    type));
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            BoundExpression receiverOpt = (BoundExpression)this.Visit(node.ReceiverOpt);
            TypeSymbol type = this.VisitType(node.Type);

            if (receiverOpt == null || receiverOpt.Kind != BoundKind.SpillSequence)
            {
                return node.Update(receiverOpt, node.FieldSymbol, node.ConstantValueOpt, node.ResultKind, type);
            }

            var spill = (BoundSpillSequence)receiverOpt;
            return RewriteSpillSequence(spill,
                node.Update(
                    spill.Value,
                    node.FieldSymbol,
                    node.ConstantValueOpt,
                    node.ResultKind,
                    type));
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            BoundExpression argument = (BoundExpression)this.Visit(node.Argument);
            TypeSymbol type = this.VisitType(node.Type);

            if (argument.Kind != BoundKind.SpillSequence)
            {
                return node.Update(argument, node.MethodOpt, node.IsExtensionMethod, type);
            }

            var spill = (BoundSpillSequence)argument;
            return RewriteSpillSequence(spill,
                node.Update(
                    spill.Value,
                    node.MethodOpt,
                    node.IsExtensionMethod,
                    type));
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            var expression = (BoundExpression)this.Visit(node.BoundExpression);
            ReadOnlyArray<BoundSwitchSection> switchSections = this.VisitList(node.SwitchSections);

            if (expression.Kind != BoundKind.SpillSequence)
            {
                return node.Update(expression, node.ConstantTargetOpt, node.LocalsOpt, switchSections, node.BreakLabel);
            }
            
            var spill = (BoundSpillSequence)expression;

            var newSwitchStatement = node.Update(
                    spill.Value,
                    node.ConstantTargetOpt,
                    node.LocalsOpt,
                    switchSections,
                    node.BreakLabel);

            return RewriteSpillSequenceAsBlock(spill, newSwitchStatement);
        }

        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            BoundExpression operand = (BoundExpression)this.Visit(node.Operand);
            BoundTypeExpression targetType = (BoundTypeExpression)this.Visit(node.TargetType);
            TypeSymbol type = this.VisitType(node.Type);

            if (operand.Kind != BoundKind.SpillSequence)
            {
                return node.Update(operand, targetType, node.Conversion, type);
            }

            var spill = (BoundSpillSequence)operand;
            var newAsOperator = node.Update(spill.Value, targetType, node.Conversion, type);
            return RewriteSpillSequence(spill, newAsOperator);
        }

        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            BoundExpression operand = (BoundExpression)this.Visit(node.Operand);
            BoundTypeExpression targetType = (BoundTypeExpression)this.Visit(node.TargetType);
            TypeSymbol type = this.VisitType(node.Type);

            if (operand.Kind != BoundKind.SpillSequence)
            {
                return node.Update(operand, targetType, node.Conversion, type);
            }

            var spill = (BoundSpillSequence)operand;
            var newIsOperator = node.Update(spill.Value, targetType, node.Conversion, type);
            return RewriteSpillSequence(spill, newIsOperator);
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            BoundExpression expressionOpt = (BoundExpression)this.Visit(node.ExpressionOpt);

            if (expressionOpt == null || expressionOpt.Kind != BoundKind.SpillSequence)
            {
                return node.Update(expressionOpt);
            }

            var spill = (BoundSpillSequence)expressionOpt;
            return RewriteSpillSequenceAsBlock(spill, node.Update(spill.Value));
        }
    }
}
