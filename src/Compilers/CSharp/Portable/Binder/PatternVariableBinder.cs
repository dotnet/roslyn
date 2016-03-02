// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class PatternVariableBinder : LocalScopeBinder
    {
        private readonly ExpressionSyntax Expression;
        private readonly ImmutableArray<ExpressionSyntax> Expressions;
        private readonly ImmutableArray<PatternSyntax> Patterns;
        public readonly SyntaxNode Syntax;

        internal PatternVariableBinder(SyntaxNode syntax, ImmutableArray<ExpressionSyntax> expressions, Binder next) : base(next)
        {
            this.Syntax = syntax;
            this.Expressions = expressions;
        }

        internal PatternVariableBinder(SyntaxNode syntax, IEnumerable<VariableDeclaratorSyntax> declarations, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance();
            foreach (var decl in declarations)
            {
                var value = decl.Initializer?.Value;
                if (value != null) expressions.Add(value);
            }
            this.Expressions = expressions.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SyntaxNode syntax, IEnumerable<ArgumentSyntax> arguments, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance();
            foreach (var arg in arguments)
            {
                var value = arg.Expression;
                if (value != null) expressions.Add(value);
            }
            this.Expressions = expressions.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SwitchSectionSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance();
            var patterns = ArrayBuilder<PatternSyntax>.GetInstance();
            foreach (var label in syntax.Labels)
            {
                var match = label as CasePatternSwitchLabelSyntax;
                if (match != null)
                {
                    patterns.Add(match.Pattern);
                    if (match.WhenClause != null)
                    {
                        expressions.Add(match.WhenClause.Condition);
                    }
                }
            }

            this.Expressions = expressions.ToImmutableAndFree();
            this.Patterns = patterns.ToImmutableAndFree();
        }

        internal PatternVariableBinder(MatchSectionSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;
            this.Patterns = ImmutableArray.Create<PatternSyntax>(syntax.Pattern);
            this.Expressions = syntax.WhenClause != null
                ? ImmutableArray.Create<ExpressionSyntax>(syntax.Expression, syntax.WhenClause.Condition)
                : ImmutableArray.Create<ExpressionSyntax>(syntax.Expression)
                ;
        }

        internal PatternVariableBinder(ForStatementSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance();
            if (syntax.Declaration != null) foreach(var decl in syntax.Declaration.Variables)
            {
                var value = decl.Initializer?.Value;
                if (value != null) expressions.Add(value);
            }

            if (syntax.Initializers != null) expressions.AddRange(syntax.Initializers);
            if (syntax.Condition != null) expressions.Add(syntax.Condition);
            if (syntax.Incrementors != null) expressions.AddRange(syntax.Incrementors);
            this.Expressions = expressions.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SyntaxNode syntax, ExpressionSyntax expression, Binder next) : base(next)
        {
            this.Expression = expression;
            this.Syntax = syntax;
        }

        internal PatternVariableBinder(AttributeSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;

            if (syntax.ArgumentList?.Arguments.Count > 0)
            {
                var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance(syntax.ArgumentList.Arguments.Count);

                foreach (var argument in syntax.ArgumentList.Arguments)
                {
                    expressions.Add(argument.Expression);
                }

                this.Expressions = expressions.ToImmutableAndFree();
            }
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var patterns = PatternVariableFinder.FindPatternVariables(Expression, Expressions, this.Patterns);
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();
            foreach (var pattern in patterns)
            {
                builder.Add(SourceLocalSymbol.MakeLocal(Next.ContainingMember(), this, RefKind.None, pattern.Type, pattern.Identifier, LocalDeclarationKind.PatternVariable));
            }
            patterns.Free();
            return builder.ToImmutableAndFree();
        }
    }
}
