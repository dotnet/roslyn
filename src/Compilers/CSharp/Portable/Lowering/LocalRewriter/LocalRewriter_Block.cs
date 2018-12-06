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
                return node.Update(node.Locals, node.LocalFunctions, VisitList(node.Statements));
            }

            var builder = ArrayBuilder<BoundStatement>.GetInstance();
            var usingDeclarationIndicies = ArrayBuilder<int>.GetInstance();
            for (int i = 0; i < node.Statements.Length; i++)
            {
                BoundStatement statement = node.Statements[i];
                if (statement.ContainsUsingDeclarationStatement())
                {
                    usingDeclarationIndicies.Add(i);
                }
                else
                {
                    statement = (BoundStatement)Visit(statement);
                }
                if (statement != null) builder.Add(statement);
            }

            // Lower any pendng using declarations we didn't visit during the initial loop
            LowerPendingUsingDeclarations(builder, usingDeclarationIndicies.ToImmutableAndFree());

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

        private void LowerPendingUsingDeclarations(ArrayBuilder<BoundStatement> statements, ImmutableArray<int> unloweredDeclarationIndicies)
        {
            // work backwards and lower the using declarations
            for (int i = unloweredDeclarationIndicies.Length - 1; i >= 0; i--)
            {
                var unlowered = statements[unloweredDeclarationIndicies[i]];

                // we may be wrapped in a label, remember it if so
                BoundLabeledStatement containingLabel = null;
                if (unlowered is BoundLabeledStatement label)
                {
                    unlowered = label.Body;
                    containingLabel = label;
                }

                var localUsing = (BoundUsingLocalDeclarations)unlowered;

                var statementEndIndex = (i == unloweredDeclarationIndicies.Length - 1) ? statements.Count - 1 : unloweredDeclarationIndicies[i + 1];
                var statementStartIndex = unloweredDeclarationIndicies[i] + 1;

                // get the lowered statements that form the body of this using declaration
                var usingBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                for (int j = statementStartIndex; j <= statementEndIndex; j++)
                {
                    usingBuilder.Add(statements[j]);
                }

                var usingNode = MakeLocalUsingDeclarationStatement(localUsing, usingBuilder.ToImmutableAndFree());

                if (containingLabel != null)
                {
                    // If we were wrapped in a label, make sure we lower that too, with our new lowered node as the body
                    usingNode = (BoundStatement)MakeLabeledStatement(containingLabel, usingNode);
                }

                // replace the unlowered node (and all others after it) with our new lowered node
                statements[unloweredDeclarationIndicies[i]] = usingNode;
                statements.Clip(unloweredDeclarationIndicies[i] + 1);
            }
        }

    }
}
