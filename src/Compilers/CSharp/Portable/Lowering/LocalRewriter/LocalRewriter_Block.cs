// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitBlock(BoundBlock node)
        {
            if (!this.Instrument || (node != _rootStatement && (node.WasCompilerGenerated || node.Syntax.Kind() != SyntaxKind.Block)))
            {
                return node.Update(node.Locals, node.LocalFunctions, VisitList(node.Statements));
            }

            var builder = ArrayBuilder<BoundStatement>.GetInstance();

            for (int i = 0; i < node.Statements.Length; i++)
            {
                var stmt = (BoundStatement)Visit(node.Statements[i]);
                if (stmt != null) builder.Add(stmt);
            }

            LocalSymbol synthesizedLocal;
            BoundStatement prologue = _instrumenter.CreateBlockPrologue(node, out synthesizedLocal);
            if (prologue != null)
            {
                builder.Insert(0, prologue);
            }

            BoundStatement epilogue = _instrumenter.CreateBlockEpilogue(node);
            if (epilogue != null)
            {
                builder.Add(epilogue);
            }

            return new BoundBlock(node.Syntax, synthesizedLocal == null ? node.Locals : node.Locals.Add(synthesizedLocal), node.LocalFunctions, builder.ToImmutableAndFree(), node.HasErrors);
        }

        public override BoundNode VisitNoOpStatement(BoundNoOpStatement node)
        {
            return (node.WasCompilerGenerated || !this.Instrument)
                ? new BoundBlock(node.Syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray<BoundStatement>.Empty)
                : _instrumenter.InstrumentNoOpStatement(node, node);
        }
    }
}
