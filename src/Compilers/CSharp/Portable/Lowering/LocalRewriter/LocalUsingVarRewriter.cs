// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Lowering.LocalRewriter
{
    internal class LocalUsingVarRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
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
                if (statements[i] is BoundLocalDeclaration localDeclaration && localDeclaration.LocalSymbol.IsUsing)
                {
                    return LowerBoundLocalDeclarationUsingVar(node, statements);
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
                            ArrayBuilder<BoundStatement> followingStatements = ArrayBuilder<BoundStatement>.GetInstance(statements.Length - (i + 1));
                            for (int k = i + 1; k < statements.Length; k++)
                            {
                                followingStatements.Add(statements[k]);
                            }
                            return LowerBoundMultipleLocalDeclarationUsingVar(boundMultiple, node.Locals, precedingStatements, followingStatements.ToImmutableArray());
                        }
                    }
                }
            }
            return node;
        }

        private BoundBlock LowerBoundLocalDeclarationUsingVar(BoundBlock node, ImmutableArray<BoundStatement> statements)
        {
            int itemCount = statements.Length;
            int firstUsingIndex = 0;

            ArrayBuilder<BoundStatement> reversedStatements = ArrayBuilder<BoundStatement>.GetInstance(itemCount);
            reversedStatements.AddRange(statements, itemCount);
            reversedStatements.ReverseContents();
            
            ArrayBuilder<BoundUsingStatement> reversedUsingStatements = ArrayBuilder<BoundUsingStatement>.GetInstance();

            for (int i = 0; i < itemCount; i++)
            {
                if (reversedStatements[i] is BoundLocalDeclaration localDeclaration && !(reversedStatements[i] is BoundReturnStatement) && SwitchBinder.ContainsUsingVariable(reversedStatements[i]))
                {
                    var followingStatements = GetFollowingStatements(statements, itemCount - 1 - i);
                    firstUsingIndex = i;

                    // Append inner using variable to the following statements of the previous one
                    if (reversedUsingStatements.Count != 0)
                    {
                        followingStatements.Add(reversedUsingStatements[reversedUsingStatements.Count - 1]);
                    }

                    BoundBlock innerBlock = new BoundBlock(
                            syntax: localDeclaration.Syntax,
                            locals: ImmutableArray<LocalSymbol>.Empty,
                            statements: followingStatements.AsImmutable()
                            );

                    BoundUsingStatement boundUsing = new BoundUsingStatement(
                        syntax: localDeclaration.Syntax,
                        locals: ImmutableArray<LocalSymbol>.Empty,
                        declarationsOpt: new BoundMultipleLocalDeclarations(
                            localDeclaration.Syntax,
                            ImmutableArray.Create(localDeclaration)),
                        expressionOpt: null,
                        iDisposableConversion: Conversion.Identity,
                        disposeMethodOpt: null,
                        body: innerBlock
                        );
                    reversedUsingStatements.Add(boundUsing);
                }
            }

            ArrayBuilder<BoundStatement> precedingStatements = ArrayBuilder<BoundStatement>.GetInstance(firstUsingIndex);
            for (int i = 0; i < (itemCount - 1 - firstUsingIndex); i++)
            {
                precedingStatements.Add(statements[i]);
            }
            if (reversedUsingStatements.Count != 0)
            {
                precedingStatements.Add(reversedUsingStatements[reversedUsingStatements.Count - 1]);
            }

            BoundBlock outermostBlock = new BoundBlock(
                            syntax: node.Syntax,
                            locals: node.Locals,
                            statements: precedingStatements.AsImmutableOrEmpty());
            return outermostBlock;
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

            ArrayBuilder<BoundUsingStatement> reversedUsingStatements = ArrayBuilder<BoundUsingStatement>.GetInstance();
            for (int i = 0; i < reversedLocals.Count; i++)
            {
                // The first element in the reversed lists' using statement must contain all following statements.
                // All other elements will only contain the previous element as a following statement.
                var innerBlockStatements = i == 0 ? followingStatements : ImmutableArray.Create((BoundStatement)reversedUsingStatements[reversedUsingStatements.Count - 1]);

                BoundBlock innerBlock = new BoundBlock(
                            syntax: boundMultiple.Syntax,
                            locals: ImmutableArray<LocalSymbol>.Empty,
                            statements: innerBlockStatements
                            );

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

        private ArrayBuilder<BoundStatement> GetFollowingStatements(ImmutableArray<BoundStatement> statements, int startingPoint)
        {
            ArrayBuilder<BoundStatement> followingStatements = ArrayBuilder<BoundStatement>.GetInstance(statements.Length);
            for (int i = startingPoint + 1; i < statements.Length; i++)
            {
                if (SwitchBinder.ContainsUsingVariable(statements[i]) && !(statements[i] is BoundReturnStatement))
                {
                    break;
                }
                else
                {
                    followingStatements.Add(statements[i]);
                }
            }
            return followingStatements;
        }
    }
}
