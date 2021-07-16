// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode? VisitMultipleLocalDeclarations(BoundMultipleLocalDeclarations node)
        {
            return VisitMultipleLocalDeclarationsBase(node);
        }

        public override BoundNode? VisitUsingLocalDeclarations(BoundUsingLocalDeclarations node)
        {
            return VisitMultipleLocalDeclarationsBase(node);
        }

        private BoundNode? VisitMultipleLocalDeclarationsBase(BoundMultipleLocalDeclarationsBase node)
        {
            ArrayBuilder<BoundStatement>? inits = null;

            foreach (var decl in node.LocalDeclarations)
            {
                var init = VisitLocalDeclaration(decl);

                if (init != null)
                {
                    if (inits == null)
                    {
                        inits = ArrayBuilder<BoundStatement>.GetInstance();
                    }

                    inits.Add((BoundStatement)init);
                }
            }

            if (inits != null)
            {
                return BoundStatementList.Synthesized(node.Syntax, node.HasErrors, inits.ToImmutableAndFree());
            }
            else
            {
                // no initializers
                return null; // TODO: but what if hasErrors?  Have we lost that?
            }
        }

    }
}
