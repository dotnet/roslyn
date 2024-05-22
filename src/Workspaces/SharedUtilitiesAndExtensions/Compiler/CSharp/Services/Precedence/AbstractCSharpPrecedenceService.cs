// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Precedence;

namespace Microsoft.CodeAnalysis.CSharp.Precedence;

internal abstract class AbstractCSharpPrecedenceService<TSyntax> : AbstractPrecedenceService<TSyntax, OperatorPrecedence>
    where TSyntax : SyntaxNode
{
    protected AbstractCSharpPrecedenceService()
    {
    }

    public sealed override PrecedenceKind GetPrecedenceKind(OperatorPrecedence precedence)
    {
        switch (precedence)
        {
            case OperatorPrecedence.NullCoalescing: return PrecedenceKind.Coalesce;
            case OperatorPrecedence.ConditionalOr:
            case OperatorPrecedence.ConditionalAnd: return PrecedenceKind.Logical;
            case OperatorPrecedence.LogicalOr:
            case OperatorPrecedence.LogicalXor:
            case OperatorPrecedence.LogicalAnd: return PrecedenceKind.Bitwise;
            case OperatorPrecedence.Equality: return PrecedenceKind.Equality;
            case OperatorPrecedence.RelationalAndTypeTesting: return PrecedenceKind.Relational;
            case OperatorPrecedence.Shift: return PrecedenceKind.Shift;
            case OperatorPrecedence.Additive:
            case OperatorPrecedence.Multiplicative: return PrecedenceKind.Arithmetic;
            default: return PrecedenceKind.Other;
        }
    }
}
