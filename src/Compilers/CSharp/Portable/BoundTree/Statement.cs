using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundStatement : Semantics.IStatement
    {
        Semantics.OperationKind Semantics.IOperation.Kind
        {
            get { return this.StatementKind; }
        }

        SyntaxNode Semantics.IOperation.Syntax
        {
            get { return this.Syntax; }
        }

        protected abstract Semantics.OperationKind StatementKind { get; }
    }

    partial class BoundBlock : Semantics.IBlock
    {
        ImmutableArray<Semantics.IStatement> Semantics.IBlock.Statements
        {
            get { return this.Statements.As<Semantics.IStatement>(); }
        }

        ImmutableArray<ILocalSymbol> Semantics.IBlock.Locals
        {
            get { return this.Locals.As<ILocalSymbol>(); }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.BlockStatement; }
        }
    }

    partial class BoundContinueStatement : Semantics.IBranch
    {
        ILabelSymbol Semantics.IBranch.Target
        {
            get { return this.Label; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.ContinueStatement; }
        }
    }

    partial class BoundBreakStatement : Semantics.IBranch
    {
        ILabelSymbol Semantics.IBranch.Target
        {
            get { return this.Label; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.BreakStatement; }
        }
    }

    partial class BoundYieldBreakStatement
    {
        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.YieldBreakStatement; }
        }
    }

    partial class BoundGotoStatement : Semantics.IBranch
    {
        ILabelSymbol Semantics.IBranch.Target
        {
            get { return this.Label; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.GoToStatement; }
        }
    }

    partial class BoundNoOpStatement
    {
        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.EmptyStatement; }
        }
    }

    partial class BoundIfStatement : Semantics.IIf, Semantics.IIfClause
    {
        ImmutableArray<Semantics.IIfClause> Semantics.IIf.IfClauses
        {
            get { return ImmutableArray.Create<Semantics.IIfClause>(this); }
        }

        Semantics.IStatement Semantics.IIf.Else
        {
            get { return this.AlternativeOpt; }
        }

        Semantics.IExpression Semantics.IIfClause.Condition
        {
            get { return this.Condition; }
        }

        Semantics.IStatement Semantics.IIfClause.Body
        {
            get { return this.Consequence; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.IfStatement; }
        }
    }

    partial class BoundWhileStatement : Semantics.IWhileUntil
    {
        bool Semantics.IWhileUntil.IsTopTest
        {
            get { return true; }
        }

        bool Semantics.IWhileUntil.IsWhile
        {
            get { return true; }
        }

        Semantics.IExpression Semantics.IForWhileUntil.Condition
        {
            get { return this.Condition; }
        }

        Semantics.LoopKind Semantics.ILoop.LoopClass
        {
            get { return Semantics.LoopKind.WhileUntil; }
        }

        Semantics.IStatement Semantics.ILoop.Body
        {
            get { return this.Body; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.LoopStatement; }
        }
    }

    partial class BoundDoStatement : Semantics.IWhileUntil
    {
        bool Semantics.IWhileUntil.IsTopTest
        {
            get { return false; }
        }

        bool Semantics.IWhileUntil.IsWhile
        {
            get { return true; }
        }

        Semantics.IExpression Semantics.IForWhileUntil.Condition
        {
            get { return this.Condition; }
        }

        Semantics.LoopKind Semantics.ILoop.LoopClass
        {
            get { return Semantics.LoopKind.WhileUntil; }
        }

        Semantics.IStatement Semantics.ILoop.Body
        {
            get { return this.Body; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.LoopStatement; }
        }
    }

    partial class BoundForStatement : Semantics.IFor
    {
        ImmutableArray<Semantics.IStatement> Semantics.IFor.Before
        {
            get { return ToStatements(this.Initializer); }
        }

        ImmutableArray<Semantics.IStatement> Semantics.IFor.AtLoopBottom
        {
            get { return ToStatements(this.Increment); }
        }

        ImmutableArray<ILocalSymbol> Semantics.IFor.Locals
        {
            get { return this.OuterLocals.As<ILocalSymbol>(); }
        }

        Semantics.IExpression Semantics.IForWhileUntil.Condition
        {
            get { return this.Condition; }
        }

        Semantics.LoopKind Semantics.ILoop.LoopClass
        {
            get { return Semantics.LoopKind.For; }
        }

        Semantics.IStatement Semantics.ILoop.Body
        {
            get { return this.Body; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.LoopStatement; }
        }

        ImmutableArray<Semantics.IStatement> ToStatements(BoundStatement statement)
        {
            BoundStatementList statementList = statement as BoundStatementList;
            if (statementList != null)
            {
                return statementList.Statements.As<Semantics.IStatement>();
            }
            else if (statement == null)
            {
                return Statement.EmptyStatementArray;
            }
            
            return ImmutableArray.Create<Semantics.IStatement>(statement);
        }
    }

    partial class BoundForEachStatement : Semantics.IForEach
    {
        ILocalSymbol Semantics.IForEach.IterationVariable
        {
            get { return this.IterationVariable; }
        }

        Semantics.IExpression Semantics.IForEach.Collection
        {
            get { return this.Expression; }
        }

        Semantics.LoopKind Semantics.ILoop.LoopClass
        {
            get { return Semantics.LoopKind.ForEach; }
        }

        Semantics.IStatement Semantics.ILoop.Body
        {
            get { return this.Body; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.LoopStatement; }
        }
    }

    partial class BoundSwitchStatement: Semantics.ISwitch
    {
        Semantics.IExpression Semantics.ISwitch.Value
        {
            get { return this.BoundExpression; }
        }

        ImmutableArray<Semantics.ICase> Semantics.ISwitch.Cases
        {
            get { return this.SwitchSections.As<Semantics.ICase>(); }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.SwitchStatement; }
        }
    }

    partial class BoundSwitchSection : Semantics.ICase
    {
        ImmutableArray<Semantics.ICaseClause> Semantics.ICase.Clauses
        {
            get { return this.BoundSwitchLabels.As<Semantics.ICaseClause>(); }
        }

        ImmutableArray<Semantics.IStatement> Semantics.ICase.Body
        {
            get { return this.Statements.As<Semantics.IStatement>(); }
        }
    }

    partial class BoundSwitchLabel : Semantics.ISingleValueCaseClause
    {
        Semantics.IExpression Semantics.ISingleValueCaseClause.Value
        {
            get { return this.ExpressionOpt; }
        }

        Semantics.RelationalOperatorCode Semantics.ISingleValueCaseClause.Equality
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
                            return Semantics.RelationalOperatorCode.IntegerEqual;

                        case SpecialType.System_Boolean:
                            return Semantics.RelationalOperatorCode.BooleanEqual;

                        case SpecialType.System_String:
                            return Semantics.RelationalOperatorCode.StringEqual;
                    }

                    if (caseValue.Type.TypeKind == TypeKind.Enum)
                    {
                        return Semantics.RelationalOperatorCode.EnumEqual;
                    }
                }

                return Semantics.RelationalOperatorCode.None;
            }
        }

        Semantics.CaseKind Semantics.ICaseClause.CaseClass
        {
            get { return this.ExpressionOpt != null ? Semantics.CaseKind.SingleValue : Semantics.CaseKind.Default; }
        }
    }

    partial class BoundTryStatement : Semantics.ITry
    {
        Semantics.IBlock Semantics.ITry.Body
        {
            get { return this.TryBlock; }
        }

        ImmutableArray<Semantics.ICatch> Semantics.ITry.Catches
        {
            get { return this.CatchBlocks.As<Semantics.ICatch>(); }
        }

        Semantics.IBlock Semantics.ITry.FinallyHandler
        {
            get { return this.FinallyBlockOpt; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.TryStatement; }
        }
    }

    partial class BoundCatchBlock : Semantics.ICatch
    {
        Semantics.IBlock Semantics.ICatch.Handler
        {
            get { return this.Body; }
        }

        ITypeSymbol Semantics.ICatch.CaughtType
        {
            get { return this.ExceptionTypeOpt; }
        }

        Semantics.IExpression Semantics.ICatch.Filter
        {
            get { return this.ExceptionFilterOpt; }
        }

        ILocalSymbol Semantics.ICatch.ExceptionLocal
        {
            get { return this.LocalOpt; }
        }

        Semantics.OperationKind Semantics.IOperation.Kind
        {
            get { return Semantics.OperationKind.CatchHandler; }
        }

        SyntaxNode Semantics.IOperation.Syntax
        {
            get { return this.Syntax; }
        }
    }

    partial class BoundFixedStatement : Semantics.IFixed
    {
        ImmutableArray<ILocalSymbol> Semantics.IFixed.FixedLocals
        {
            get { return this.Locals.As<ILocalSymbol>(); }
        }

        Semantics.IVariableDeclaration Semantics.IFixed.Variables
        {
            get { return this.Declarations; }
        }

        Semantics.IStatement Semantics.IFixed.Body
        {
            get { return this.Body; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.FixedStatement; }
        }
    }

    partial class BoundUsingStatement: Semantics.IUsingWithDeclaration, Semantics.IUsingWithExpression
    {
        ImmutableArray<ILocalSymbol> Semantics.IUsingWithDeclaration.UsingLocals
        {
            get { return this.Locals.As<ILocalSymbol>(); }
        }

        Semantics.IVariableDeclaration Semantics.IUsingWithDeclaration.Variables
        {
            get { return this.DeclarationsOpt; }
        }

        Semantics.IExpression Semantics.IUsingWithExpression.Value
        {
            get { return this.ExpressionOpt; }
        }

        Semantics.IStatement Semantics.IUsing.Body
        {
            get { return this.Body; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return this.ExpressionOpt != null ? Semantics.OperationKind.UsingWithExpressionStatement : Semantics.OperationKind.UsingWithDeclarationStatement; }
        }
    }

    partial class BoundThrowStatement : Semantics.IThrow
    {
        Semantics.IExpression Semantics.IThrow.Thrown
        {
            get { return this.ExpressionOpt; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.ThrowStatement; }
        }
    }

    partial class BoundReturnStatement : Semantics.IReturn
    {
        Semantics.IExpression Semantics.IReturn.Returned
        {
            get { return this.ExpressionOpt; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.ReturnStatement; }
        }
    }

    partial class BoundYieldReturnStatement : Semantics.IReturn
    {
        Semantics.IExpression Semantics.IReturn.Returned
        {
            get { return this.Expression; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.YieldReturnStatement; }
        }
    }

    partial class BoundLockStatement : Semantics.ILock
    {
        Semantics.IExpression Semantics.ILock.Locked
        {
            get { return this.Argument; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.LockStatement; }
        }
    }

    partial class BoundBadStatement
    {
        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.None; }
        }
    }

    partial class BoundStatementList
    {
        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.None; }
        }
    }

    partial class BoundConditionalGoto
    {
        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.None; }
        }
    }

    partial class BoundSequencePoint
    {
        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.None; }
        }
    }

    partial class BoundSequencePointWithSpan
    {
        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.None; }
        }
    }

    partial class BoundStateMachineScope
    {
        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.None; }
        }
    }

    partial class BoundLocalDeclaration : Semantics.IVariableDeclaration, Semantics.IVariable
    {
        ImmutableArray<Semantics.IVariable> Semantics.IVariableDeclaration.Variables
        {
            get { return ImmutableArray.Create<Semantics.IVariable>(this); }
        }

        ILocalSymbol Semantics.IVariable.Variable
        {
            get { return this.LocalSymbol; }
        }

        Semantics.IExpression Semantics.IVariable.InitialValue
        {
            get { return this.InitializerOpt; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.VariableDeclarationStatement; }
        }
    }

    partial class BoundMultipleLocalDeclarations : Semantics.IVariableDeclaration
    {
        ImmutableArray<Semantics.IVariable> Semantics.IVariableDeclaration.Variables
        {
            get { return this.LocalDeclarations.As<Semantics.IVariable>(); }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.VariableDeclarationStatement; }
        }
    }

    partial class BoundLabelStatement : Semantics.ILabel
    {
        ILabelSymbol Semantics.ILabel.Label
        {
            get { return this.Label; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.LabelStatement; }
        }
    }

    partial class BoundLabeledStatement : Semantics.ILabeled
    {
        Semantics.IStatement Semantics.ILabeled.Target
        {
            get { return this.Body; }
        }

        ILabelSymbol Semantics.ILabel.Label
        {
            get { return this.Label; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.LabeledStatement; }
        }
    }

    partial class BoundExpressionStatement:Semantics.IExpressionStatement
    {
        Semantics.IExpression Semantics.IExpressionStatement.Expression
        {
            get { return this.Expression; }
        }

        protected override Semantics.OperationKind StatementKind
        {
            get { return Semantics.OperationKind.ExpressionStatement; }
        }
    }

    class Statement
    {
        internal static readonly ImmutableArray<Semantics.IStatement> EmptyStatementArray = ImmutableArray.Create<Semantics.IStatement>();
    }
}
