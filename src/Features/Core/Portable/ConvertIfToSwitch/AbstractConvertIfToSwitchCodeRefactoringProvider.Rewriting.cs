// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<
        TIfStatementSyntax, TExpressionSyntax, TIsExpressionSyntax, TPatternSyntax>
    {
        public abstract SyntaxNode CreateSwitchExpressionStatement(SyntaxNode target, ImmutableArray<AnalyzedSwitchSection> sections);
        public abstract SyntaxNode CreateSwitchStatement(TIfStatementSyntax ifStatement, SyntaxNode target, IEnumerable<SyntaxNode> sectionList);
        public abstract IEnumerable<SyntaxNode> AsSwitchSectionStatements(IOperation operation);
        public abstract SyntaxNode AsSwitchLabelSyntax(AnalyzedSwitchLabel label);

        private async Task<Document> UpdateDocumentAsync(
            Document document,
            SyntaxNode target,
            TIfStatementSyntax ifStatement,
            ImmutableArray<AnalyzedSwitchSection> sections,
            bool convertToSwitchExpression,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(root);

            var generator = SyntaxGenerator.GetGenerator(document);
            var ifSpan = ifStatement.Span;

            SyntaxNode @switch;
            if (convertToSwitchExpression)
            {
                @switch = CreateSwitchExpressionStatement(target, sections);
            }
            else
            {
                var lastNode = sections.LastOrDefault()?.SyntaxToRemove ?? ifStatement;
                @switch = CreateSwitchStatement(ifStatement, target, sections.Select(AsSwitchSectionSyntax))
                    .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(lastNode.GetTrailingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }

            var nodesToRemove = sections.Skip(1).Select(s => s.SyntaxToRemove).Where(s => s.Parent == ifStatement.Parent);
            root = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
            root = root.ReplaceNode(root.FindNode(ifSpan), @switch);
            return document.WithSyntaxRoot(root);

            SyntaxNode AsSwitchSectionSyntax(AnalyzedSwitchSection section)
            {
                var statements = AsSwitchSectionStatements(section.Body);
                return section.Labels.IsDefault
                    ? generator.DefaultSwitchSection(statements)
                    : generator.SwitchSectionFromLabels(section.Labels.Select(AsSwitchLabelSyntax), statements);
            }
        }
    }
}
