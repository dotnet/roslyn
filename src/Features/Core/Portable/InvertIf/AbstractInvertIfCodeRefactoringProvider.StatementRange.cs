// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InvertIf;

internal abstract partial class AbstractInvertIfCodeRefactoringProvider<
    TSyntaxKind, TStatementSyntax, TIfStatementSyntax, TEmbeddedStatement>
{
    protected readonly struct StatementRange
    {
        public readonly TStatementSyntax FirstStatement;
        public readonly TStatementSyntax LastStatement;

        public StatementRange(TStatementSyntax firstStatement, TStatementSyntax lastStatement)
        {
            Debug.Assert(firstStatement != null);
            Debug.Assert(lastStatement != null);
            Debug.Assert(firstStatement.Parent != null);
            Debug.Assert(firstStatement.Parent == lastStatement.Parent);
            Debug.Assert(firstStatement.SpanStart <= lastStatement.SpanStart);
            FirstStatement = firstStatement;
            LastStatement = lastStatement;
        }

        public bool IsEmpty => FirstStatement == null;
        public SyntaxNode Parent => FirstStatement.GetRequiredParent();
    }
}
