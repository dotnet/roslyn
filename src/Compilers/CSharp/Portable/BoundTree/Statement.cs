// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundStatement : IStatement
    {
        OperationKind IOperation.Kind => this.StatementKind;

        SyntaxNode IOperation.Syntax => this.Syntax;

        protected abstract OperationKind StatementKind { get; }
    }

    partial class BoundBlock : IBlock
    {
        ImmutableArray<IStatement> IBlock.Statements => this.Statements.As<IStatement>();

        ImmutableArray<ILocalSymbol> IBlock.Locals => this.Locals.As<ILocalSymbol>();

        protected override OperationKind StatementKind => OperationKind.BlockStatement;
    }

    partial class BoundContinueStatement : IBranch
    {
        ILabelSymbol IBranch.Target => this.Label;

        protected override OperationKind StatementKind => OperationKind.ContinueStatement;
    }

    partial class BoundBreakStatement : IBranch
    {
        ILabelSymbol IBranch.Target => this.Label;

        protected override OperationKind StatementKind => OperationKind.BreakStatement;
    }

    partial class BoundYieldBreakStatement
    {
        protected override OperationKind StatementKind => OperationKind.YieldBreakStatement;
    }

    partial class BoundGotoStatement : IBranch
    {
        ILabelSymbol IBranch.Target => this.Label;

        protected override OperationKind StatementKind => OperationKind.GoToStatement;
    }

    partial class BoundNoOpStatement
    {
        protected override OperationKind StatementKind => OperationKind.EmptyStatement;
    }

    partial class BoundIfStatement : IIf, IIfClause
    {
        ImmutableArray<IIfClause> IIf.IfClauses => ImmutableArray.Create<IIfClause>(this);

        IStatement IIf.Else => this.AlternativeOpt;

        IExpression IIfClause.Condition => this.Condition;

        IStatement IIfClause.Body => this.Consequence;

        protected override OperationKind StatementKind => OperationKind.IfStatement;
    }

    partial class BoundWhileStatement : IWhileUntil
    {
        bool IWhileUntil.IsTopTest => true;

        bool IWhileUntil.IsWhile => true;

        IExpression IForWhileUntil.Condition => this.Condition;

        LoopKind ILoop.LoopKind => LoopKind.WhileUntil;

        IStatement ILoop.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.LoopStatement;
    }

    partial class BoundDoStatement : IWhileUntil
    {
        bool IWhileUntil.IsTopTest => false;

        bool IWhileUntil.IsWhile => true;

        IExpression IForWhileUntil.Condition => this.Condition;

        LoopKind ILoop.LoopKind => LoopKind.WhileUntil;

        IStatement ILoop.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.LoopStatement;
    }

    partial class BoundForStatement : IFor
    {
        ImmutableArray<IStatement> IFor.Before => ToStatements(this.Initializer);

        ImmutableArray<IStatement> IFor.AtLoopBottom => ToStatements(this.Increment);

        ImmutableArray<ILocalSymbol> IFor.Locals => this.OuterLocals.As<ILocalSymbol>();

        IExpression IForWhileUntil.Condition => this.Condition;

        LoopKind ILoop.LoopKind => LoopKind.For;

        IStatement ILoop.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.LoopStatement;

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
        ILocalSymbol IForEach.IterationVariable => this.IterationVariable;

        IExpression IForEach.Collection => this.Expression;

        LoopKind ILoop.LoopKind => LoopKind.ForEach;

        IStatement ILoop.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.LoopStatement;
    }

    partial class BoundSwitchStatement: ISwitch
    {
        IExpression ISwitch.Value => this.BoundExpression;

        ImmutableArray<ICase> ISwitch.Cases => this.SwitchSections.As<ICase>();

        protected override OperationKind StatementKind => OperationKind.SwitchStatement;
    }

    partial class BoundSwitchSection : ICase
    {
        ImmutableArray<ICaseClause> ICase.Clauses => this.BoundSwitchLabels.As<ICaseClause>();

        ImmutableArray<IStatement> ICase.Body => this.Statements.As<IStatement>();
    }

    partial class BoundSwitchLabel : ISingleValueCaseClause
    {
        IExpression ISingleValueCaseClause.Value => this.ExpressionOpt;

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

        CaseKind ICaseClause.CaseKind => this.ExpressionOpt != null ? CaseKind.SingleValue : CaseKind.Default;
    }

    partial class BoundTryStatement : ITry
    {
        IBlock ITry.Body => this.TryBlock;

        ImmutableArray<ICatch> ITry.Catches => this.CatchBlocks.As<ICatch>();

        IBlock ITry.FinallyHandler => this.FinallyBlockOpt;

        protected override OperationKind StatementKind => OperationKind.TryStatement;
    }

    partial class BoundCatchBlock : ICatch
    {
        IBlock ICatch.Handler => this.Body;

        ITypeSymbol ICatch.CaughtType => this.ExceptionTypeOpt;

        IExpression ICatch.Filter => this.ExceptionFilterOpt;

        ILocalSymbol ICatch.ExceptionLocal => this.LocalOpt;

        OperationKind IOperation.Kind => OperationKind.CatchHandler;

        SyntaxNode IOperation.Syntax => this.Syntax;
    }

    partial class BoundFixedStatement : IFixed
    {
        IVariableDeclaration IFixed.Variables => this.Declarations;

        IStatement IFixed.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.FixedStatement;
    }

    partial class BoundUsingStatement: IUsingWithDeclaration, IUsingWithExpression
    {
        IVariableDeclaration IUsingWithDeclaration.Variables => this.DeclarationsOpt;

        IExpression IUsingWithExpression.Value => this.ExpressionOpt;

        IStatement IUsing.Body => this.Body;

        protected override OperationKind StatementKind => this.ExpressionOpt != null ? OperationKind.UsingWithExpressionStatement : OperationKind.UsingWithDeclarationStatement;
    }

    partial class BoundThrowStatement : IThrow
    {
        IExpression IThrow.Thrown => this.ExpressionOpt;

        protected override OperationKind StatementKind => OperationKind.ThrowStatement;
    }

    partial class BoundReturnStatement : IReturn
    {
        IExpression IReturn.Returned => this.ExpressionOpt;

        protected override OperationKind StatementKind => OperationKind.ReturnStatement;
    }

    partial class BoundYieldReturnStatement : IReturn
    {
        IExpression IReturn.Returned => this.Expression;

        protected override OperationKind StatementKind => OperationKind.YieldReturnStatement;
    }

    partial class BoundLockStatement : ILock
    {
        IExpression ILock.Locked => this.Argument;

        IStatement ILock.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.LockStatement;
    }

    partial class BoundBadStatement
    {
        protected override OperationKind StatementKind => OperationKind.None;
    }

    partial class BoundStatementList
    {
        protected override OperationKind StatementKind => OperationKind.None;
    }

    partial class BoundConditionalGoto
    {
        protected override OperationKind StatementKind => OperationKind.None;
    }

    partial class BoundSequencePoint
    {
        protected override OperationKind StatementKind => OperationKind.None;
    }

    partial class BoundSequencePointWithSpan
    {
        protected override OperationKind StatementKind => OperationKind.None;
    }

    partial class BoundStateMachineScope
    {
        protected override OperationKind StatementKind => OperationKind.None;
    }

    partial class BoundLocalDeclaration : IVariableDeclaration, IVariable
    {
        ImmutableArray<IVariable> IVariableDeclaration.Variables => ImmutableArray.Create<IVariable>(this);

        ILocalSymbol IVariable.Variable => this.LocalSymbol;

        IExpression IVariable.InitialValue => this.InitializerOpt;

        protected override OperationKind StatementKind => OperationKind.VariableDeclarationStatement;
    }

    partial class BoundMultipleLocalDeclarations : IVariableDeclaration
    {
        ImmutableArray<IVariable> IVariableDeclaration.Variables => this.LocalDeclarations.As<IVariable>();

        protected override OperationKind StatementKind => OperationKind.VariableDeclarationStatement;
    }

    partial class BoundLabelStatement : ILabel
    {
        ILabelSymbol ILabel.Label => this.Label;

        protected override OperationKind StatementKind => OperationKind.LabelStatement;
    }

    partial class BoundLabeledStatement : ILabeled
    {
        IStatement ILabeled.Labeled => this.Body;

        ILabelSymbol ILabel.Label => this.Label;

        protected override OperationKind StatementKind => OperationKind.LabeledStatement;
    }

    partial class BoundExpressionStatement:IExpressionStatement
    {
        IExpression IExpressionStatement.Expression => this.Expression;

        protected override OperationKind StatementKind => OperationKind.ExpressionStatement;
    }

}
