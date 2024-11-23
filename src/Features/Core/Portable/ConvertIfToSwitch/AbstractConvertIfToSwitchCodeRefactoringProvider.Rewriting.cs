// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch;

internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<
    TIfStatementSyntax, TExpressionSyntax, TIsExpressionSyntax, TPatternSyntax>
{
    public abstract SyntaxNode CreateSwitchExpressionStatement(SyntaxNode target, ImmutableArray<AnalyzedSwitchSection> sections, Feature feature);
    public abstract SyntaxNode CreateSwitchStatement(TIfStatementSyntax ifStatement, SyntaxNode target, IEnumerable<SyntaxNode> sectionList);
    public abstract IEnumerable<SyntaxNode> AsSwitchSectionStatements(IOperation operation);
    public abstract SyntaxNode AsSwitchLabelSyntax(AnalyzedSwitchLabel label, Feature feature);
    protected abstract SyntaxTriviaList GetLeadingTriviaToTransfer(SyntaxNode syntaxToRemove);

    private async Task<Document> UpdateDocumentAsync(
        Document document,
        SyntaxNode target,
        TIfStatementSyntax ifStatement,
        ImmutableArray<AnalyzedSwitchSection> sections,
        Feature feature,
        bool convertToSwitchExpression,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var generator = SyntaxGenerator.GetGenerator(document);
        var ifSpan = ifStatement.Span;
        var options = root.SyntaxTree.Options;

        var @switch = convertToSwitchExpression
            ? CreateSwitchExpressionStatement(target, sections, feature)
            : CreateSwitchStatement(ifStatement, target, sections.Select(section => AsSwitchSectionSyntax(section, generator, feature)));

        var lastNode = sections.Last().SyntaxToRemove;
        @switch = @switch
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(lastNode.GetTrailingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation)
            .WithAdditionalAnnotations(Simplifier.Annotation);

        var nodesToRemove = sections.Skip(1).Select(s => s.SyntaxToRemove).Where(s => s.Parent == ifStatement.Parent);
        root = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        Debug.Assert(root is object); // we didn't remove the root
        root = root.ReplaceNode(root.FindNode(ifSpan, getInnermostNodeForTie: true), @switch);
        return document.WithSyntaxRoot(root);
    }

    private SyntaxNode AsSwitchSectionSyntax(AnalyzedSwitchSection section, SyntaxGenerator generator, Feature feature)
    {
        var statements = AsSwitchSectionStatements(section.Body);
        var sectionNode = section.Labels.IsDefault
            ? generator.DefaultSwitchSection(statements)
            : generator.SwitchSectionFromLabels(section.Labels.Select(label => AsSwitchLabelSyntax(label, feature)), statements);

        sectionNode = sectionNode.WithPrependedLeadingTrivia(GetLeadingTriviaToTransfer(section.SyntaxToRemove));

        return sectionNode;
    }
}
