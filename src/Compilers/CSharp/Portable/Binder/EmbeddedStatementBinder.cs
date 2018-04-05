// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder owns the scope for an embedded statement.
    /// </summary>
    internal sealed class EmbeddedStatementBinder : LocalScopeBinder
    {
        private readonly SyntaxList<StatementSyntax> _statements;

        public EmbeddedStatementBinder(Binder enclosing, StatementSyntax statement)
            : base(enclosing, enclosing.Flags)
        {
            Debug.Assert(statement != null);
            _statements = new SyntaxList<StatementSyntax>(statement);
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return BuildLocals(_statements, this);
        }

        protected override ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions()
        {
            return BuildLocalFunctions(_statements);
        }

        internal override bool IsLocalFunctionsScopeBinder
        {
            get
            {
                return true;
            }
        }

        protected override ImmutableArray<LabelSymbol> BuildLabels()
        {
            ArrayBuilder<LabelSymbol> labels = null;
            base.BuildLabels(_statements, ref labels);
            return (labels != null) ? labels.ToImmutableAndFree() : ImmutableArray<LabelSymbol>.Empty;
        }

        internal override bool IsLabelsScopeBinder
        {
            get
            {
                return true;
            }
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (ScopeDesignator == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _statements.First();
            }
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (ScopeDesignator == scopeDesignator)
            {
                return this.LocalFunctions;
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
