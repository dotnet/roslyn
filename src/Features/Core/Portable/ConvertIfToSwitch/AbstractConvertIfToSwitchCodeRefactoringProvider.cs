// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    internal abstract class AbstractConvertIfToSwitchCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);

            await CreateAnalyzer(syntaxFacts, semanticModel)
                .ComputeRefactoringsAsync(context).ConfigureAwait(false);
        }

        protected interface IPattern<TSwitchLabelSyntax>
            where TSwitchLabelSyntax : SyntaxNode
        {
            TSwitchLabelSyntax CreateSwitchLabel();
        }

        protected interface IAnalyzer
        {
            Task ComputeRefactoringsAsync(CodeRefactoringContext context);
        }

        protected abstract IAnalyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel);

        protected abstract class Analyzer<TStatementSyntax, TIfStatementSyntax, TExpressionSyntax, TSwitchLabelSyntax> : IAnalyzer
            where TExpressionSyntax : SyntaxNode
            where TIfStatementSyntax : SyntaxNode
            where TSwitchLabelSyntax : SyntaxNode
        {
            protected readonly ISyntaxFactsService _syntaxFacts;
            protected readonly SemanticModel _semanticModel;
            private int _numberOfSubsequentIfStatementsToRemove = -1;
            private TExpressionSyntax _switchExpression;
            private Optional<TStatementSyntax> _switchDefaultBodyOpt;

            public Analyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
            {
                _syntaxFacts = syntaxFacts;
                _semanticModel = semanticModel;
            }

            public async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var (document, textSpan, cancellationToken) = context;
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var ifStatement = await context.TryGetSelectedNodeAsync<TIfStatementSyntax>().ConfigureAwait(false);
                if (ifStatement == null || ifStatement.ContainsDiagnostics)
                {
                    return;
                }

                var switchSections = GetSections(ifStatement).ToList();

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
                var labelCount = switchSections.SelectMany(t => t.patterns).Count() +
                    (_switchDefaultBodyOpt.HasValue ? 1 : 0);
                if (labelCount < 2)
                {
                    return;
                }

                context.RegisterRefactoring(new MyCodeAction(Title, c =>
                    UpdateDocumentAsync(root, document, ifStatement, switchSections)));
            }

            private IEnumerable<(IEnumerable<IPattern<TSwitchLabelSyntax>> patterns, TStatementSyntax statement)> GetSections(
                TIfStatementSyntax rootIfStatement)
            {
                // Iterate over subsequent if-statements whose endpoint is unreachable.
                foreach (var statement in GetSubsequentStatements(rootIfStatement))
                {
                    if (!(statement is TIfStatementSyntax ifStatement))
                    {
                        yield break;
                    }

                    if (!CanConvertIfToSwitch(ifStatement))
                    {
                        yield break;
                    }

                    var sectionList = new List<(IEnumerable<IPattern<TSwitchLabelSyntax>>, TStatementSyntax)>();

                    // Iterate over if-else statement chain.
                    foreach (var (condition, body) in GetIfElseStatementChain(ifStatement))
                    {
                        // If there is no condition, we have reached the "else" part.
                        if (condition == null)
                        {
                            _switchDefaultBodyOpt = body;
                            break;
                        }

                        var patternList = new List<IPattern<TSwitchLabelSyntax>>();

                        // Iterate over "||" or "OrElse" operands to make a case label per each condition.
                        var patterns = GetLogicalOrOperands(condition).Reverse().Select(CreatePatternFromExpression);
                        foreach (var pattern in patterns)
                        {
                            // If we could not create a pattern from the condition, we stop.
                            if (pattern == null)
                            {
                                yield break;
                            }

                            patternList.Add(pattern);
                        }

                        sectionList.Add((patternList, body));
                    }

                    foreach (var section in sectionList)
                    {
                        yield return section;
                    }

                    _numberOfSubsequentIfStatementsToRemove++;

                    if (_switchDefaultBodyOpt.HasValue || EndPointIsReachable(ifStatement))
                    {
                        yield break;
                    }
                }
            }

            protected bool SetInitialOrIsEquivalentToSwitchExpression(TExpressionSyntax expression)
            {
                // If we have not figured the switch expression yet,
                // we will assume that the first expression is the one.
                if (_switchExpression == null)
                {
                    _switchExpression = UnwrapCast(expression);
                    return true;
                }

                return _syntaxFacts.AreEquivalent(UnwrapCast(expression), _switchExpression);
            }

            private bool IsConstant(TExpressionSyntax node)
                => _semanticModel.GetConstantValue(node).HasValue;

            protected bool TryDetermineConstant(
                TExpressionSyntax expression1,
                TExpressionSyntax expression2,
                out TExpressionSyntax constant,
                out TExpressionSyntax expression)
            {
                (constant, expression) =
                        IsConstant(expression1) ? (expression1, expression2) :
                        IsConstant(expression2) ? (expression2, expression1) :
                        default;

                return constant != null;
            }

            private IEnumerable<SyntaxNode> GetSubsequentStatements(SyntaxNode currentStatement)
            {
                do
                {
                    yield return currentStatement;
                    currentStatement = _syntaxFacts.GetNextExecutableStatement(currentStatement);
                }
                while (currentStatement != null);
            }

            private Task<Document> UpdateDocumentAsync(
                SyntaxNode root,
                Document document,
                TIfStatementSyntax ifStatement,
                IEnumerable<(IEnumerable<IPattern<TSwitchLabelSyntax>> patterns, TStatementSyntax statement)> sections)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var sectionList =
                    sections.Select(s => generator.SwitchSectionFromLabels(
                        labels: s.patterns.Select(p => p.CreateSwitchLabel()),
                        statements: GetSwitchSectionBody(s.statement))).ToList();

                if (_switchDefaultBodyOpt.HasValue)
                {
                    sectionList.Add(generator.DefaultSwitchSection(GetSwitchSectionBody(_switchDefaultBodyOpt.Value)));
                }

                var ifSpan = ifStatement.Span;
                var @switch = CreateSwitchStatement(ifStatement, _switchExpression, sectionList);
                var nodesToRemove = GetSubsequentStatements(ifStatement)
                    .Skip(1).Take(_numberOfSubsequentIfStatementsToRemove).ToList();
                root = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);

                var lastNode = nodesToRemove.LastOrDefault() ?? ifStatement;
                @switch = @switch.WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                                 .WithTrailingTrivia(lastNode.GetTrailingTrivia())
                                 .WithAdditionalAnnotations(Formatter.Annotation);

                root = root.ReplaceNode(root.FindNode(ifSpan), @switch);
                return Task.FromResult(document.WithSyntaxRoot(root));
            }

            protected abstract SyntaxNode CreateSwitchStatement(TIfStatementSyntax ifStatement, TExpressionSyntax expression, List<SyntaxNode> sectionList);

            protected abstract TExpressionSyntax UnwrapCast(TExpressionSyntax expression);

            protected abstract bool EndPointIsReachable(TIfStatementSyntax ifStatement);

            protected abstract bool CanConvertIfToSwitch(TIfStatementSyntax ifStatement);

            protected abstract IEnumerable<TExpressionSyntax> GetLogicalOrOperands(TExpressionSyntax syntaxNode);

            protected abstract IEnumerable<(TExpressionSyntax, TStatementSyntax)> GetIfElseStatementChain(TIfStatementSyntax ifStatement);

            protected abstract IPattern<TSwitchLabelSyntax> CreatePatternFromExpression(TExpressionSyntax operand);

            protected abstract IEnumerable<SyntaxNode> GetSwitchSectionBody(TStatementSyntax switchDefaultBody);

            protected abstract string Title { get; }
        }

        protected sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
