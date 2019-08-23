// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<TIfStatementSyntax> : CodeRefactoringProvider
        where TIfStatementSyntax : SyntaxNode
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var ifStatement = await context.TryGetRelevantNodeAsync<TIfStatementSyntax>();
            if (ifStatement == null || ifStatement.ContainsDiagnostics)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var ifOperation = semanticModel.GetOperation(ifStatement);
            if (!(ifOperation is IConditionalOperation {Parent: IBlockOperation {Operations: var operations}}))
            {
                return;
            }

            var index = operations.IndexOf(ifOperation);
            if (index == -1)
            {
                return;
            }

            var analyzer = CreateAnalyzer(document.GetLanguageService<ISyntaxFactsService>());
            var (sections, target) = analyzer.AnalyzeIfStatementSequence(operations.AsSpan().Slice(index), out var defaultBodyOpt);
            if (sections.IsDefaultOrEmpty)
            {
                return;
            }

            // To prevent noisiness we don't offer this unless we're going to generate at least
            // two switch labels.  It can be quite annoying to basically have this offered
            // on pretty much any simple 'if' like "if (a == 0)" or "if (x == null)".  In these
            // cases, the converted code just looks and feels worse, and it ends up causing the
            // lightbulb to appear too much.
            //
            // This does mean that if someone has a simple if, and is about to add a lot more
            // cases, and says to themselves "let me convert this to a switch first!", then they'll
            // be out of luck.  However, I believe the core value here is in taking existing large
            // if-chains/checks and easily converting them over to a switch.  So not offering the
            // feature on simple if-statements seems like an acceptable compromise to take to ensure
            // the overall user experience isn't degraded.
            var labelCount = sections.Sum(section => section.Labels.Length) + (defaultBodyOpt is object ? 1 : 0);
            if (labelCount < 2)
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(Title,
                _ => UpdateDocumentAsync(root, document, target, ifStatement, sections, defaultBodyOpt)));
        }

        private Task<Document> UpdateDocumentAsync(
            SyntaxNode root,
            Document document,
            SyntaxNode target,
            SyntaxNode ifStatement,
            ImmutableArray<SwitchSection> sections,
            IOperation? defaultBodyOpt)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var sectionList = sections
                .Select(section => generator.SwitchSectionFromLabels(
                    labels: section.Labels.Select(AsSwitchLabelSyntax),
                    statements: AsSwitchSectionStatements(section.Body)))
                .ToList();

            if (defaultBodyOpt is object)
            {
                sectionList.Add(generator.DefaultSwitchSection(AsSwitchSectionStatements(defaultBodyOpt)));
            }

            var ifSpan = ifStatement.Span;
            var @switch = CreateSwitchStatement(ifStatement, target, sectionList);

            foreach (var section in sections.AsSpan().Slice(1))
            {
                if (section.IfStatementSyntax.Parent != ifStatement.Parent)
                {
                    break;
                }

                root = root.RemoveNode(section.IfStatementSyntax, SyntaxRemoveOptions.KeepNoTrivia);
            }

            var lastNode = sections.LastOrDefault()?.IfStatementSyntax ?? ifStatement;
            @switch = @switch.WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(lastNode.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            root = root.ReplaceNode(root.FindNode(ifSpan), @switch);
            return Task.FromResult(document.WithSyntaxRoot(root));
        }

        protected abstract SyntaxNode CreateSwitchStatement(SyntaxNode ifStatement, SyntaxNode expression, IEnumerable<SyntaxNode> sectionList);
        protected abstract IEnumerable<SyntaxNode> AsSwitchSectionStatements(IOperation operation);
        protected abstract SyntaxNode AsSwitchLabelSyntax(SwitchLabel label);
        protected abstract Analyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts);
        protected abstract string Title { get; }

        protected sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
