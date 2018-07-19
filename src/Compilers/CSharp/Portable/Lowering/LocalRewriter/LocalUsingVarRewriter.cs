// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Lowering.LocalRewriter
{
    internal class LocalUsingVarRewriter : BoundTreeRewriterWithStackGuard
    {
        public static BoundNode Rewrite(BoundStatement statement)
        {
            var localUsingVarRewriter = new LocalUsingVarRewriter();
            return localUsingVarRewriter.Visit(statement);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            ImmutableArray<BoundStatement> statements = (ImmutableArray<BoundStatement>)this.VisitList(node.Statements);

            for (int i = 0; i < statements.Length; i++)
            {
                if (statements[i] is BoundLocalDeclaration localDeclaration)
                {
                    if (localDeclaration.LocalSymbol.IsUsing)
                    {
                        List<BoundStatement> precedingStatements = new List<BoundStatement>();
                        for (int j = 0; j < i; j++)
                        {
                            precedingStatements.Add(statements[j]);
                        }
                        var followingStatements = ImmutableArray.Create(
                            statements, i + 1,
                            statements.Length - i - 1);

                        var localDeclarations = ImmutableArray.Create(localDeclaration);

                        BoundBlock innerBlock = new BoundBlock(
                            syntax: localDeclaration.Syntax,
                            locals: ImmutableArray.Create<LocalSymbol>(),
                            statements: followingStatements
                            );

                        BoundUsingStatement boundUsing = new BoundUsingStatement(
                            syntax: localDeclaration.Syntax,
                            locals: ImmutableArray.Create<LocalSymbol>(),
                            declarationsOpt: new BoundMultipleLocalDeclarations(
                                localDeclaration.Syntax,
                                localDeclarations),
                            expressionOpt: null,
                            iDisposableConversion: Conversion.Identity,
                            disposeMethodOpt: null,
                            body: innerBlock
                            );
                        precedingStatements.Add(boundUsing);

                        BoundBlock outermostBlock = new BoundBlock(
                            syntax: localDeclaration.Syntax,
                            locals: node.Locals,
                            statements: precedingStatements.ToImmutableArray<BoundStatement>());

                        return outermostBlock;

                    }
                }
                else if (statements[i] is BoundMultipleLocalDeclarations boundMultiple)
                {
                    if (!boundMultiple.LocalDeclarations.IsDefaultOrEmpty)
                    {
                        if (boundMultiple.LocalDeclarations[0].LocalSymbol.IsUsing)
                        {
                            ArrayBuilder<BoundStatement> precedingStatements = ArrayBuilder<BoundStatement>.GetInstance(i);
                            for (int j = 0; j < i; j++)
                            {
                                precedingStatements.Add(statements[j]);
                            }
                            List<BoundStatement> followingStatements = new List<BoundStatement>();
                            for (int k = i + 1; i < statements.Length; k++)
                                followingStatements.Add(statements[k]);
                            return LowerBoundMultipleLocalDeclarationUsingVar(boundMultiple, node.Locals, precedingStatements, followingStatements.ToImmutableArray());
                        }
                    }
                }
            }
            return node;
        }

        private BoundBlock LowerBoundMultipleLocalDeclarationUsingVar(BoundMultipleLocalDeclarations boundMultiple,
                                                                      ImmutableArray<LocalSymbol> locals,
                                                                      ArrayBuilder<BoundStatement> precedingStatements,
                                                                      ImmutableArray<BoundStatement> followingStatements)
        {
            int itemCount = boundMultiple.LocalDeclarations.Length;
            ArrayBuilder<BoundLocalDeclaration> reversedLocals = ArrayBuilder<BoundLocalDeclaration>.GetInstance(itemCount);
            reversedLocals.AddRange(boundMultiple.LocalDeclarations, itemCount);
            reversedLocals.ReverseContents();

            List<BoundUsingStatement> reversedUsingStatements = new List<BoundUsingStatement>();
            for (int i = 0; i < reversedLocals.Count; i++)
            {
                BoundBlock innerBlock;
                // The first element in the reversed lists' using statement must contain all following statements.
                if (i == 0)
                {
                    innerBlock = new BoundBlock(
                            syntax: boundMultiple.Syntax,
                            locals: ImmutableArray<LocalSymbol>.Empty,
                            statements: followingStatements
                            );
                }
                // All other elements will only contain the previous element as a following statement.
                else
                {
                    BoundStatement previousUsing = reversedUsingStatements[reversedUsingStatements.Count - 1];
                    innerBlock = new BoundBlock(
                            syntax: boundMultiple.Syntax,
                            locals: ImmutableArray<LocalSymbol>.Empty,
                            statements: ImmutableArray.Create(previousUsing)
                            );
                }

                BoundUsingStatement boundUsing = new BoundUsingStatement(
                        syntax: boundMultiple.Syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        declarationsOpt: new BoundMultipleLocalDeclarations(
                            boundMultiple.Syntax,
                            ImmutableArray.Create(boundMultiple.LocalDeclarations[i])),
                        expressionOpt: null,
                        iDisposableConversion: Conversion.Identity,
                        disposeMethodOpt: null,
                        body: innerBlock
                        );
                reversedUsingStatements.Add(boundUsing);
            }

            precedingStatements.Add((BoundStatement)reversedUsingStatements[reversedUsingStatements.Count - 1]);
            BoundBlock outermostBlock = new BoundBlock(
                            syntax: boundMultiple.Syntax,
                            locals: locals,
                            statements: precedingStatements.ToImmutableArray());
            return outermostBlock;
        }
    }
}
