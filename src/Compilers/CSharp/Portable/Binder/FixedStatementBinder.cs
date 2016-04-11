// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class FixedStatementBinder : LocalScopeBinder
    {
        private readonly FixedStatementSyntax _syntax;

        public FixedStatementBinder(Binder enclosing, FixedStatementSyntax syntax)
            : base(enclosing)
        {
            Debug.Assert(syntax != null);
            _syntax = syntax;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            if (_syntax.Declaration != null)
            {
                var locals = new ArrayBuilder<LocalSymbol>(_syntax.Declaration.Variables.Count);
                foreach (VariableDeclaratorSyntax declarator in _syntax.Declaration.Variables)
                {
                    locals.Add(MakeLocal(RefKind.None, _syntax.Declaration, declarator, LocalDeclarationKind.FixedVariable));

                    if (declarator.Initializer != null)
                    {
                        BuildAndAddPatternVariables(locals, declarator.Initializer.Value);
                    }
                }

                return locals.ToImmutable();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (_syntax == scopeDesignator)
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
