// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.Dialog
{
    [ExportLanguageService(typeof(AbstractInterfacePullerWithDialog), LanguageNames.CSharp), Shared]
    internal class CSharpInterfacePullerViaDialog : AbstractInterfacePullerWithDialog
    {
        protected override void ChangeMemberToNonStatic(DocumentEditor editor, ISymbol symbol, SyntaxNode node, SyntaxNode containingTypeNode, ICodeGenerationService codeGenerationService)
        {
            var changer = new PullMemberUpSyntaxEditor();
            changer.ChangeMemberToNonStatic(editor, symbol, node, containingTypeNode, codeGenerationService);
        }

        protected override void ChangeMemberToPublic(DocumentEditor editor, ISymbol symbol, SyntaxNode node, SyntaxNode containingTypeNode, ICodeGenerationService codeGenerationService)
        {
            var changer = new PullMemberUpSyntaxEditor();
            changer.ChangeMemberToPublic(editor, symbol, node, containingTypeNode, codeGenerationService);
        }
    }
}
