// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings
{
    [ExportLanguageService(typeof(IRefactoringHelpersService), LanguageNames.CSharp), Shared]
    internal class CSharpRefactoringHelpersService : AbstractRefactoringHelpersService<ExpressionSyntax, ArgumentSyntax>
    {
        protected override IEnumerable<SyntaxNode> ExtractNodesSimple(SyntaxNode node, ISyntaxFactsService syntaxFacts)
        {
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
                var localDeclarationStatement = declarator.Parent?.Parent as LocalDeclarationStatementSyntax;
                if (localDeclarationStatement != null)
                {
                    var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
                    if (variables.Count == 1)
                    {
                        // -> `var a = b`;
                        yield return declarator.Parent;

                        // -> `var a = b;`
                        yield return localDeclarationStatement;
                    }
                }

            }

        }
    }
}
