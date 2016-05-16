// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class BlockBinder : LocalScopeBinder
    {
        private readonly SyntaxList<StatementSyntax> _statements;

        public BlockBinder(Binder enclosing, SyntaxList<StatementSyntax> statements)
            : this(enclosing, statements, enclosing.Flags)
        {
        }

        public BlockBinder(Binder enclosing, SyntaxList<StatementSyntax> statements, BinderFlags additionalFlags)
            : base(enclosing, enclosing.Flags | additionalFlags)
        {
            _statements = statements;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return BuildLocals(_statements);
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

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (IsMatchingScopeDesignator(scopeDesignator))
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }

        private bool IsMatchingScopeDesignator(CSharpSyntaxNode scopeDesignator)
        {
            if (scopeDesignator.Kind() == SyntaxKind.Block)
            {
                if (((BlockSyntax)scopeDesignator).Statements == _statements)
                {
                    return true;
                }
            }
            else if (_statements.Count == 1 && _statements.First() == scopeDesignator)
            {
                // This code compensates for the fact that we fake an enclosing block
                // when there is an (illegal) local declaration as a controlled statement.
                return true;
            }

            return false;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (IsMatchingScopeDesignator(scopeDesignator))
            {
                return this.LocalFunctions;
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
