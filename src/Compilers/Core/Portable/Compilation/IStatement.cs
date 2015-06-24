using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    public interface IStatement : IOperation
    {
    }

    public interface IBlock : IStatement
    {
        ImmutableArray<IStatement> Statements { get; }
        ImmutableArray<ILocalSymbol> Locals { get; }
    }

    public interface IVariableDeclaration : IStatement
    {
        ImmutableArray<IVariable> Variables { get; }
    }

    public interface IVariable
    {
        ILocalSymbol Variable { get; }
        IExpression InitialValue { get; }
    }

    public interface ISwitch : IStatement
    {
        IExpression Value { get; }
        ImmutableArray<ICase> Cases { get; }
    }

    public interface ICase
    {
        ImmutableArray<ICaseClause> Clauses { get; }
        ImmutableArray<IStatement> Body { get; }
    }

    public interface ICaseClause
    {
        CaseKind CaseClass { get; }
    }

    public interface ISingleValueCaseClause : ICaseClause
    {
        IExpression Value { get; }
        RelationalOperatorCode Equality { get; }
    }

    public interface IRelationalCaseClause : ICaseClause
    {
        RelationalOperatorCode Relation { get; }
        IExpression Value { get; }
    }

    public interface IRangeCaseClause : ICaseClause
    {
        IExpression MinimumValue { get; }
        IExpression MaximumValue { get; }
    }

    public enum CaseKind
    {
        SingleValue,
        Relational,
        Range,
        Default
    }

    public interface IIf : IStatement
    {
        ImmutableArray<IIfClause> IfClauses { get; }
        IStatement Else { get; }
    }

    public interface IIfClause
    {
        IExpression Condition { get; }
        IStatement Body { get; }
    }

    public interface ILoop : IStatement
    {
        LoopKind LoopClass { get; }
        IStatement Body { get; }
    }

    public interface IForWhileUntil : ILoop
    {
        IExpression Condition { get; }
    }

    public interface IWhileUntil : IForWhileUntil
    {
        bool IsTopTest { get; }
        bool IsWhile { get; }
    }

    public interface IFor : IForWhileUntil
    {
        ImmutableArray<IStatement> Before { get; }
        ImmutableArray<IStatement> AtLoopBottom { get; }
        ImmutableArray<ILocalSymbol> Locals { get; }
    }

    public interface IForEach : ILoop
    {
        ILocalSymbol IterationVariable { get; }
        IExpression Collection { get; }
    }

    public enum LoopKind
    {
        WhileUntil,
        For,
        ForEach
    }

    public interface ILabel : IStatement
    {
        ILabelSymbol Label { get; }
    }

    public interface ILabeled: ILabel
    {
        IStatement Target { get; }
    }

    public interface IBranch : IStatement
    {
        ILabelSymbol Target { get; }
    }

    public interface IThrow : IStatement
    {
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
