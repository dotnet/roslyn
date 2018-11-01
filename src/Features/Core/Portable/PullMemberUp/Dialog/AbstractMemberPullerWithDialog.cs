// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog
{
    internal abstract class AbstractMemberPullerWithDialog
    {
        private readonly HashSet<DocumentId> _changedDocuments;

        private HashSet<DocumentId> ChangedDocuments { get => _changedDocuments ?? new HashSet<DocumentId>(); }

        protected async Task ChangeMembers(
            PullMemberDialogResult result,
            SolutionEditor solutionEditor,
            Document contextDocument,
            Func<(ISymbol member, bool makeAbstract), bool> memberFilter,
            Action<SyntaxNode, ISymbol, SyntaxNode, DocumentEditor> actionOnMember,
            CancellationToken cancellationToken)
        {
            var groupedSyntaxAndSymbol = await GetSyntaxAndGroupingMembers(result, memberFilter, cancellationToken);
            foreach (var groupingResult in groupedSyntaxAndSymbol)
            {
                var document = contextDocument.Project.Solution.GetDocument(groupingResult.Key);
                var editor = await solutionEditor.GetDocumentEditorAsync(document.Id).ConfigureAwait(false);
                ChangedDocuments.Add(document.Id);
                foreach (var (syntax, symbol) in groupingResult)
                {
                    if (syntax != null)
                    {
                        var containingType = FindContainingTypeNode(syntax, document);
                        if (containingType != null)
                        {
                            actionOnMember(syntax, symbol, containingType, editor);
                        }
                    }
                }
            }
        }

        private async Task<IEnumerable<IGrouping<SyntaxTree, (SyntaxNode, ISymbol)>>> GetSyntaxAndGroupingMembers(
            PullMemberDialogResult result,
            Func<(ISymbol member, bool makeAbstract), bool> memberFilter,
            CancellationToken cancellationToken)
        {
            // Group syntax by its syntax tree since members may come from different document
            var membersToChange = result.SelectedMembers.
                Where(selectionPair => memberFilter(selectionPair)).Select(selectionPair => selectionPair.member);

            var tasks = membersToChange.
                SelectMany(symbol => symbol.DeclaringSyntaxReferences.Select(async @ref => (syntax: await @ref.GetSyntaxAsync(cancellationToken), symbol)));

            return (await Task.WhenAll(tasks).ConfigureAwait(false)).GroupBy(symbolAndSyntaxPair => symbolAndSyntaxPair.syntax.SyntaxTree);
        }

        private SyntaxNode FindContainingTypeNode(SyntaxNode node, Document document)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            return generator.GetDeclaration(node, DeclarationKind.Class) ?? generator.GetDeclaration(node, DeclarationKind.Interface);
        }
    }
}
