// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<
        TIfStatementSyntax, TExpressionSyntax, TIsExpressionSyntax, TPatternSyntax> : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var ifStatement = await context.TryGetRelevantNodeAsync<TIfStatementSyntax>().ConfigureAwait(false);
            if (ifStatement == null || ifStatement.ContainsDiagnostics)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var ifOperation = semanticModel.GetOperation(ifStatement);
            if (!(ifOperation is IConditionalOperation { Parent: IBlockOperation { Operations: var operations } }))
            {
                return;
            }

            var index = operations.IndexOf(ifOperation);
            if (index == -1)
            {
                return;
            }

            var analyzer = CreateAnalyzer(document.GetLanguageService<ISyntaxFactsService>());
            var (sections, target) = analyzer.AnalyzeIfStatementSequence(operations.AsSpan().Slice(index));
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
            var labelCount = sections.Sum(section => section.Labels.IsDefault ? 1 : section.Labels.Length);
            if (labelCount < 2)
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(GetTitle(forSwitchExpression: false),
                    c => UpdateDocumentAsync(document, target, ifStatement, sections, convertToSwitchExpression: false, c)),
                ifStatement.Span);

            if (analyzer.SupportsSwitchExpression &&
                CanConvertToSwitchExpression(sections))
            {
                context.RegisterRefactoring(
                    new MyCodeAction(GetTitle(forSwitchExpression: true),
                        c => UpdateDocumentAsync(document, target, ifStatement, sections, convertToSwitchExpression: true, c)),
                    ifStatement.Span);
            }
        }

        public abstract string GetTitle(bool forSwitchExpression);
        public abstract Analyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts);

        private static bool CanConvertToSwitchExpression(ImmutableArray<AnalyzedSwitchSection> sections)
        {
            return
                sections.Any(section => section.Labels.IsDefault) &&
                sections.All(section => section.Labels.IsDefault || section.Labels.Length == 1) &&
                sections.Any(section => section.Body.Kind == OperationKind.Return) &&
                sections.All(section => CanConvertToSwitchArm(section.Body));

            static bool CanConvertToSwitchArm(IOperation op)
            {
                switch (op)
                {
                    case IReturnOperation { ReturnedValue: { } }:
                    case IThrowOperation { Exception: { } }:
                    case IBlockOperation { Operations: { Length: 1 } statements } when CanConvertToSwitchArm(statements[0]):
                        return true;
                }

                return false;
            }
        }

        public sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
