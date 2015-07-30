using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundStatement : IStatement
    {
        OperationKind IOperation.Kind
        {
            get { return this.StatementKind; }
        }

        SyntaxNode IOperation.Syntax
        {
            get { return this.Syntax; }
        }

        protected abstract OperationKind StatementKind { get; }
    }

    partial class BoundBlock : IBlock
    {
        ImmutableArray<IStatement> IBlock.Statements
        {
            get { return this.Statements.As<IStatement>(); }
        }

        ImmutableArray<ILocalSymbol> IBlock.Locals
        {
            get { return this.Locals.As<ILocalSymbol>(); }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.BlockStatement; }
        }
    }

    partial class BoundContinueStatement : IBranch
    {
        ILabelSymbol IBranch.Target
        {
            get { return this.Label; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.ContinueStatement; }
        }
    }

    partial class BoundBreakStatement : IBranch
    {
        ILabelSymbol IBranch.Target
        {
            get { return this.Label; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.BreakStatement; }
        }
    }

    partial class BoundYieldBreakStatement
    {
        protected override OperationKind StatementKind
        {
            get { return OperationKind.YieldBreakStatement; }
        }
    }

    partial class BoundGotoStatement : IBranch
    {
        ILabelSymbol IBranch.Target
        {
            get { return this.Label; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.GoToStatement; }
        }
    }

    partial class BoundNoOpStatement
    {
        protected override OperationKind StatementKind
        {
            get { return OperationKind.EmptyStatement; }
        }
    }

    partial class BoundIfStatement : IIf, IIfClause
    {
        ImmutableArray<IIfClause> IIf.IfClauses
        {
            get { return ImmutableArray.Create<IIfClause>(this); }
        }

        IStatement IIf.Else
        {
            get { return this.AlternativeOpt; }
        }

        IExpression IIfClause.Condition
        {
            get { return this.Condition; }
        }

        IStatement IIfClause.Body
        {
            get { return this.Consequence; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.IfStatement; }
        }
    }

    partial class BoundWhileStatement : IWhileUntil
    {
        bool IWhileUntil.IsTopTest
        {
            get { return true; }
        }

        bool IWhileUntil.IsWhile
        {
            get { return true; }
        }

        IExpression IForWhileUntil.Condition
        {
            get { return this.Condition; }
        }

        LoopKind ILoop.LoopKind
        {
            get { return LoopKind.WhileUntil; }
        }

        IStatement ILoop.Body
        {
            get { return this.Body; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.LoopStatement; }
        }
    }

    partial class BoundDoStatement : IWhileUntil
    {
        bool IWhileUntil.IsTopTest
        {
            get { return false; }
        }

        bool IWhileUntil.IsWhile
        {
            get { return true; }
        }

        IExpression IForWhileUntil.Condition
        {
            get { return this.Condition; }
        }

        LoopKind ILoop.LoopKind
        {
            get { return LoopKind.WhileUntil; }
        }

        IStatement ILoop.Body
        {
            get { return this.Body; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.LoopStatement; }
        }
    }

    partial class BoundForStatement : IFor
    {
        ImmutableArray<IStatement> IFor.Before
        {
            get { return ToStatements(this.Initializer); }
        }

        ImmutableArray<IStatement> IFor.AtLoopBottom
        {
            get { return ToStatements(this.Increment); }
        }

        ImmutableArray<ILocalSymbol> IFor.Locals
        {
            get { return this.OuterLocals.As<ILocalSymbol>(); }
        }

        IExpression IForWhileUntil.Condition
        {
            get { return this.Condition; }
        }

        LoopKind ILoop.LoopKind
        {
            get { return LoopKind.For; }
        }

        IStatement ILoop.Body
        {
            get { return this.Body; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.LoopStatement; }
        }

        ImmutableArray<IStatement> ToStatements(BoundStatement statement)
        {
            BoundStatementList statementList = statement as BoundStatementList;
            if (statementList != null)
            {
                return statementList.Statements.As<IStatement>();
            }
            else if (statement == null)
            {
                return ImmutableArray<IStatement>.Empty;
            }
            
            return ImmutableArray.Create<IStatement>(statement);
        }
    }

    partial class BoundForEachStatement : IForEach
    {
        ILocalSymbol IForEach.IterationVariable
        {
            get { return this.IterationVariable; }
        }

        IExpression IForEach.Collection
        {
            get { return this.Expression; }
        }

        LoopKind ILoop.LoopKind
        {
            get { return LoopKind.ForEach; }
        }

        IStatement ILoop.Body
        {
            get { return this.Body; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.LoopStatement; }
        }
    }

    partial class BoundSwitchStatement: ISwitch
    {
        IExpression ISwitch.Value
        {
            get { return this.BoundExpression; }
        }

        ImmutableArray<ICase> ISwitch.Cases
        {
            get { return this.SwitchSections.As<ICase>(); }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.SwitchStatement; }
        }
    }

    partial class BoundSwitchSection : ICase
    {
        ImmutableArray<ICaseClause> ICase.Clauses
        {
            get { return this.BoundSwitchLabels.As<ICaseClause>(); }
        }

        ImmutableArray<IStatement> ICase.Body
        {
            get { return this.Statements.As<IStatement>(); }
        }
    }

    partial class BoundSwitchLabel : ISingleValueCaseClause
    {
        IExpression ISingleValueCaseClause.Value
        {
            get { return this.ExpressionOpt; }
        }

        RelationalOperationKind ISingleValueCaseClause.Equality
        {
            get
            {
                BoundExpression caseValue = this.ExpressionOpt;
                if (caseValue != null)
                {
                    switch (caseValue.Type.SpecialType)
                    {
                        case SpecialType.System_Int32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_Int16:
                        case SpecialType.System_SByte:
                        case SpecialType.System_Byte:
                        case SpecialType.System_Char:
                            return RelationalOperationKind.IntegerEqual;

                        case SpecialType.System_Boolean:
                            return RelationalOperationKind.BooleanEqual;

                        case SpecialType.System_String:
                            return RelationalOperationKind.StringEqual;
                    }

                    if (caseValue.Type.TypeKind == TypeKind.Enum)
                    {
                        return RelationalOperationKind.EnumEqual;
                    }
                }

                return RelationalOperationKind.None;
            }
        }

        CaseKind ICaseClause.CaseKind
        {
            get { return this.ExpressionOpt != null ? CaseKind.SingleValue : CaseKind.Default; }
        }
    }

    partial class BoundTryStatement : ITry
    {
        IBlock ITry.Body
        {
            get { return this.TryBlock; }
        }

        ImmutableArray<ICatch> ITry.Catches
        {
            get { return this.CatchBlocks.As<ICatch>(); }
        }

        IBlock ITry.FinallyHandler
        {
            get { return this.FinallyBlockOpt; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.TryStatement; }
        }
    }

    partial class BoundCatchBlock : ICatch
    {
        IBlock ICatch.Handler
        {
            get { return this.Body; }
        }

        ITypeSymbol ICatch.CaughtType
        {
            get { return this.ExceptionTypeOpt; }
        }

        IExpression ICatch.Filter
        {
            get { return this.ExceptionFilterOpt; }
        }

        ILocalSymbol ICatch.ExceptionLocal
        {
            get { return this.LocalOpt; }
        }

        OperationKind IOperation.Kind
        {
            get { return OperationKind.CatchHandler; }
        }

        SyntaxNode IOperation.Syntax
        {
            get { return this.Syntax; }
        }
    }

    partial class BoundFixedStatement : IFixed
    {
        IVariableDeclaration IFixed.Variables
        {
            get { return this.Declarations; }
        }

        IStatement IFixed.Body
        {
            get { return this.Body; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.FixedStatement; }
        }
    }

    partial class BoundUsingStatement: IUsingWithDeclaration, IUsingWithExpression
    {
        IVariableDeclaration IUsingWithDeclaration.Variables
        {
            get { return this.DeclarationsOpt; }
        }

        IExpression IUsingWithExpression.Value
        {
            get { return this.ExpressionOpt; }
        }

        IStatement IUsing.Body
        {
            get { return this.Body; }
        }

        protected override OperationKind StatementKind
        {
            get { return this.ExpressionOpt != null ? OperationKind.UsingWithExpressionStatement : OperationKind.UsingWithDeclarationStatement; }
        }
    }

    partial class BoundThrowStatement : IThrow
    {
        IExpression IThrow.Thrown
        {
            get { return this.ExpressionOpt; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.ThrowStatement; }
        }
    }

    partial class BoundReturnStatement : IReturn
    {
        IExpression IReturn.Returned
        {
            get { return this.ExpressionOpt; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.ReturnStatement; }
        }
    }

    partial class BoundYieldReturnStatement : IReturn
    {
        IExpression IReturn.Returned
        {
            get { return this.Expression; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.YieldReturnStatement; }
        }
    }

    partial class BoundLockStatement : ILock
    {
        IExpression ILock.Locked
        {
            get { return this.Argument; }
        }

        IStatement ILock.Body
        {
            get { return this.Body; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.LockStatement; }
        }
    }

    partial class BoundBadStatement
    {
        protected override OperationKind StatementKind
        {
            get { return OperationKind.None; }
        }
    }

    partial class BoundStatementList
    {
        protected override OperationKind StatementKind
        {
            get { return OperationKind.None; }
        }
    }

    partial class BoundConditionalGoto
    {
        protected override OperationKind StatementKind
        {
            get { return OperationKind.None; }
        }
    }

    partial class BoundSequencePoint
    {
        protected override OperationKind StatementKind
        {
            get { return OperationKind.None; }
        }
    }

    partial class BoundSequencePointWithSpan
    {
        protected override OperationKind StatementKind
        {
            get { return OperationKind.None; }
        }
    }

    partial class BoundStateMachineScope
    {
        protected override OperationKind StatementKind
        {
            get { return OperationKind.None; }
        }
    }

    partial class BoundLocalDeclaration : IVariableDeclaration, IVariable
    {
        ImmutableArray<IVariable> IVariableDeclaration.Variables
        {
            get { return ImmutableArray.Create<IVariable>(this); }
        }

        ILocalSymbol IVariable.Variable
        {
            get { return this.LocalSymbol; }
        }

        IExpression IVariable.InitialValue
        {
            get { return this.InitializerOpt; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.VariableDeclarationStatement; }
        }
    }

    partial class BoundMultipleLocalDeclarations : IVariableDeclaration
    {
        ImmutableArray<IVariable> IVariableDeclaration.Variables
        {
            get { return this.LocalDeclarations.As<IVariable>(); }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.VariableDeclarationStatement; }
        }
    }

    partial class BoundLabelStatement : ILabel
    {
        ILabelSymbol ILabel.Label
        {
            get { return this.Label; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.LabelStatement; }
        }
    }

    partial class BoundLabeledStatement : ILabeled
    {
        IStatement ILabeled.Labeled
        {
            get { return this.Body; }
        }

        ILabelSymbol ILabel.Label
        {
            get { return this.Label; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.LabeledStatement; }
        }
    }

    partial class BoundExpressionStatement:IExpressionStatement
    {
        IExpression IExpressionStatement.Expression
        {
            get { return this.Expression; }
        }

        protected override OperationKind StatementKind
        {
            get { return OperationKind.ExpressionStatement; }
        }
    }

}
