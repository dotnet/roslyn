// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.InvertIf
{
    internal abstract partial class AbstractInvertIfCodeRefactoringProvider<TIfStatementSyntax, TStatementSyntax, TEmbeddedStatement>
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
            public SyntaxNode Parent => FirstStatement.Parent;
        }
    }
}
