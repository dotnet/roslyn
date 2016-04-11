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
        public readonly CSharpSyntaxNode ScopeDesignator;

        internal PatternVariableBinder(CSharpSyntaxNode scopeDesignator, ImmutableArray<ExpressionSyntax> expressions, Binder next) : base(next)
        {
            this.ScopeDesignator = scopeDesignator;
            this._nodes = StaticCast<CSharpSyntaxNode>.From(expressions);
        }

        internal PatternVariableBinder(CSharpSyntaxNode scopeDesignator, IEnumerable<VariableDeclaratorSyntax> declarations, Binder next) : base(next)
        {
            this.ScopeDesignator = scopeDesignator;
            var nodes = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
            foreach (var decl in declarations)
            {
                var value = decl.Initializer?.Value;
                if (value != null) nodes.Add(value);
            }
            this._nodes = nodes.ToImmutableAndFree();
        }

        internal PatternVariableBinder(CSharpSyntaxNode scopeDesignator, IEnumerable<ArgumentSyntax> arguments, Binder next) : base(next)
        {
            this.ScopeDesignator = scopeDesignator;
            var nodes = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
            foreach (var arg in arguments)
            {
                var value = arg.Expression;
                if (value != null) nodes.Add(value);
            }
            this._nodes = nodes.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SwitchSectionSyntax scopeDesignator, Binder next) : base(next)
        {
            this.ScopeDesignator = scopeDesignator;
            var nodes = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
            foreach (var label in scopeDesignator.Labels)
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

        internal PatternVariableBinder(MatchSectionSyntax scopeDesignator, Binder next) : base(next)
        {
            this.ScopeDesignator = scopeDesignator;
            this._nodes = scopeDesignator.WhenClause != null
                ? ImmutableArray.Create<CSharpSyntaxNode>(scopeDesignator.Pattern, scopeDesignator.WhenClause.Condition, scopeDesignator.Expression)
                : ImmutableArray.Create<CSharpSyntaxNode>(scopeDesignator.Pattern, scopeDesignator.Expression)
                ;
        }

        internal PatternVariableBinder(CSharpSyntaxNode scopeDesignator, ExpressionSyntax expression, Binder next) : base(next)
        {
            this._node = expression;
            this.ScopeDesignator = scopeDesignator;
        }

        internal PatternVariableBinder(AttributeSyntax scopeDesignator, Binder next) : base(next)
        {
            this.ScopeDesignator = scopeDesignator;

            if (scopeDesignator.ArgumentList?.Arguments.Count > 0)
            {
                var expressions = ArrayBuilder<CSharpSyntaxNode>.GetInstance(scopeDesignator.ArgumentList.Arguments.Count);

                foreach (var argument in scopeDesignator.ArgumentList.Arguments)
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

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (ScopeDesignator == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
