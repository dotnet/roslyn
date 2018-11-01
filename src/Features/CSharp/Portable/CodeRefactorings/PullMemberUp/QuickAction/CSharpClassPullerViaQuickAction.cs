// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp.QuickAction;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.QuickAction
{
    [ExportLanguageService(typeof(AbstractClassPullerWithQuickAction), LanguageNames.CSharp), Shared]
    internal class CSharpClassPullerViaQuickAction : AbstractClassPullerWithQuickAction
    {
        protected override void RemoveNode(DocumentEditor editor, SyntaxNode node, ISymbol symbol)
        {
            var remover = new PullMemberUpSyntaxRemover();
            remover.RemoveNode(editor, node, symbol);
        }
    }
}
