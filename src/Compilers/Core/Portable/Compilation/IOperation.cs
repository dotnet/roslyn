using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Semantics
{
    public interface IOperation
    {
        OperationKind Kind { get; }
        SyntaxNode Syntax { get; }
    }

    public enum OperationKind
    {
        None,

        BlockStatement,
        VariableDeclarationStatement,
        SwitchStatement,
        IfStatement,
        LoopStatement,
        ContinueStatement,
        BreakStatement,
        YieldBreakStatement,
        LabelStatement,
        LabeledStatement,            // Why do both of these exist?
        GoToStatement,
        EmptyStatement,
        ThrowStatement,
        ReturnStatement,
        LockStatement,
        TryStatement,
        CatchHandler,
        UsingWithDeclarationStatement,
        UsingWithExpressionStatement,
        YieldReturnStatement,
        FixedStatement,

        ExpressionStatement,

        Literal,
        Conversion,
        Invocation,
        ArrayElementReference,
        PointerIndirectionReference,
        LocalReference,
        ParameterReference,
        TemporaryReference,
        FieldReference,
        MethodReference,
        PropertyReference,
        LateBoundMemberReference,
        UnaryOperator,
        BinaryOperator,
        ConditionalChoice,
        NullCoalescing,
        RelationalOperator,
        Lambda,
        ObjectCreation,
        TypeParameterObjectCreation,
        ArrayCreation,
        DefaultValue,
        Instance,
        BaseClassInstance,
        ClassInstance,
        ImplicitInstance,
        Is,
        TypeOperation,
        Await,
        AddressOf,
        Assignment,
        CompoundAssignment,
        Parenthesized,

        UnboundLambda,

        // VB only

        Omitted,
        StopStatement,
        EndStatement,
        WithStatement,

        // Newly added

        ConditionalAccess
    }
}
