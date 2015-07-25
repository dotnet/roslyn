using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements.
    /// </summary>
    public interface IStatement : IOperation
    {
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    public interface IBlock : IStatement
    {
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        ImmutableArray<IStatement> Statements { get; }
        /// <summary>
        /// Local declarations contained within the block.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    public interface IVariableDeclaration : IStatement
    {
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        ImmutableArray<IVariable> Variables { get; }
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    public interface IVariable
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
    public interface ISwitch : IStatement
    {
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        IExpression Value { get; }
        /// <summary>
        /// Cases of the switch.
        /// </summary>
        ImmutableArray<ICase> Cases { get; }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    public interface ICase
    {
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        ImmutableArray<ICaseClause> Clauses { get; }
        /// <summary>
        /// Statements of the case.
        /// </summary>
        ImmutableArray<IStatement> Body { get; }
    }

    /// <summary>
    /// Represents a clause of a C# case or a VB Case.
    /// </summary>
    public interface ICaseClause
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
        /// Indicates default: in C3 or Case Else in VB.
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
        RelationalOperatorCode Equality { get; }
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
        RelationalOperatorCode Relation { get; }
    }

    // Represents Case x To y in VB.
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
    public interface IIf : IStatement
    {
        /// <summary>
        /// Clauses of the if. For C# there is one clause per if, but for VB there can be multiple.
        /// </summary>
        ImmutableArray<IIfClause> IfClauses { get; }
        /// <summary>
        /// Else of the if statement.
        /// </summary>
        IStatement Else { get; }
    }

    /// <summary>
    /// Represents a conditional clause of an if statement.
    /// </summary>
    public interface IIfClause
    {
        /// <summary>
        /// Condition of the clause.
        /// </summary>
        IExpression Condition { get; }
        /// <summary>
        /// Body of the clause.
        /// </summary>
        IStatement Body { get; }
    }

    /// <summary>
    /// Represents a C# while, for, foreach, or do statement, or a VB While, For, For Each, or Do statement.
    /// </summary>
    public interface ILoop : IStatement
    {
        /// <summary>
        /// Kind of the loop.
        /// </summary>
        LoopKind LoopKind { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        IStatement Body { get; }
    }

    /// <summary>
    /// Kinds of loops.
    /// </summary>
    public enum LoopKind
    {
        /// <summary>
        /// Indicates a C# while or do loop, or a VB While or Do loop.
        /// </summary>
        WhileUntil,
        /// <summary>
        /// Indicates a C# for loop or a VB For loop.
        /// </summary>
        For,
        /// <summary>
        /// Indicates a C# foreach loop or a VB For Each loop.
        /// </summary>
        ForEach
    }

    /// <summary>
    /// Represents a C# while, for, or do statement, or a VB While, For, or Do statement.
    /// </summary>
    public interface IForWhileUntil : ILoop
    {
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        IExpression Condition { get; }
    }

    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    public interface IWhileUntil : IForWhileUntil
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
    public interface IFor : IForWhileUntil
    {
        /// <summary>
        /// Statements to execute before entry to the loop. For C# these come from the first clause of the for statement. For VB these initialize the index variable of the For statement.
        /// </summary>
        ImmutableArray<IStatement> Before { get; }
        /// <summary>
        /// Statements to execute at the bottom of the loop. For C# these come from the third clause of the for statement. For VB these increment the index variable of the For statement.
        /// </summary>
        ImmutableArray<IStatement> AtLoopBottom { get; }
        /// <summary>
        /// Declarations local to the loop.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }

    /// <summary>
    /// Represents a C# foreach statement or a VB For Each staement.
    /// </summary>
    public interface IForEach : ILoop
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
    public interface ILabel : IStatement
    {
        // Label that can be the target of branches.
        ILabelSymbol Label { get; }
    }

    /// <summary>
    /// Represents a C# label statement.
    /// </summary>
    public interface ILabeled: ILabel
    {
        // Statement that has been labeled.
        IStatement Labeled { get; }
    }
    
    /// <summary>
    /// Represents a C# goto, break, or continue statement, or a VB GoTo, Exit ***, or Continue *** statement
    /// </summary>
    public interface IBranch : IStatement
    {
        // Label that is the target of the branch.
        ILabelSymbol Target { get; }
    }

    /// <summary>
    /// Represents a C# throw or a VB Throw statement.
    /// </summary>
    public interface IThrow : IStatement
    {
        // Thrown expression.
        IExpression Thrown { get; }
    }

    public interface IReturn : IStatement
    {
        IExpression Returned { get; }
    }

    public interface ILock : IStatement
    {
        IExpression Locked { get; }
    }

    public interface ITry: IStatement
    {
        IBlock Body { get; }
        ImmutableArray<ICatch> Catches { get; }
        IBlock FinallyHandler { get; }
    }

    public interface ICatch : IOperation
    {
        IBlock Handler { get; }
        ITypeSymbol CaughtType { get; }
        IExpression Filter { get; }
        ILocalSymbol ExceptionLocal { get; }
    }

    public interface IUsing: IStatement
    {
        IStatement Body { get; }
    }

    public interface IUsingWithDeclaration : IUsing
    {
        ImmutableArray<ILocalSymbol> UsingLocals { get; }
        IVariableDeclaration Variables { get; }
    }

    public interface IUsingWithExpression : IUsing
    {
        IExpression Value { get; }
    }

    public interface IFixed : IStatement
    {
        ImmutableArray<ILocalSymbol> FixedLocals { get; }
        IVariableDeclaration Variables { get; }
        IStatement Body { get; }
    }

    public interface IExpressionStatement : IStatement
    {
        IExpression Expression { get; }
    }

    public interface IWith : IStatement
    {
        IStatement Body { get; }
        IExpression Value { get; }
    }
}
