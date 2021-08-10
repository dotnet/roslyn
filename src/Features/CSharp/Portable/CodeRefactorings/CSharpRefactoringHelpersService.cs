// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings
{
    [ExportLanguageService(typeof(IRefactoringHelpersService), LanguageNames.CSharp), Shared]
    internal class CSharpRefactoringHelpersService : AbstractRefactoringHelpersService<ExpressionSyntax, ArgumentSyntax, ExpressionStatementSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRefactoringHelpersService()
        {
        }

        protected override IEnumerable<SyntaxNode> ExtractNodesSimple(SyntaxNode? node, ISyntaxFactsService syntaxFacts)
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

        protected override bool TryGetVariableDeclaratorInSingleFieldDeclaration(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? singleVariableDeclarator)
        {
            singleVariableDeclarator = null;
            if (node is BaseFieldDeclarationSyntax { Declaration: { Variables: { Count: 1 } } } baseFieldDeclarationNode)
            {
                singleVariableDeclarator = baseFieldDeclarationNode.Declaration.Variables[0];
                return true;
            }

            if (node.Parent is BaseFieldDeclarationSyntax && node is VariableDeclarationSyntax { Variables: { Count: 1 } } variableDeclarationNode)
            {
                singleVariableDeclarator = variableDeclarationNode.Variables[0];
                return true;
            }

            return false;
        }
    }
}
