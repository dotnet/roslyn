// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMembrUp.Dialog;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.Dialog
{
    [ExportLanguageService(typeof(AbstractClassPullerWithDialog), LanguageNames.CSharp), Shared]
    internal class CSharpClassPullerViaDialog : AbstractClassPullerWithDialog
    {
        protected override void RemoveNode(DocumentEditor editor, SyntaxNode node, ISymbol symbol)
        {
            var remover = new PullMemberUpSyntaxEditor();
            remover.RemoveNode(editor, node, symbol);
        }
    }
}
