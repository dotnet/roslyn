// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a block scope.
    /// </summary>
    public interface IBlockStatement : IOperation
    {
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        ImmutableArray<IOperation> Statements { get; }
        /// <summary>
        /// Local declarations contained within the block.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    public interface IVariableDeclarationStatement : IOperation
    {
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        ImmutableArray<IVariableDeclaration> Variables { get; }
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    public interface IVariableDeclaration : IOperation
    {
        /// <summary>
        /// Variable declared by the declaration.
        /// </summary>
        ILocalSymbol Variable { get; }
        /// <summary>
        /// Initializer of the variable.
        /// </summary>
        IExpression InitialValue { get; }
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    public interface ISwitchStatement : IOperation
    {
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        IExpression Value { get; }
        /// <summary>
        /// Cases of the switch.
        /// </summary>
        ImmutableArray<ISwitchCase> Cases { get; }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    public interface ISwitchCase : IOperation
    {
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        ImmutableArray<ICaseClause> Clauses { get; }
        /// <summary>
        /// Statements of the case.
        /// </summary>
        ImmutableArray<IOperation> Body { get; }
    }

    /// <summary>
    /// Represents a clause of a C# case or a VB Case.
    /// </summary>
    public interface ICaseClause : IOperation
    {
        /// <summary>
        /// Kind of the clause.
        /// </summary>
        CaseKind CaseKind { get; }
    }

    /// <summary>
    /// Kinds of cases.
    /// </summary>
    public enum CaseKind
    {
        /// <summary>
        /// Indicates case x in C# or Case x in VB.
        /// </summary>
        SingleValue,
        /// <summary>
        /// Indicates Case Is op x in VB.
        /// </summary>
        Relational,
        /// <summary>
        /// Indicates Case x To Y in VB.
        /// </summary>
        Range,
        /// <summary>
        /// Indicates default in C# or Case Else in VB.
        /// </summary>
        Default
    }

    /// <summary>
    /// Represents case x in C# or Case x in VB.
    /// </summary>
    public interface ISingleValueCaseClause : ICaseClause
    {
        /// <summary>
        /// Case value.
        /// </summary>
        IExpression Value { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        BinaryOperationKind Equality { get; }
    }

    /// <summary>
    /// Represents Case Is op x in VB.
    /// </summary>
    public interface IRelationalCaseClause : ICaseClause
    {
        /// <summary>
        /// Case value.
        /// </summary>
        IExpression Value { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value. 
        /// </summary>
        BinaryOperationKind Relation { get; }
    }

    /// <summary>
    /// Represents Case x To y in VB.
    /// </summary>
    public interface IRangeCaseClause : ICaseClause
    {
        /// <summary>
        /// Minimum value of the case range.
        /// </summary>
        IExpression MinimumValue { get; }
        /// <summary>
        /// Maximum value of the case range.
        /// </summary>
        IExpression MaximumValue { get; }
    }

    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    public interface IIfStatement : IOperation
    {
        /// <summary>
        /// Condition of the if statement. For C# there is naturally one clause per if, but for VB If statements with multiple clauses are rewritten to have only one.
        /// </summary>
        IExpression Condition { get; }
        /// <summary>
        /// Statement executed if the condition is true.
        /// </summary>
        IOperation IfTrue { get; }
        /// <summary>
        /// Statement executed if the condition is false.
        /// </summary>
        IOperation IfFalse { get; }
    }

    /// <summary>
    /// Represents a C# while, for, foreach, or do statement, or a VB While, For, For Each, or Do statement.
    /// </summary>
    public interface ILoopStatement : IOperation
    {
        /// <summary>
        /// Kind of the loop.
        /// </summary>
        LoopKind LoopKind { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        IOperation Body { get; }
    }

    /// <summary>
    /// Kinds of loops.
    /// </summary>
    public enum LoopKind
    {
        /// <summary>
        /// Indicates a C# while or do loop, or a VB While or Do loop.
        /// </summary>
        WhileUntil = 0x0,
        /// <summary>
        /// Indicates a C# for loop or a VB For loop.
        /// </summary>
        For = 0x1,
        /// <summary>
        /// Indicates a C# foreach loop or a VB For Each loop.
        /// </summary>
        ForEach = 0x2
    }

    /// <summary>
    /// Represents a C# while, for, or do statement, or a VB While, For, or Do statement.
    /// </summary>
    public interface IForWhileUntilLoopStatement : ILoopStatement
    {
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        IExpression Condition { get; }
    }

    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    public interface IWhileUntilLoopStatement : IForWhileUntilLoopStatement
    {
        /// <summary>
        /// True if the loop test executes at the top of the loop; false if the loop test executes at the bottom of the loop.
        /// </summary>
        bool IsTopTest { get; }
        /// <summary>
        /// True if the loop is a while loop; false if the loop is an until loop.
        /// </summary>
        bool IsWhile { get; }
    }

    /// <summary>
    /// Represents a C# for statement or a VB For statement.
    /// </summary>
    public interface IForLoopStatement : IForWhileUntilLoopStatement
    {
        /// <summary>
        /// Statements to execute before entry to the loop. For C# these come from the first clause of the for statement. For VB these initialize the index variable of the For statement.
        /// </summary>
        ImmutableArray<IOperation> Before { get; }
        /// <summary>
        /// Statements to execute at the bottom of the loop. For C# these come from the third clause of the for statement. For VB these increment the index variable of the For statement.
        /// </summary>
        ImmutableArray<IOperation> AtLoopBottom { get; }
        /// <summary>
        /// Declarations local to the loop.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }

    /// <summary>
    /// Represents a C# foreach statement or a VB For Each staement.
    /// </summary>
    public interface IForEachLoopStatement : ILoopStatement
    {
        /// <summary>
        /// Iteration variable of the loop.
        /// </summary>
        ILocalSymbol IterationVariable { get; }
        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        IExpression Collection { get; }
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    public interface ILabelStatement : IOperation
    {
        /// <summary>
        ///  Label that can be the target of branches.
        /// </summary>
        ILabelSymbol Label { get; }
    }

    /// <summary>
    /// Represents a C# label statement.
    /// </summary>
    public interface ILabeledStatement : ILabelStatement
    {
        /// <summary>
        /// Statement that has been labeled.
        /// </summary>
        IOperation Labeled { get; }
    }

    /// <summary>
    /// Represents a C# goto, break, or continue statement, or a VB GoTo, Exit ***, or Continue *** statement
    /// </summary>
    public interface IBranchStatement : IOperation
    {
        /// <summary>
        /// Label that is the target of the branch.
        /// </summary>
        ILabelSymbol Target { get; }
        /// <summary>
        /// Kind of the branch.
        /// </summary>
        BranchKind BranchKind { get; }
    }

    public enum BranchKind
    {
        Continue = 0x0,
        Break = 0x1,
        GoTo = 0x2
    }

    /// <summary>
    /// Represents a C# throw or a VB Throw statement.
    /// </summary>
    public interface IThrowStatement : IOperation
    {
        /// <summary>
        /// Value to be thrown.
        /// </summary>
        IExpression ThrownObject { get; }
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    public interface IReturnStatement : IOperation
    {
        /// <summary>
        /// Value to be returned.
        /// </summary>
        IExpression ReturnedValue { get; }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    public interface ILockStatement : IOperation
    {
        /// <summary>
        /// Value to be locked.
        /// </summary>
        IExpression LockedObject { get; }
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        IOperation Body { get; }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    public interface ITryStatement : IOperation
    {
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        IBlockStatement Body { get; }
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        ImmutableArray<ICatchClause> Catches { get; }
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        IBlockStatement FinallyHandler { get; }
    }

    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    public interface ICatchClause : IOperation
    {
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        IBlockStatement Handler { get; }
        /// <summary>
        /// Type of exception to be handled.
        /// </summary>
        ITypeSymbol CaughtType { get; }
        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        IExpression Filter { get; }
        /// <summary>
        /// Symbol for the local catch variable bound to the caught exception.
        /// </summary>
        ILocalSymbol ExceptionLocal { get; }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    public interface IUsingStatement : IOperation
    {
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        IOperation Body { get; }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement that declares one or more local variables for the resources held by the using.
    /// </summary>
    public interface IUsingWithDeclarationStatement : IUsingStatement
    {
        /// <summary>
        /// Declaration of variables introduced by the using.
        /// </summary>
        IVariableDeclarationStatement Declaration { get; }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement that uses an expression for the resource held by the using.
    /// </summary>
    public interface IUsingWithExpressionStatement : IUsingStatement
    {
        /// <summary>
        /// Resource held by the using.
        /// </summary>
        IExpression Value { get; }
    }

    /// <summary>
    /// Represents a C# fixed staement.
    /// </summary>
    public interface IFixedStatement : IOperation
    {
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        IVariableDeclarationStatement Variables { get; }
        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        IOperation Body { get; }
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    public interface IExpressionStatement : IOperation
    {
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        IExpression Expression { get; }
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    public interface IWithStatement : IOperation
    {
        /// <summary>
        /// Body of the with.
        /// </summary>
        IOperation Body { get; }
        /// <summary>
        /// Value to whose members leading-dot-qualified references within the with body bind.
        /// </summary>
        IExpression Value { get; }
    }

    /// <summary>
    /// Reprsents an empty statement.
    /// </summary>
    public interface IEmptyStatement : IOperation
    {
    }

    /// <summary>
    /// Represents a VB Stop statement.
    /// </summary>
    public interface IStopStatement : IOperation
    {
    }

    /// <summary>
    /// Represents a VB End statemnt.
    /// </summary>
    public interface IEndStatement : IOperation
    {
    }

    /// <summary>
    /// Represents a syntactically or semantically invalid C# or VB statement.
    /// </summary>
    public interface IInvalidStatement : IOperation
    {
    }
}
