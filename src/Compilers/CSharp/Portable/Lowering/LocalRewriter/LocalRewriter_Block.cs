// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
                return node.Update(node.Locals, node.LocalFunctions, VisitStatementSubList(node.Statements));
            }

            var builder = ArrayBuilder<BoundStatement>.GetInstance();
            builder.AddRange(VisitStatementSubList(node.Statements));

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


        /// <summary>
        /// Visit a partial list of statements that possibly contain using declarations
        /// </summary>
        /// <param name="statements">The list of statements to visit</param>
        /// <param name="startIndex">The index of the <paramref name="statements"/> to begin visiting at</param>
        /// <returns>An <see cref="ImmutableArray{T}"/> of <see cref="BoundStatement"/></returns>
        public ImmutableArray<BoundStatement> VisitStatementSubList(ImmutableArray<BoundStatement> statements, int startIndex = 0)
        {
            var builder = ArrayBuilder<BoundStatement>.GetInstance();
            for (int i = startIndex; i < statements.Length; i++)
            {
                BoundStatement statement = (BoundStatement)VisitPossibleUsingDeclaration(statements[i], statements, i, out var replacedUsingDeclarations);
                if (statement != null)
                {
                    builder.Add(statement);
                }

                if (replacedUsingDeclarations)
                {
                    break;
                }
            }
            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Visits a node that is possibly a <see cref="BoundUsingLocalDeclarations"/>
        /// </summary>
        /// <param name="node">The node to visit</param>
        /// <param name="statements">A list of statements that follow this node</param>
        /// <param name="startIndex">The startIndex of statements</param>
        /// <param name="replacedLocalDeclarations">Set to true if this visited a <see cref="BoundUsingLocalDeclarations"/> node</param>
        /// <returns></returns>
        public BoundNode VisitPossibleUsingDeclaration(BoundStatement node, ImmutableArray<BoundStatement> statements, int startIndex, out bool replacedLocalDeclarations)
        {
            switch (node.Kind)
            {
                case BoundKind.LabeledStatement:
                    var labelStatement = (BoundLabeledStatement)node;
                    return MakeLabeledStatement(labelStatement, (BoundStatement)VisitPossibleUsingDeclaration(labelStatement.Body, statements, startIndex, out replacedLocalDeclarations));
                case BoundKind.UsingLocalDeclarations:
                    var usingDeclarations = (BoundUsingLocalDeclarations)node;
                    replacedLocalDeclarations = true;
                    return MakeLocalUsingDeclarationStatement(usingDeclarations, VisitStatementSubList(statements, startIndex + 1));
                default:
                    replacedLocalDeclarations = false;
                    return Visit(node);
            }
        }


        public override BoundNode VisitNoOpStatement(BoundNoOpStatement node)
        {
            return (node.WasCompilerGenerated || !this.Instrument)
                ? new BoundBlock(node.Syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray<BoundStatement>.Empty)
                : _instrumenter.InstrumentNoOpStatement(node, node);
        }
    }
}
