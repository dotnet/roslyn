// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Utilities
{
    internal sealed class CSharpRefactoringHelpers : AbstractRefactoringHelpers<ExpressionSyntax, ArgumentSyntax, ExpressionStatementSyntax>
    {
        public static CSharpRefactoringHelpers Instance { get; } = new CSharpRefactoringHelpers();

        private CSharpRefactoringHelpers()
        {
        }

        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        protected override IEnumerable<SyntaxNode> ExtractNodesSimple(SyntaxNode? node, ISyntaxFacts syntaxFacts)
        {
            if (node == null)
            {
                yield break;
            }

            foreach (var extractedNode in base.ExtractNodesSimple(node, syntaxFacts))
            {
                yield return extractedNode;
            }

            // `var a = b;`
            // -> `var a = b`;
            if (node is LocalDeclarationStatementSyntax localDeclaration)
            {
                yield return localDeclaration.Declaration;
            }

            // var `a = b`;
            if (node is VariableDeclaratorSyntax declarator)
            {
                var declaration = declarator.Parent;
                if (declaration?.Parent is LocalDeclarationStatementSyntax localDeclarationStatement)
                {
                    var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
                    if (variables.Count == 1)
                    {
                        // -> `var a = b`;
                        yield return declaration;

                        // -> `var a = b;`
                        yield return localDeclarationStatement;
                    }
                }
            }
        }
    }
}
