// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitBlock(BoundBlock node)
        {
            if (node.WasCompilerGenerated || !this.Instrument || node.Syntax.Kind() != SyntaxKind.Block)
            {
                return node.Update(node.Locals, node.LocalFunctions, VisitList(node.Statements));
            }

            BlockSyntax syntax = (BlockSyntax)node.Syntax;

            var builder = ArrayBuilder<BoundStatement>.GetInstance();

            BoundStatement prologue = _instrumenter.CreateBlockPrologue(node);
            if (prologue != null)
            {
                builder.Add(prologue);
            }

            for (int i = 0; i < node.Statements.Length; i++)
            {
                var stmt = (BoundStatement)Visit(node.Statements[i]);
                if (stmt != null) builder.Add(stmt);
            }

            BoundStatement epilogue = _instrumenter.CreateBlockEpilogue(node);
            if (epilogue != null)
            {
                builder.Add(epilogue);
            }

            return new BoundBlock(node.Syntax, node.Locals, node.LocalFunctions, builder.ToImmutableAndFree(), node.HasErrors);
        }

        public override BoundNode VisitNoOpStatement(BoundNoOpStatement node)
        {
            return (node.WasCompilerGenerated || !this.Instrument)
                ? new BoundBlock(node.Syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray<LocalFunctionSymbol>.Empty, ImmutableArray<BoundStatement>.Empty)
                : _instrumenter.InstrumentNoOpStatement(node, node);
        }
    }
}
