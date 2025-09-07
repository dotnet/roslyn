// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitBlock(BoundBlock node)
        {
            if (Instrument)
            {
                Instrumenter.PreInstrumentBlock(node, this);
            }

            var builder = ArrayBuilder<BoundStatement>.GetInstance();
            // If _additionalLocals is null, this must be the outermost block of the current function.
            // If so, create a collection where child statements can insert inline array temporaries,
            // and add those temporaries to the generated block.
            var previousLocals = _additionalLocals;
            if (previousLocals is null)
            {
                _additionalLocals = ArrayBuilder<LocalSymbol>.GetInstance();
            }

            try
            {
                VisitStatementSubList(builder, node.Statements);

                var additionalLocals = TemporaryArray<LocalSymbol>.Empty;

                BoundBlockInstrumentation? instrumentation = null;
                if (Instrument)
                {
                    Instrumenter.InstrumentBlock(node, this, ref additionalLocals, out var prologue, out var epilogue, out instrumentation);
                    if (prologue != null)
                    {
                        builder.Insert(0, prologue);
                    }

                    if (epilogue != null)
                    {
                        builder.Add(epilogue);
                    }
                }

                var locals = node.Locals;
                if (previousLocals is null)
                {
                    locals = locals.AddRange(_additionalLocals!);
                }
                locals = locals.AddRange(additionalLocals);
                return new BoundBlock(node.Syntax, locals, node.LocalFunctions, node.HasUnsafeModifier, instrumentation, builder.ToImmutableAndFree(), node.HasErrors);
            }
            finally
            {
                if (previousLocals is null)
                {
                    _additionalLocals!.Free();
                    _additionalLocals = previousLocals;
                }
            }
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
                BoundStatement? statement = VisitPossibleUsingDeclaration(statements[i], statements, i, out var replacedUsingDeclarations);
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
        public BoundStatement? VisitPossibleUsingDeclaration(BoundStatement node, ImmutableArray<BoundStatement> statements, int statementIndex, out bool replacedLocalDeclarations)
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
                    return VisitStatement(node);
            }
        }

        public override BoundNode VisitNoOpStatement(BoundNoOpStatement node)
        {
            return (node.WasCompilerGenerated || !this.Instrument)
                ? new BoundBlock(node.Syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray<BoundStatement>.Empty)
                : Instrumenter.InstrumentNoOpStatement(node, node);
        }
    }
}
