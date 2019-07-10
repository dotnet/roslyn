// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings
{
    [ExportLanguageService(typeof(IRefactoringHelpersService), LanguageNames.CSharp), Shared]
    internal class CSharpRefactoringHelpersService : AbstractRefactoringHelpersService<PropertyDeclarationSyntax, ParameterSyntax, MethodDeclarationSyntax>
    {
        protected override SyntaxNode ExtractNodeOfHeader(SyntaxNode node, ISyntaxFactsService syntaxFacts)
        {
            var baseExtraction = base.ExtractNodeOfHeader(node, syntaxFacts);
            if (baseExtraction != null)
            {
                return baseExtraction;
            }

            if (IsInLocalFunctionHeader(node, syntaxFacts))
            {
                return node.GetAncestorOrThis<LocalFunctionStatementSyntax>();
            }

            return null;
        }

        private bool IsInLocalFunctionHeader(SyntaxNode node, ISyntaxFactsService syntaxFacts)
        {
            var containingMethod = node.GetAncestorOrThis<LocalFunctionStatementSyntax>();
            if (containingMethod == null)
            {
                return false;
            }

            return syntaxFacts.IsInHeader(node, containingMethod, containingMethod.ParameterList);
        }
    }
}
