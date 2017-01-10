// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeRefactorings.ConvertIfToSwitch
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

        protected interface IPattern
        {
            SyntaxNode CreateSwitchLabel();
        }

        protected interface IAnalyzer
        {
            Task ComputeRefactoringsAsync(CodeRefactoringContext context);
        }

        protected abstract IAnalyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel);

        protected abstract class Analyzer<TStatementSyntax, TIfStatementSyntax, TExpressionSyntax> : IAnalyzer
            where TExpressionSyntax : SyntaxNode
            where TIfStatementSyntax : SyntaxNode
        {
            protected readonly ISyntaxFactsService _syntaxFacts;
            protected readonly SemanticModel _semanticModel;
            private TExpressionSyntax _switchExpression;

            public Analyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
            {
                _syntaxFacts = syntaxFacts;
                _semanticModel = semanticModel;
            }

            public async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var document = context.Document;
                var cancellationToken = context.CancellationToken;
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var ifStatement = root.FindNode(context.Span).FirstAncestorOrSelf<TIfStatementSyntax>();
                if (ifStatement == null)
                {
                    return;
                }

                if (ifStatement.ContainsDiagnostics)
                {
                    return;
                }

                if (!ifStatement.GetFirstToken().GetLocation().SourceSpan.IntersectsWith(context.Span))
                {
                    return;
                }

                if (!CanConvertIfToSwitch(ifStatement))
                {
                    return;
                }

                var switchDefaultBody = default(TStatementSyntax);
                var switchSections = new List<(IEnumerable<IPattern>, TStatementSyntax)>();

                // Iterate over if-else statement chain.
                foreach (var (body, condition) in GetIfElseStatementChain(ifStatement))
                {
                    if (condition == null)
                    {
                        switchDefaultBody = body;
                        break;
                    }

                    var operands = GetLogicalOrExpressionOperands(condition);
                    var patterns = new List<IPattern>();

                    // Iterate over "||" operands to make a case label per each condition.
                    foreach (var operand in operands.Reverse())
                    {
                        var pattern = CreatePatternFromExpression(operand);
                        if (pattern == null)
                        {
                            return;
                        }

                        patterns.Add(pattern);
                    }

                    switchSections.Add((patterns, body));
                }

                context.RegisterRefactoring(new MyCodeAction(Title, c =>
                    UpdateDocumentAsync(root, document, ifStatement, switchDefaultBody, switchSections)));
            }

            protected bool SetInitialOrIsEquivalentToSwitchExpression(TExpressionSyntax expression)
            {
                // If we have not figured the switch expression yet,
                // we will assume that the first expression is the one.
                if (_switchExpression == null)
                {
                    _switchExpression = expression;
                    return true;
                }

                return _syntaxFacts.AreEquivalent(expression, _switchExpression);
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
                        default((TExpressionSyntax, TExpressionSyntax));

                return constant != null;
            }

            protected abstract bool CanConvertIfToSwitch(TIfStatementSyntax ifStatement);

            protected abstract IEnumerable<TExpressionSyntax> GetLogicalOrExpressionOperands(TExpressionSyntax syntaxNode);

            protected abstract IEnumerable<(TStatementSyntax, TExpressionSyntax)> GetIfElseStatementChain(TIfStatementSyntax ifStatement);

            protected abstract IPattern CreatePatternFromExpression(TExpressionSyntax operand);

            protected abstract IEnumerable<SyntaxNode> GetSwitchSectionBody(TStatementSyntax switchDefaultBody);

            protected abstract IEnumerable<SyntaxNode> GetSubsequentIfStatements(TIfStatementSyntax ifStatement);

            protected abstract string Title { get; }

            private Task<Document> UpdateDocumentAsync(
                SyntaxNode root,
                Document document,
                TIfStatementSyntax ifStatement,
                TStatementSyntax switchDefaultBody,
                IEnumerable<(IEnumerable<IPattern> patterns, TStatementSyntax statement)> sections)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var sectionList =
                    sections.Select(s => generator.SwitchSectionFromLabels(
                        labels: s.patterns.Select(p => p.CreateSwitchLabel()),
                        statements: GetSwitchSectionBody(s.statement))).ToList();

                if (switchDefaultBody?.Equals(default(TStatementSyntax)) == false)
                {
                    sectionList.Add(generator.DefaultSwitchSection(GetSwitchSectionBody(switchDefaultBody)));
                }

                var ifSpan = ifStatement.Span;
                var @switch = generator.SwitchStatement(_switchExpression, sectionList);
                var subsequentIfStatements = GetSubsequentIfStatements(ifStatement);
                root = root.RemoveNodes(subsequentIfStatements, SyntaxRemoveOptions.KeepNoTrivia);
                root = root.ReplaceNode(root.FindNode(ifSpan), @switch);
                return Task.FromResult(document.WithSyntaxRoot(root));
            }
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