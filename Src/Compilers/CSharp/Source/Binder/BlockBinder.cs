// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class BlockBinder : LocalScopeBinder
    {
        private readonly SyntaxList<StatementSyntax> statements;

        public BlockBinder(MethodSymbol owner, Binder enclosing, SyntaxList<StatementSyntax> statements)
            : this(owner, enclosing, statements, enclosing.Flags)
        {
        }

        public BlockBinder(MethodSymbol owner, Binder enclosing, SyntaxList<StatementSyntax> statements, BinderFlags additionalFlags)
            : base(owner, enclosing, enclosing.Flags | additionalFlags)
        {
            this.statements = statements;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return BuildLocals(this.statements);
        }

        protected override ImmutableArray<LabelSymbol> BuildLabels()
        {
            ArrayBuilder<LabelSymbol> labels = null;
            base.BuildLabels(this.statements, ref labels);
            return (labels != null) ? labels.ToImmutableAndFree() : ImmutableArray<LabelSymbol>.Empty;
        }
    }
}
