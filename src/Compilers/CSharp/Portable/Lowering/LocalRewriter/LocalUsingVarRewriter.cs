using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Lowering.LocalRewriter
{
    class LocalUsingVarRewriter : BoundTreeRewriterWithStackGuard
    {
        public static BoundNode Rewrite(BoundStatement statement)
        {
            var localUsingVarRewriter = new LocalUsingVarRewriter();
            return (BoundNode)localUsingVarRewriter.Visit(statement);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            int current = 0;
            ImmutableArray<BoundStatement> statements = (ImmutableArray<BoundStatement>)this.VisitList(node.Statements);

            foreach (BoundStatement statement in statements)
            {
                if (statement is BoundLocalDeclaration boundAssignment)
                {
                    if (boundAssignment.LocalSymbol.IsUsing)
                    {
                        ImmutableArray<LocalSymbol> locals = ImmutableArray.Create<LocalSymbol>(boundAssignment.LocalSymbol);
                        List<BoundStatement> followingStatements = new List<BoundStatement>();
                        for (int i = current + 1; i < statements.Length; i++)
                            followingStatements.Add(statements[i]);
                        List<BoundLocalDeclaration> localDeclarations = new List<BoundLocalDeclaration>();
                        localDeclarations.Add(boundAssignment);

                        BoundBlock boundBlock = new BoundBlock(
                            syntax: boundAssignment.Syntax,
                            locals: ImmutableArray.Create<LocalSymbol>(),
                            statements: followingStatements.ToImmutableArray<BoundStatement>()
                            );

                        BoundUsingStatement boundUsing = new BoundUsingStatement(
                            syntax: boundAssignment.Syntax,
                            locals: locals,
                            declarationsOpt: new BoundMultipleLocalDeclarations(
                                boundAssignment.Syntax,
                                localDeclarations.ToImmutableArray<BoundLocalDeclaration>()),
                            expressionOpt: null,
                            iDisposableConversion: Conversion.Identity,
                            body: boundBlock
                            );

                        return boundUsing;

                    }
                }

                current++;
            }
            return null;
        }
    }
}
