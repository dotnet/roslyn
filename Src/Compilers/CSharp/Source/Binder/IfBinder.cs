// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class IfBinder : LocalScopeBinder
    {
        private readonly IfStatementSyntax ifStatement;

        public IfBinder(Binder enclosing, IfStatementSyntax ifStatement)
            : base(enclosing, enclosing.Flags)
        {
            this.ifStatement = ifStatement;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return BuildLocals(ifStatement);
        }

        internal override BoundIfStatement BindIfParts(DiagnosticBag diagnostics)
        {
            {
                var condition = BindBooleanExpression(ifStatement.Condition, diagnostics);
                var consequence = BindPossibleEmbeddedStatement(ifStatement.Statement, diagnostics);
                if (ifStatement.Else == null)
                {
                    return new BoundIfStatement(ifStatement, this.Locals, condition, consequence, null);
                }

                var alternative = BindPossibleEmbeddedStatement(ifStatement.Else.Statement, diagnostics);
                return new BoundIfStatement(ifStatement, this.Locals, condition, consequence, alternative);
            }
        }
    }
}
