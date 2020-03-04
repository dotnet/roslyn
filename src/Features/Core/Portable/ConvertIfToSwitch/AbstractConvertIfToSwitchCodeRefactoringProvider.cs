﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<
        TIfStatementSyntax, TExpressionSyntax, TIsExpressionSyntax, TPatternSyntax> : CodeRefactoringProvider
    {
        public abstract string GetTitle(bool forSwitchExpression);
        public abstract Analyzer CreateAnalyzer(ISyntaxFacts syntaxFacts, ParseOptions options);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var ifStatement = await context.TryGetRelevantNodeAsync<TIfStatementSyntax>().ConfigureAwait(false);
            if (ifStatement == null || ifStatement.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return;
            }

            var semanticModel = (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
            var ifOperation = semanticModel.GetOperation(ifStatement);
            if (!(ifOperation is IConditionalOperation { Parent: IBlockOperation parentBlock }))
            {
                return;
            }

            var operations = parentBlock.Operations;
            var index = operations.IndexOf(ifOperation);
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                return;
            }

            var analyzer = CreateAnalyzer(syntaxFactsService, ifStatement.SyntaxTree.Options);
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

            if (analyzer.Supports(Feature.SwitchExpression) &&
                CanConvertToSwitchExpression(sections))
            {
                context.RegisterRefactoring(
                    new MyCodeAction(GetTitle(forSwitchExpression: true),
                        c => UpdateDocumentAsync(document, target, ifStatement, sections, convertToSwitchExpression: true, c)),
                    ifStatement.Span);
            }
        }

        private static bool CanConvertToSwitchExpression(ImmutableArray<AnalyzedSwitchSection> sections)
        {
            return
                // There must be a default case for an exhaustive switch expression
                sections.Any(section => section.Labels.IsDefault) &&
                // All arms must have a single label
                sections.All(section => section.Labels.IsDefault || section.Labels.Length == 1) &&
                // There must be at least one return statement
                sections.Any(section => GetSwitchArmKind(section.Body) == OperationKind.Return) &&
                // All arms must be convertible to a switch arm
                sections.All(section => GetSwitchArmKind(section.Body) != default);

            static OperationKind GetSwitchArmKind(IOperation op)
            {
                switch (op)
                {
                    case IReturnOperation { ReturnedValue: { } }:
                    case IThrowOperation { Exception: { } }:
                        return op.Kind;

                    case IBlockOperation { Operations: { Length: 1 } statements }:
                        return GetSwitchArmKind(statements[0]);
                }

                return default;
            }
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
