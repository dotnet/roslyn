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
            var builder = ArrayBuilder<BoundStatement>.GetInstance();
            VisitStatementSubList(builder, node.Statements);

            if (!this.Instrument || (node != _rootStatement && (node.WasCompilerGenerated || node.Syntax.Kind() != SyntaxKind.Block)))
            {
                return node.Update(node.Locals, node.LocalFunctions, builder.ToImmutableAndFree());
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


        /// <summary>
        /// Visit a partial list of statements that possibly contain using declarations
        /// </summary>
        /// <param name="builder">The array builder to append statements to</param>
        /// <param name="statements">The list of statements to visit</param>
        /// <param name="startIndex">The index of the <paramref name="statements"/> to begin visiting at</param>
        /// <returns>An <see cref="ImmutableArray{T}"/> of <see cref="BoundStatement"/></returns>
        public void VisitStatementSubList(ArrayBuilder<BoundStatement> builder, ImmutableArray<BoundStatement> statements, int startIndex = 0)
        {
            for (int i = startIndex; i < statements.Length; i++)
            {
                BoundStatement statement = VisitPossibleUsingDeclaration(statements[i], statements, i, out var replacedUsingDeclarations);
                if (statement != null)
                {
                    builder.Add(statement);
                }

                if (replacedUsingDeclarations)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Visits a node that is possibly a <see cref="BoundUsingLocalDeclarations"/>
        /// </summary>
        /// <param name="node">The node to visit</param>
        /// <param name="statements">All statements in the block containing this node</param>
        /// <param name="statementIndex">The current statement being visited in <paramref name="statements"/></param>
        /// <param name="replacedLocalDeclarations">Set to true if this visited a <see cref="BoundUsingLocalDeclarations"/> node</param>
        /// <returns>A <see cref="BoundStatement"/></returns>
        /// <remarks>
        /// The node being visited is not necessarily equal to statements[startIndex]. 
        /// When traversing down a set of labels, we set node to the label.body and recurse, but statements[startIndex] still refers to the original parent label 
        /// as we haven't actually moved down the original statement list
        /// </remarks>
        public BoundStatement VisitPossibleUsingDeclaration(BoundStatement node, ImmutableArray<BoundStatement> statements, int statementIndex, out bool replacedLocalDeclarations)
        {
            switch (node.Kind)
            {
                case BoundKind.LabeledStatement:
                    var labelStatement = (BoundLabeledStatement)node;
                    return MakeLabeledStatement(labelStatement, VisitPossibleUsingDeclaration(labelStatement.Body, statements, statementIndex, out replacedLocalDeclarations));
                case BoundKind.UsingLocalDeclarations:
                    // visit everything after this node 
                    ArrayBuilder<BoundStatement> builder = ArrayBuilder<BoundStatement>.GetInstance();
                    VisitStatementSubList(builder, statements, statementIndex + 1);
                    // make a using declaration with the visited statements as its body
                    replacedLocalDeclarations = true;
                    return MakeLocalUsingDeclarationStatement((BoundUsingLocalDeclarations)node, builder.ToImmutableAndFree());
                default:
                    replacedLocalDeclarations = false;
                    return (BoundStatement)Visit(node);
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
