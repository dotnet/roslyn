// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog
{
    internal abstract class AbstractMemberPullerWithDialog
    {
        protected ICodeGenerationService CodeGenerationService { get; }

        protected IPullMemberUpSyntaxChangeService ChangeService { get; }

        protected Document ContextDocument { get; }

        internal AbstractMemberPullerWithDialog(Document document)
        {
            CodeGenerationService = document.Project.LanguageServices.GetRequiredService<ICodeGenerationService>();
            ChangeService = document.Project.LanguageServices.GetRequiredService<IPullMemberUpSyntaxChangeService>();
        }

        protected async Task ChangeMembers(
            PullMemberDialogResult result,
            Func<(ISymbol member, bool makeAbstract), bool> memberFilter,
            Func<SyntaxNode, ISymbol, SyntaxNode, Task> actionOnMember,
            CancellationToken cancellationToken)
        {
            var member = result.SelectedMembers.Where(selectionPair => memberFilter(selectionPair)).Select(selectionPair => selectionPair.member);
            var tasks = member.
                Select(async symbol =>
                (memberSyntax: await symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntaxAsync(cancellationToken),
                memberSymbol: symbol));
            var syntaxAndSymbolPairs = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var (syntax, symbol) in syntaxAndSymbolPairs)
            {
                var containingType = await FindContainingTypeNode(syntax, symbol, cancellationToken);
                if (syntax != null && containingType != null)
                {
                    await actionOnMember(syntax, symbol, containingType);
                }
            }
        }

        protected async Task<SyntaxNode> FindContainingTypeNode(SyntaxNode syntaxNode, ISymbol symbol, CancellationToken cancellationToken)
        {
            var nodes = await Task.WhenAll(
                symbol.ContainingSymbol.DeclaringSyntaxReferences.
                Select(@ref => @ref.GetSyntaxAsync(cancellationToken))).ConfigureAwait(false);
            return nodes.Where(node => node.Contains(syntaxNode)).FirstOrDefault();
        }
    }
}
