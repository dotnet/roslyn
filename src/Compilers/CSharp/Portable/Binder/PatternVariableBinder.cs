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
        private readonly CSharpSyntaxNode _node;
        private readonly ImmutableArray<CSharpSyntaxNode> _nodes;
        public readonly SyntaxNode Syntax;

        internal PatternVariableBinder(SyntaxNode syntax, ImmutableArray<ExpressionSyntax> expressions, Binder next) : base(next)
        {
            this.Syntax = syntax;
            this._nodes = StaticCast<CSharpSyntaxNode>.From(expressions);
        }

        internal PatternVariableBinder(SyntaxNode syntax, IEnumerable<VariableDeclaratorSyntax> declarations, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var nodes = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
            foreach (var decl in declarations)
            {
                var value = decl.Initializer?.Value;
                if (value != null) nodes.Add(value);
            }
            this._nodes = nodes.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SyntaxNode syntax, IEnumerable<ArgumentSyntax> arguments, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var nodes = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
            foreach (var arg in arguments)
            {
                var value = arg.Expression;
                if (value != null) nodes.Add(value);
            }
            this._nodes = nodes.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SwitchSectionSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var nodes = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
            foreach (var label in syntax.Labels)
            {
                var match = label as CasePatternSwitchLabelSyntax;
                if (match != null)
                {
                    nodes.Add(match.Pattern);
                    if (match.WhenClause != null)
                    {
                        nodes.Add(match.WhenClause.Condition);
                    }
                }
            }

            this._nodes = nodes.ToImmutableAndFree();
        }

        internal PatternVariableBinder(MatchSectionSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;
            this._nodes = syntax.WhenClause != null
                ? ImmutableArray.Create<CSharpSyntaxNode>(syntax.Pattern, syntax.WhenClause.Condition, syntax.Expression)
                : ImmutableArray.Create<CSharpSyntaxNode>(syntax.Pattern, syntax.Expression)
                ;
        }

        internal PatternVariableBinder(ForStatementSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var expressions = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
            if (syntax.Declaration != null) foreach(var decl in syntax.Declaration.Variables)
            {
                var value = decl.Initializer?.Value;
                if (value != null) expressions.Add(value);
            }

            if (syntax.Initializers != null) expressions.AddRange(syntax.Initializers);
            if (syntax.Condition != null) expressions.Add(syntax.Condition);
            if (syntax.Incrementors != null) expressions.AddRange(syntax.Incrementors);
            this._nodes = expressions.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SyntaxNode syntax, ExpressionSyntax expression, Binder next) : base(next)
        {
            this._node = expression;
            this.Syntax = syntax;
        }

        internal PatternVariableBinder(AttributeSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;

            if (syntax.ArgumentList?.Arguments.Count > 0)
            {
                var expressions = ArrayBuilder<CSharpSyntaxNode>.GetInstance(syntax.ArgumentList.Arguments.Count);

                foreach (var argument in syntax.ArgumentList.Arguments)
                {
                    expressions.Add(argument.Expression);
                }

                this._nodes = expressions.ToImmutableAndFree();
            }
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();
            BuildAndAddPatternVariables(builder, _node, _nodes);
            return builder.ToImmutableAndFree();
        }
    }
}
