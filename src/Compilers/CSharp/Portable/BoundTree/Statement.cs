// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundStatement : IStatement
    {
        OperationKind IOperation.Kind => this.StatementKind;

        bool IOperation.IsInvalid => this.HasErrors;

        SyntaxNode IOperation.Syntax => this.Syntax;

        protected abstract OperationKind StatementKind { get; }
    }

    partial class BoundBlock : IBlockStatement
    {
        ImmutableArray<IStatement> IBlockStatement.Statements => this.Statements.As<IStatement>();

        ImmutableArray<ILocalSymbol> IBlockStatement.Locals => this.Locals.As<ILocalSymbol>();

        protected override OperationKind StatementKind => OperationKind.BlockStatement;
    }

    partial class BoundContinueStatement : IBranchStatement
    {
        ILabelSymbol IBranchStatement.Target => this.Label;

        protected override OperationKind StatementKind => OperationKind.ContinueStatement;
    }

    partial class BoundBreakStatement : IBranchStatement
    {
        ILabelSymbol IBranchStatement.Target => this.Label;

        protected override OperationKind StatementKind => OperationKind.BreakStatement;
    }

    partial class BoundYieldBreakStatement
    {
        protected override OperationKind StatementKind => OperationKind.YieldBreakStatement;
    }

    partial class BoundGotoStatement : IBranchStatement
    {
        ILabelSymbol IBranchStatement.Target => this.Label;

        protected override OperationKind StatementKind => OperationKind.GoToStatement;
    }

    partial class BoundNoOpStatement
    {
        protected override OperationKind StatementKind => OperationKind.EmptyStatement;
    }

    partial class BoundIfStatement : IIfStatement
    {
        IExpression IIfStatement.Condition => this.Condition;

        IStatement IIfStatement.IfTrue => this.Consequence;

        IStatement IIfStatement.IfFalse => this.AlternativeOpt;

        protected override OperationKind StatementKind => OperationKind.IfStatement;
    }

    partial class BoundWhileStatement : IWhileUntilLoopStatement
    {
        bool IWhileUntilLoopStatement.IsTopTest => true;

        bool IWhileUntilLoopStatement.IsWhile => true;

        IExpression IForWhileUntilLoopStatement.Condition => this.Condition;

        LoopKind ILoopStatement.LoopKind => LoopKind.WhileUntil;

        IStatement ILoopStatement.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.LoopStatement;
    }

    partial class BoundDoStatement : IWhileUntilLoopStatement
    {
        bool IWhileUntilLoopStatement.IsTopTest => false;

        bool IWhileUntilLoopStatement.IsWhile => true;

        IExpression IForWhileUntilLoopStatement.Condition => this.Condition;

        LoopKind ILoopStatement.LoopKind => LoopKind.WhileUntil;

        IStatement ILoopStatement.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.LoopStatement;
    }

    partial class BoundForStatement : IForLoopStatement
    {
        ImmutableArray<IStatement> IForLoopStatement.Before => ToStatements(this.Initializer);

        ImmutableArray<IStatement> IForLoopStatement.AtLoopBottom => ToStatements(this.Increment);

        ImmutableArray<ILocalSymbol> IForLoopStatement.Locals => this.OuterLocals.As<ILocalSymbol>();

        IExpression IForWhileUntilLoopStatement.Condition => this.Condition;

        LoopKind ILoopStatement.LoopKind => LoopKind.For;

        IStatement ILoopStatement.Body => this.Body;

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

    partial class BoundForEachStatement : IForEachLoopStatement
    {
        ILocalSymbol IForEachLoopStatement.IterationVariable => this.IterationVariable;

        IExpression IForEachLoopStatement.Collection => this.Expression;

        LoopKind ILoopStatement.LoopKind => LoopKind.ForEach;

        IStatement ILoopStatement.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.LoopStatement;
    }

    partial class BoundSwitchStatement : ISwitchStatement
    {
        private static readonly ConditionalWeakTable<BoundSwitchStatement, object> s_switchSectionsMappings =
            new ConditionalWeakTable<BoundSwitchStatement, object>();

        IExpression ISwitchStatement.Value => this.Expression;

        ImmutableArray<ICase> ISwitchStatement.Cases
        {
            get
            {
                return (ImmutableArray<ICase>) s_switchSectionsMappings.GetValue(this, 
                    switchStatement =>
                    {
                        return switchStatement.SwitchSections.SelectAsArray(switchSection => (ICase)new SwitchSection(switchSection));   
                    });
            }
        }

        protected override OperationKind StatementKind => OperationKind.SwitchStatement;

        private class SwitchSection : ICase
        {
            public SwitchSection(BoundSwitchSection boundNode)
            {
                this.Body = boundNode.Statements.As<IStatement>();
                this.Clauses = boundNode.SwitchLabels.As<ICaseClause>();
                this.IsInvalid = boundNode.HasErrors;
                this.Syntax = boundNode.Syntax;
    }

            public ImmutableArray<IStatement> Body { get; }

            public ImmutableArray<ICaseClause> Clauses { get; }

            public bool IsInvalid { get; }

            public OperationKind Kind => OperationKind.SwitchSection;

            public SyntaxNode Syntax { get; }
        }
    }

    partial class BoundSwitchSection
    {
        protected override OperationKind StatementKind => OperationKind.None;
    }

    partial class BoundSwitchLabel : ISingleValueCaseClause
    {
        IExpression ISingleValueCaseClause.Value => this.ExpressionOpt;

        BinaryOperationKind ISingleValueCaseClause.Equality
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
                            return BinaryOperationKind.IntegerEquals;

                        case SpecialType.System_Boolean:
                            return BinaryOperationKind.BooleanEquals;

                        case SpecialType.System_String:
                            return BinaryOperationKind.StringEquals;
                    }

                    if (caseValue.Type.TypeKind == TypeKind.Enum)
                    {
                        return BinaryOperationKind.EnumEquals;
                    }
                }

                return BinaryOperationKind.None;
            }
        }

        CaseKind ICaseClause.CaseKind => this.ExpressionOpt != null ? CaseKind.SingleValue : CaseKind.Default;

        OperationKind IOperation.Kind => OperationKind.SingleValueCaseClause;

        bool IOperation.IsInvalid => this.HasErrors;

        SyntaxNode IOperation.Syntax => this.Syntax;
    }

    partial class BoundTryStatement : ITryStatement
    {
        IBlockStatement ITryStatement.Body => this.TryBlock;

        ImmutableArray<ICatch> ITryStatement.Catches => this.CatchBlocks.As<ICatch>();

        IBlockStatement ITryStatement.FinallyHandler => this.FinallyBlockOpt;

        protected override OperationKind StatementKind => OperationKind.TryStatement;
    }

    partial class BoundCatchBlock : ICatch
    {
        IBlockStatement ICatch.Handler => this.Body;

        ITypeSymbol ICatch.CaughtType => this.ExceptionTypeOpt;

        IExpression ICatch.Filter => this.ExceptionFilterOpt;

        ILocalSymbol ICatch.ExceptionLocal => this.LocalOpt;

        OperationKind IOperation.Kind => OperationKind.CatchHandler;

        bool IOperation.IsInvalid => this.Body.HasErrors || (this.ExceptionFilterOpt != null && this.ExceptionFilterOpt.HasErrors);

        SyntaxNode IOperation.Syntax => this.Syntax;
    }

    partial class BoundFixedStatement : IFixedStatement
    {
        IVariableDeclarationStatement IFixedStatement.Variables => this.Declarations;

        IStatement IFixedStatement.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.FixedStatement;
    }

    partial class BoundUsingStatement : IUsingWithDeclarationStatement, IUsingWithExpressionStatement
    {
        IVariableDeclarationStatement IUsingWithDeclarationStatement.Variables => this.DeclarationsOpt;

        IExpression IUsingWithExpressionStatement.Value => this.ExpressionOpt;

        IStatement IUsingStatement.Body => this.Body;

        protected override OperationKind StatementKind => this.ExpressionOpt != null ? OperationKind.UsingWithExpressionStatement : OperationKind.UsingWithDeclarationStatement;
    }

    partial class BoundThrowStatement : IThrowStatement
    {
        IExpression IThrowStatement.Thrown => this.ExpressionOpt;

        protected override OperationKind StatementKind => OperationKind.ThrowStatement;
    }

    partial class BoundReturnStatement : IReturnStatement
    {
        IExpression IReturnStatement.Returned => this.ExpressionOpt;

        protected override OperationKind StatementKind => OperationKind.ReturnStatement;
    }

    partial class BoundYieldReturnStatement : IReturnStatement
    {
        IExpression IReturnStatement.Returned => this.Expression;

        protected override OperationKind StatementKind => OperationKind.YieldReturnStatement;
    }

    partial class BoundLockStatement : ILockStatement
    {
        IExpression ILockStatement.Locked => this.Argument;

        IStatement ILockStatement.Body => this.Body;

        protected override OperationKind StatementKind => OperationKind.LockStatement;
    }

    partial class BoundBadStatement
    {
        protected override OperationKind StatementKind => OperationKind.InvalidStatement;
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

    partial class BoundLocalDeclaration : IVariableDeclarationStatement
    {
        private static readonly ConditionalWeakTable<BoundLocalDeclaration, object> s_variablesMappings =
            new ConditionalWeakTable<BoundLocalDeclaration, object>();

        ImmutableArray<IVariable> IVariableDeclarationStatement.Variables
        {
            get
            {
                return (ImmutableArray<IVariable>) s_variablesMappings.GetValue(this, 
                    declaration => ImmutableArray.Create<IVariable>(new VariableDeclaration(declaration.LocalSymbol, declaration.InitializerOpt, declaration.Syntax)));
            }
        }

        protected override OperationKind StatementKind => OperationKind.VariableDeclarationStatement;
    }

    partial class BoundMultipleLocalDeclarations : IVariableDeclarationStatement
    {
        private static readonly ConditionalWeakTable<BoundMultipleLocalDeclarations, object> s_variablesMappings =
            new ConditionalWeakTable<BoundMultipleLocalDeclarations, object>();

        ImmutableArray<IVariable> IVariableDeclarationStatement.Variables
        {
            get
            {
                return (ImmutableArray<IVariable>)s_variablesMappings.GetValue(this,
                    multipleDeclarations =>
                        multipleDeclarations.LocalDeclarations.SelectAsArray(declaration => 
                            (IVariable)new VariableDeclaration(declaration.LocalSymbol, declaration.InitializerOpt, declaration.Syntax)));
            }
        }

        protected override OperationKind StatementKind => OperationKind.VariableDeclarationStatement;
    }

    partial class BoundLabelStatement : ILabelStatement
    {
        ILabelSymbol ILabelStatement.Label => this.Label;

        protected override OperationKind StatementKind => OperationKind.LabelStatement;
    }

    partial class BoundLabeledStatement : ILabeledStatement
    {
        IStatement ILabeledStatement.Labeled => this.Body;

        ILabelSymbol ILabelStatement.Label => this.Label;

        protected override OperationKind StatementKind => OperationKind.LabeledStatement;
    }

    partial class BoundExpressionStatement : IExpressionStatement
    {
        IExpression IExpressionStatement.Expression => this.Expression;

        protected override OperationKind StatementKind => OperationKind.ExpressionStatement;
    }

    partial class BoundLocalFunctionStatement
    {
        protected override OperationKind StatementKind => OperationKind.LocalFunctionStatement;
    }

    partial class BoundPatternSwitchStatement
    {
        // TODO: this may need its own OperationKind.
        protected override OperationKind StatementKind => OperationKind.SwitchStatement;
    }

    partial class BoundLetStatement
    {
        // TODO: this may need its own OperationKind.
        protected override OperationKind StatementKind => OperationKind.IfStatement;
    }
}
