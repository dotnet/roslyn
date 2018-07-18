// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            int current = 0;
            ImmutableArray<BoundStatement> statements = (ImmutableArray<BoundStatement>)this.VisitList(node.Statements);

            foreach (BoundStatement statement in statements)
            {
                if (statement is BoundLocalDeclaration localDeclaration)
                {
                    if (localDeclaration.LocalSymbol.IsUsing)
                    {
                        ImmutableArray<LocalSymbol> locals = ImmutableArray.Create<LocalSymbol>(localDeclaration.LocalSymbol);

                        List<BoundStatement> precedingStatements = new List<BoundStatement>();
                        for (int j = 0; j < current; j++)
                        {
                            precedingStatements.Add(statements[j]);
                        }
                        var followingStatements = ImmutableArray.Create(
                            statements, current + 1,
                            statements.Length - current - 1);

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
                else if (statement is BoundMultipleLocalDeclarations boundMultiple)
                {
                    if (boundMultiple.LocalDeclarations.Any())
                    {
                        if (boundMultiple.LocalDeclarations[0].LocalSymbol.IsUsing)
                        {
                            List<BoundStatement> precedingStatements = new List<BoundStatement>();
                            for (int j = 0; j < current; j++)
                            {
                                precedingStatements.Add(statements[j]);
                            }
                            List<BoundStatement> followingStatements = new List<BoundStatement>();
                            for (int i = current + 1; i < statements.Length; i++)
                                followingStatements.Add(statements[i]);
                            return LowerBoundMultipleLocalDeclarationUsingVar(boundMultiple, node.Locals, precedingStatements, followingStatements.ToImmutableArray());
                        }
                    }
                }
                current++;
            }
            return node;
        }

        private BoundBlock LowerBoundMultipleLocalDeclarationUsingVar(BoundMultipleLocalDeclarations boundMultiple,
                                                                      ImmutableArray<LocalSymbol> locals,
                                                                      List<BoundStatement> precedingStatements,
                                                                      ImmutableArray<BoundStatement> followingStatements)
        {
            List<BoundLocalDeclaration> reversedLocals = Enumerable.Reverse(boundMultiple.LocalDeclarations).ToList();
            List<BoundUsingStatement> reversedUsingStatements = new List<BoundUsingStatement>();
            for (int i = 0; i < reversedLocals.Count; i++)
            {
                BoundBlock innerBlock; 
                // The first element in the reversed lists' using statement must contain all following statements.
                if (i == 0)
                {
                    innerBlock = new BoundBlock(
                            syntax: boundMultiple.Syntax,
                            locals: ImmutableArray.Create<LocalSymbol>(),
                            statements: followingStatements
                            );
                }
                // All other elements will only contain the previous element as a following statement.
                else
                {
                    BoundStatement previousUsing = reversedUsingStatements.Last();
                    innerBlock = new BoundBlock(
                            syntax: boundMultiple.Syntax,
                            locals: ImmutableArray.Create<LocalSymbol>(),
                            statements: ImmutableArray.Create(previousUsing)
                            );
                }

                BoundUsingStatement boundUsing = new BoundUsingStatement(
                        syntax: boundMultiple.Syntax,
                        locals: ImmutableArray.Create<LocalSymbol>(),
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

            precedingStatements.Add((BoundStatement)reversedUsingStatements.Last());
            BoundBlock outermostBlock = new BoundBlock(
                            syntax: boundMultiple.Syntax,
                            locals: locals,
                            statements: precedingStatements.ToImmutableArray());
            return outermostBlock;
        }

        internal static bool ContainsUsingVariable(BoundStatement boundStatement)
        {
            if (boundStatement is BoundLocalDeclaration boundLocal)
            {
                return boundLocal.LocalSymbol.IsUsing;
            }
            else if (boundStatement is BoundMultipleLocalDeclarations boundMultiple)
            {
                if (!boundMultiple.LocalDeclarations.IsEmpty)
                {
                    return boundMultiple.LocalDeclarations[0].LocalSymbol.IsUsing;
                }
            }
            return false;
        }
    }
}
