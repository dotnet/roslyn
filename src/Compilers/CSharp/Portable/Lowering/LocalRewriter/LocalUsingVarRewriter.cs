// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LocalUsingVarRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        public static BoundNode Rewrite(BoundStatement statement)
        {
            var localUsingVarRewriter = new LocalUsingVarRewriter();
            return localUsingVarRewriter.Visit(statement);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            ImmutableArray<BoundStatement> statements = this.VisitList(node.Statements);
            for (int i = 0; i < statements.Length; i++)
            {
                if (statements[i] is BoundUsingLocalDeclarations localDeclaration)
                {
                    return LowerBoundLocalDeclarationUsingVar(node, statements);
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
                if (reversedStatements[i] is BoundUsingLocalDeclarations localDeclaration && SwitchBinder.ContainsUsingVariable(reversedStatements[i]))
                {
                    Debug.Assert(!(localDeclaration.IDisposableConversion != Conversion.NoConversion && localDeclaration.DisposeMethodOpt != default));

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
                        declarationsOpt: localDeclaration,
                        expressionOpt: null,
                        iDisposableConversion: localDeclaration.IDisposableConversion,
                        awaitOpt: null,
                        disposeMethodOpt: localDeclaration.DisposeMethodOpt,
                        body: innerBlock
                        );
                    reversedUsingStatements.Add(boundUsing);
                }
            }
            reversedStatements.Free();
            ArrayBuilder<BoundStatement> precedingStatements = ArrayBuilder<BoundStatement>.GetInstance(itemCount - firstUsingIndex);
            for (int i = 0; i < (itemCount - 1 - firstUsingIndex); i++)
            {
                precedingStatements.Add(statements[i]);
            }
            if (reversedUsingStatements.Count != 0)
            {
                precedingStatements.Add(reversedUsingStatements[reversedUsingStatements.Count - 1]);
            }
            reversedUsingStatements.Free();
            BoundBlock outermostBlock = new BoundBlock(
                            syntax: node.Syntax,
                            locals: node.Locals,
                            statements: precedingStatements.ToImmutableOrEmptyAndFree());
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
