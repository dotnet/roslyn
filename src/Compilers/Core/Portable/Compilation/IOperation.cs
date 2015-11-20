﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements and expressions.
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// Identifies the kind of the operation.
        /// </summary>
        OperationKind Kind { get; }
        /// <summary>
        /// Syntax that was analyzed to produce the operation.
        /// </summary>
        SyntaxNode Syntax { get; }
    }

    /// <summary>
    /// All of the kinds of operations, including statements and expressions.
    /// </summary>
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
        LocalFunctionStatement,

        ExpressionStatement,

        LiteralExpression,
        ConversionExpression,
        InvocationExpression,
        ArrayElementReferenceExpression,
        PointerIndirectionReferenceExpression,
        LocalReferenceExpression,
        ParameterReferenceExpression,
        SyntheticLocalReferenceExpression,
        FieldReferenceExpression,
        MethodReferenceExpression,
        PropertyReferenceExpression,
        LateBoundMemberReferenceExpression,
        UnaryOperatorExpression,
        BinaryOperatorExpression,
        ConditionalChoiceExpression,
        NullCoalescingExpression,
        LambdaExpression,
        ObjectCreationExpression,
        TypeParameterObjectCreationExpression,
        ArrayCreationExpression,
        DefaultValueExpression,
        InstanceReferenceExpression,
        BaseClassInstanceReferenceExpression,
        ClassInstanceReferenceExpression,
        IsExpression,
        TypeOperationExpression,
        AwaitExpression,
        AddressOfExpression,
        AssignmentExpression,
        CompoundAssignmentExpression,
        ParenthesizedExpression,

        UnboundLambdaExpression,

        // VB only

        OmittedArgumentExpression,
        StopStatement,
        EndStatement,
        WithStatement,

        // Newly added

        ConditionalAccessExpression,
        IncrementExpression
    }
}
