// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Precedence;

namespace Microsoft.CodeAnalysis.CSharp.Precedence;

internal abstract class AbstractCSharpPrecedenceService<TSyntax> : AbstractPrecedenceService<TSyntax, OperatorPrecedence>
    where TSyntax : SyntaxNode
{
    public sealed override PrecedenceKind GetPrecedenceKind(OperatorPrecedence precedence)
        => precedence switch
        {
            OperatorPrecedence.NullCoalescing => PrecedenceKind.Coalesce,
            OperatorPrecedence.ConditionalOr or OperatorPrecedence.ConditionalAnd => PrecedenceKind.Logical,
            OperatorPrecedence.LogicalOr or OperatorPrecedence.LogicalXor or OperatorPrecedence.LogicalAnd => PrecedenceKind.Bitwise,
            OperatorPrecedence.Equality => PrecedenceKind.Equality,
            OperatorPrecedence.RelationalAndTypeTesting => PrecedenceKind.Relational,
            OperatorPrecedence.Shift => PrecedenceKind.Shift,
            OperatorPrecedence.Additive or OperatorPrecedence.Multiplicative => PrecedenceKind.Arithmetic,
            _ => PrecedenceKind.Other,
        };
}
