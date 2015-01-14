// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    /// <summary>
    /// Operator precedence classes from section 7.3.1 of the C# language specification.
    /// </summary>
    internal enum OperatorPrecedence
    {
        None = 0,
        AssignmentAndLambdaExpression,
        Conditional,
        NullCoalescing,
        ConditionalOr,
        ConditionalAnd,
        LogicalOr,
        LogicalXor,
        LogicalAnd,
        Equality,
        RelationalAndTypeTesting,
        Shift,
        Additive,
        Multiplicative,
        Unary,
        Primary
    }
}
