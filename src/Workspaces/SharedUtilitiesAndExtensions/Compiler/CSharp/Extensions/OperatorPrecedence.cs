// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

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
    Switch,
    Range,
    Unary,
    Primary
}
