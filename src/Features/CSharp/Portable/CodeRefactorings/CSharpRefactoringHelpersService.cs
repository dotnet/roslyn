// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

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

        protected override SyntaxNode? ExtractSingleVariableFromFieldDeclaration(SyntaxNode root, int location)
        {
            var token = root.FindToken(location);
            if (token.Parent != null)
            {
                var baseFieldNode = token.Parent.FirstAncestorOrSelf<BaseFieldDeclarationSyntax>();
                if (baseFieldNode is { Declaration: { Variables: { Count: 1 } } } && !baseFieldNode.AttributeLists.Span.Contains(location))
                {
                    return baseFieldNode.Declaration.Variables[0];
                }
            }

            return null;
        }
    }
}
