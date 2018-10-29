using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    interface IPullMemberUpSyntaxChangeService : ILanguageService 
    {
        void RemoveNode(DocumentEditor editor, SyntaxNode node, ISymbol symbol);

        void ChangeMemberToPublic(DocumentEditor editor, ISymbol memberSymbol, SyntaxNode memberSytax, SyntaxNode containingTypeSyntax, ICodeGenerationService codeGenerationService);

        void ChangeMemberToNonStatic(DocumentEditor editor, ISymbol memberSymbol, SyntaxNode memberSyntax, SyntaxNode containingTypeSyntax, ICodeGenerationService codeGenerationService);
    }
}
